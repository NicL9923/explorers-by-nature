using System;
using System.Collections;
using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AnimalFurPlayTests
{
    [UnityTest]
    public IEnumerator SwitchingQualityChangesFurBudgetWithoutRemovingTheAnimal()
    {
        int previous=QualitySettings.GetQualityLevel();
        int low=Array.IndexOf(QualitySettings.names,"Low"),high=Array.IndexOf(QualitySettings.names,"High");
        Assert.That(low,Is.EqualTo(0),"Low must remain available at the index used by ApplyQuality.");
        Assert.That(high,Is.EqualTo(1),"High must remain available at the index used by ApplyQuality.");
        var model=ModelArt.Instantiate("ReferenceDeer/Deer",null);
        try
        {
            var fur=model.GetComponent<AnimalFur>();Assert.That(fur,Is.Not.Null);
            QualitySettings.SetQualityLevel(high,true);yield return null;yield return null;
            Assert.That(fur.ConfiguredClumps,Is.EqualTo(1800));
            QualitySettings.SetQualityLevel(low,true);yield return null;yield return null;
            Assert.That(fur.ConfiguredClumps,Is.EqualTo(600));
            Assert.That(model.GetComponent<AnimalFurGroom>().skin.quality,Is.EqualTo(SkinQuality.Bone4),"The coat and its pinned roots must use the same skin weights on Low.");
            Assert.That(model.activeInHierarchy,Is.True);
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())Assert.That(skin.enabled,Is.True);
        }
        finally { UnityEngine.Object.Destroy(model);QualitySettings.SetQualityLevel(previous,true); }
    }
}
