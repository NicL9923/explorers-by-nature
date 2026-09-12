using UnityEngine;

namespace ExplorersByNature
{
    // Shared by terrain, the trail, wildlife, and the reproducible camera route.
    public static class ValleyShape
    {
        public const float Size = 900f;
        public const float WaterHeight = 12f;
        public static float RiverX(float z) => 35f + Mathf.Sin(z * .012f) * 22f;
        public static float TrailX(float z) => -65f + Mathf.Sin(z * .013f) * 18f;

        public static float Height(float x, float z)
        {
            float hills = Mathf.PerlinNoise((x + 1300) * .006f, (z + 800) * .006f) * 16f;
            float ridge = 68f * Gaussian(x + 95, z - 140, 100, 95);
            float peaks = 195f * Gaussian(x + 205, z - 350, 95, 100)
                + 245f * Gaussian(x - 155, z - 370, 90, 85)
                + 130f * Gaussian(x - 355, z - 200, 85, 150);
            float terrain = 18f + hills + ridge + peaks;
            float riverDistance = Mathf.Abs(x - RiverX(z));
            float valley = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(7, 52, riverDistance));
            return Mathf.Lerp(8f, terrain, valley);
        }

        static float Gaussian(float x, float z, float sx, float sz) => Mathf.Exp(-.5f * (x * x / (sx * sx) + z * z / (sz * sz)));
        public static Vector3 Ground(float x, float z, float offset = 0) => new Vector3(x, Height(x, z) + offset, z);
        public static Vector3 Trail(float z, float offset = 0) => Ground(TrailX(z), z, offset);
        public static Vector3 Spawn => Trail(-235, .3f);
        public static Vector3 Overlook => Trail(135, .3f);
    }
}
