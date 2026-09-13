using System.Collections;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public sealed class EnvironmentPlayTests
{
    [UnityTest]
    public IEnumerator DrizzleAndEveningKeepWorldLitAndSoundLocalized()
    {
        yield return SceneManager.LoadSceneAsync("Pinewatch");yield return null;
        var weather=Object.FindFirstObjectByType<SkyWeather>();Assert.That(weather,Is.Not.Null);
        weather.SetPreview(.85f,1);yield return null;yield return null;
        Assert.That(SkyWeather.RainAmount,Is.EqualTo(1));
        Assert.That(RenderSettings.sun.intensity,Is.GreaterThan(.15f),"Nights remain forgiving");
        Assert.That(RenderSettings.ambientSkyColor.maxColorComponent,Is.GreaterThan(.35f));
        var rain=weather.GetComponentInChildren<ParticleSystem>();Assert.That(rain,Is.Not.Null);Assert.That(rain.main.maxParticles,Is.LessThanOrEqualTo(400));
        var sound=Object.FindFirstObjectByType<NatureSoundscape>();Assert.That(sound,Is.Not.Null);
        int rivers=0;
        foreach(var source in sound.GetComponentsInChildren<AudioSource>())
            if(source.name=="River current"){rivers++;Assert.That(source.spatialBlend,Is.EqualTo(1));Assert.That(source.clip,Is.Not.Null);Assert.That(source.maxDistance,Is.LessThanOrEqualTo(100));}
        Assert.That(rivers,Is.GreaterThanOrEqualTo(8));
        weather.SetPreview(.42f,0);yield return null;
        Assert.That(SkyWeather.RainAmount,Is.Zero);Assert.That(RenderSettings.sun.intensity,Is.GreaterThan(1));
        int wings=0;
        foreach(var filter in Object.FindFirstObjectByType<NatureDetails>().GetComponentsInChildren<MeshFilter>())
            if(filter.sharedMesh.name=="Swept swallow feather silhouette")
            {
                wings++;foreach(var normal in filter.sharedMesh.normals)Assert.That(normal.sqrMagnitude,Is.InRange(.99f,1.01f),"Double-sided bird wings must have finite unit normals for HDR lighting");
            }
        Assert.That(wings,Is.GreaterThanOrEqualTo(4));
        var session=Object.FindFirstObjectByType<WalkSession>();
        var floor=Object.FindFirstObjectByType<ForestUnderstory>();Assert.That(floor.PlantCount,Is.GreaterThan(100));
        var volume=Object.FindFirstObjectByType<Volume>();Assert.That(volume.sharedProfile.TryGet<Tonemapping>(out var tone),Is.True);Assert.That(tone.mode.value,Is.EqualTo(TonemappingMode.ACES));
        session.ApplyQuality(false);yield return null;
        Assert.That(session.walker.view.GetUniversalAdditionalCameraData().renderPostProcessing,Is.False);
        Assert.That(floor.gameObject.activeInHierarchy,Is.True,"Low keeps the forest floor");
        var atmosphere=Object.FindFirstObjectByType<WoodlandAtmosphere>();
        foreach(var renderer in atmosphere.GetComponentsInChildren<Renderer>())Assert.That(renderer.enabled,Is.False);
        session.ApplyQuality(true);yield return null;
        Assert.That(session.walker.view.GetUniversalAdditionalCameraData().renderPostProcessing,Is.True);
        foreach(var renderer in atmosphere.GetComponentsInChildren<Renderer>())Assert.That(renderer.enabled,Is.True);
    }
}
