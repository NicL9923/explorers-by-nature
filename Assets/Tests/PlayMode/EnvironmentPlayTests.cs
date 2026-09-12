using System.Collections;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    }
}
