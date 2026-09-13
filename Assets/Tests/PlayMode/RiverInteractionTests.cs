using System.Collections;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RiverInteractionTests
{
    [UnityTest]
    public IEnumerator PebblesDisturbWaterAndDeepWaterSupportsTheWalkerAcrossQualityChanges()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
        Assert.That(WindWeather.Current,Is.Not.Null,"Scene reloads must retain the wind driver.");
        var river=RiverDynamics.Current;Assert.That(river,Is.Not.Null);
        var session=Object.FindFirstObjectByType<WalkSession>();var walker=session.walker;
        walker.Automated=true;
        try
        {
            river.Visit();yield return null;
            int before=river.ImpactCount;
            Assert.That(river.ThrowPebble(),Is.True);
            float deadline=Time.realtimeSinceStartup+6;
            while(river.ImpactCount==before && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(river.ImpactCount,Is.GreaterThan(before),"A thrown pebble must reach the water and disturb it.");
            Assert.That(RiverDynamics.DepthAt(river.LastImpact.x,river.LastImpact.z),Is.GreaterThan(0));
            var field=river.ActiveField;
            foreach(bool high in new[]{false,true})
            {
                session.ApplyQuality(high);yield return null;
                Assert.That(river.ActiveField,Is.SameAs(field),"Changing graphics must retain ongoing waves.");
                Assert.That(river.FloatingObjectCount,Is.EqualTo(8));
                walker.Teleport(new Vector3(ValleyShape.RiverX(-165),ValleyShape.WaterHeight-.65f,-165));
                for(int frame=0;frame<90;frame++){walker.Move(Vector2.zero,false,1f/60);yield return null;}
                Assert.That(walker.IsWading,Is.True);
                Assert.That(walker.view.transform.position.y,Is.GreaterThan(ValleyShape.WaterHeight+.45f),"Deep water must keep the family camera above the surface.");
                river.Visit();walker.Move(Vector2.zero,false,1f/60);
                Assert.That(walker.IsWading,Is.False,"Returning to the bank must restore normal walking.");
            }
        }
        finally{walker.Automated=false;walker.SetMenu(true);}
    }
}
