using System.Collections;
using System.Linq;
using ExplorersByNature;
using ExplorersByNature.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PlayerAvatarTests
{
    [UnityTest]
    public IEnumerator EveryExplorerImportsAtHumanScaleWithArticulatedLimbs()
    {
        var actor=new GameObject("Imported explorer test");var avatar=actor.AddComponent<PlayerAvatar>();
        try
        {
            foreach(string id in PlayerAvatar.Ids)
            {
                avatar.SetModel(id);
                Assert.That(avatar.Model,Is.Not.Null,id);
                var renderers=avatar.Model.GetComponentsInChildren<Renderer>();
                Assert.That(renderers,Is.Not.Empty,id+" has visible geometry");
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.y,Is.InRange(1.6f,2.1f),id+" human height");
                Assert.That(bounds.size.x,Is.LessThan(1.5f),id+" human width");
                var joints=avatar.Model.GetComponentsInChildren<Transform>();
                foreach(string name in new[]{"ArmL","ArmR","LegL","LegR","Torso","Head"})
                    Assert.That(joints.Count(joint=>joint.name==name),Is.EqualTo(1),id+" unique pivot "+name);
                var meshes=avatar.Model.GetComponentsInChildren<MeshFilter>().Select(filter=>filter.sharedMesh)
                    .Concat(avatar.Model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(skin=>skin.sharedMesh));
                long triangles=0;foreach(var mesh in meshes)for(int sub=0;sub<mesh.subMeshCount;sub++)triangles+=mesh.GetIndexCount(sub)/3;
                Assert.That(triangles,Is.GreaterThan(2000).And.LessThan(50000),id+" authored mesh budget");
                yield return null;
            }
        }
        finally {Object.Destroy(actor);}
    }

    [UnityTest]
    public IEnumerator RemoteExplorerSwapRetiresPreviousModelAndDefaultsUnknownIds()
    {
        var actor=new GameObject("Remote explorer test");var avatar=actor.AddComponent<PlayerAvatar>();
        try
        {
            avatar.SetModel(PlayerModels.RanchHand);var original=avatar.Model;
            avatar.SetModel(PlayerModels.TrailScout);
            Assert.That(original.activeSelf,Is.False,"retired model hides in the same frame");
            Assert.That(avatar.ModelId,Is.EqualTo(PlayerModels.TrailScout));
            yield return null;
            Assert.That(original==null,Is.True,"retired model is destroyed");
            Assert.That(actor.transform.childCount,Is.EqualTo(1));
            Assert.That(avatar.Model.activeInHierarchy,Is.True);
            var current=avatar.Model;avatar.SetModel(PlayerModels.TrailScout);
            Assert.That(avatar.Model,Is.SameAs(current),"unchanged polls retain the current model");
            avatar.SetModel("missing-explorer");yield return null;
            Assert.That(avatar.ModelId,Is.EqualTo(PlayerModels.Default));
            Assert.That(avatar.Model,Is.Not.Null);Assert.That(actor.transform.childCount,Is.EqualTo(1));
        }
        finally {Object.Destroy(actor);}
    }

    [UnityTest]
    public IEnumerator WardrobeConfirmsCancelsAndReleasesPreviewResources()
    {
        bool hadChoice=PlayerPrefs.HasKey("ExplorerModel");string saved=PlayerPrefs.GetString("ExplorerModel");
        PlayerWardrobe wardrobe=null;
        try
        {
            PlayerPrefs.SetString("ExplorerModel",PlayerModels.RanchHand);
            yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
            var walker=Object.FindFirstObjectByType<FirstPersonWalker>();walker.Automated=true;
            wardrobe=Object.FindFirstObjectByType<PlayerWardrobe>();Assert.That(wardrobe,Is.Not.Null);
            int cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            for(int cycle=0;cycle<3;cycle++)
            {
                string selected=PlayerWardrobe.SelectedId;wardrobe.Open();
                Assert.That(wardrobe.DraftId,Is.EqualTo(selected),"reopening starts at the current selection");
                Assert.That(walker.MenuOpen,Is.True);
                var preview=wardrobe.PreviewAvatar;
                var camera=preview.transform.parent.GetComponentInChildren<Camera>();var portrait=camera.targetTexture;
                Assert.That(portrait.IsCreated(),Is.True);
                wardrobe.Choose(PlayerAvatar.Index(PlayerModels.Homesteader));wardrobe.Confirm();
                Assert.That(PlayerWardrobe.SelectedId,Is.EqualTo(PlayerModels.Homesteader));
                Assert.That(PlayerPrefs.GetString("ExplorerModel"),Is.EqualTo(PlayerModels.Homesteader));
                Assert.That(wardrobe.Opened,Is.False);Assert.That(wardrobe.PreviewAvatar,Is.Null);
                yield return null;
                Assert.That(preview==null && camera==null && portrait==null,Is.True,"closing destroys preview, camera and render texture");
                wardrobe.Open();wardrobe.Choose(PlayerAvatar.Index(PlayerModels.Frontiersman));wardrobe.Close();
                Assert.That(PlayerWardrobe.SelectedId,Is.EqualTo(PlayerModels.Homesteader),"cancel preserves confirmed selection");
                yield return null;
                Assert.That(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(cameras),"repeated open/close does not leak cameras");
            }
        }
        finally
        {
            if(wardrobe!=null)wardrobe.Close();
            if(hadChoice)PlayerPrefs.SetString("ExplorerModel",saved);else PlayerPrefs.DeleteKey("ExplorerModel");
            PlayerPrefs.Save();
        }
    }
}
