using System.Collections;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ValleyPlayTests
{
    [UnityTest]
    public IEnumerator ExplorerStandsOnTerrainAndQualityKeepsWildlife()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");
        yield return null;
        var world = Object.FindFirstObjectByType<ValleyWorld>();
        var session = Object.FindFirstObjectByType<WalkSession>();
        Assert.That(world.Ready, Is.True);
        var walker = world.walker;
        walker.SetMenu(true);
        CharacterController controller = walker.GetComponent<CharacterController>();
        Vector3 start = walker.transform.position;
        for (int step = 0; step < 120; step++)
        {
            controller.Move(new Vector3(0, -4, 3) / 60);
            yield return null;
        }
        Assert.That(walker.transform.position.z - start.z, Is.GreaterThan(4), "Explorer must move along the starting trail");
        Assert.That(controller.isGrounded, Is.True, "Explorer must land on terrain");
        TerrainCollider terrainCollider = world.Ground.GetComponent<TerrainCollider>();
        Assert.That(terrainCollider.Raycast(new Ray(walker.view.transform.position, Vector3.down), out RaycastHit hit, 3), Is.True);
        Assert.That(hit.collider, Is.TypeOf<TerrainCollider>());
        int wildlife = Object.FindObjectsByType<WildlifeRoamer>(FindObjectsSortMode.None).Length;
        Assert.That(wildlife, Is.EqualTo(4));
        session.ApplyQuality(false);
        yield return null;
        Assert.That(world.DenseGrass.activeSelf, Is.False);
        Assert.That(Object.FindObjectsByType<WildlifeRoamer>(FindObjectsSortMode.None), Has.Length.EqualTo(wildlife));
        session.ApplyQuality(true);
        yield return null;
        Assert.That(world.DenseGrass.activeSelf, Is.True);
        Assert.That(Object.FindObjectsByType<WildlifeRoamer>(FindObjectsSortMode.None), Has.Length.EqualTo(wildlife));
    }
}
