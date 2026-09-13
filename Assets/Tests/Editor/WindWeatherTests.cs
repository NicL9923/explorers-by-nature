using ExplorersByNature;
using NUnit.Framework;
using UnityEngine;

public sealed class WindWeatherTests
{
    [Test]
    public void CalmStopsWindAndGustsStayCoherentAcrossFramesAndNearbyPlants()
    {
        Vector3 position = ValleyShape.Trail(-150);
        Assert.That(WindWeather.SampleVelocity(position, 37, 1, 0), Is.EqualTo(Vector3.zero));
        for (int second = 0; second < 120; second++)
        {
            Vector3 current = WindWeather.SampleVelocity(position, second, 1, 1.6f);
            Assert.That(current.magnitude, Is.InRange(0, 10.25f));
            Assert.That(Vector3.Distance(current, WindWeather.SampleVelocity(position, second + .016f, 1, 1.6f)), Is.LessThan(.08f));
            Assert.That(Vector3.Distance(current, WindWeather.SampleVelocity(position + Vector3.right, second, 1, 1.6f)), Is.LessThan(.5f));
        }
    }
}
