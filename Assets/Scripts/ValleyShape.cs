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
            // The ranch, river outing and overlook retain their original ground. Only the
            // northern mountain basin transitions into sharper ridges and sheltered gullies.
            if(z>210)
            {
                float alpine = Mathf.SmoothStep(0,1,Mathf.InverseLerp(210,290,z));
                float carved = Mathf.Max(MountainMass(x+205,z-355,130,160,222,.3f),
                    Mathf.Max(MountainMass(x-155,z-385,145,155,272,1.4f),MountainMass(x-355,z-255,125,175,180,2.1f)));
                float foothills=peaks*.24f;
                peaks=Mathf.Lerp(peaks,Mathf.Max(carved,foothills),alpine);
            }
            float terrain = 18f + hills + ridge + peaks;
            float riverDistance = Mathf.Abs(x - RiverX(z));
            float valley = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(7, 52, riverDistance));
            return Mathf.Lerp(8f, terrain, valley);
        }

        // A bent watershed with a broken crest and tributary gullies, rather than a
        // radial cone. Also used by the decorative range beyond the playable terrain.
        public static float MountainMass(float x, float z, float width, float depth, float height, float phase)
        {
            float dx = x / width, dz = z / depth;
            float seed = phase * 173f;
            float bend = .16f * Mathf.Sin(dz * 3.1f + phase) + .07f * Mathf.Sin(dz * 7.3f - phase);
            float cross = dx - bend;
            float along = dz + .16f * dx;
            float radius = Mathf.Sqrt(cross * cross + along * along * .72f);
            float envelope = Mathf.Clamp01(1 - radius / 1.55f);
            if (envelope <= 0) return 0;

            // The narrow spine carries several unequal summits. Wide shoulders read as
            // connected foothills while the crest exposes alternating light/shadow faces.
            float crest = Mathf.Clamp01(1 - (Mathf.Sqrt(cross * cross + .012f) - .1095f) / 1.12f);
            float length = Mathf.Clamp01(1 - Mathf.Abs(along) / 1.65f);
            float saddles = .86f + .10f * Mathf.Sin(along * 4.8f + phase)
                + .04f * Mathf.Sin(along * 9.1f - phase * 2);
            float spine = Mathf.Pow(crest, 1.65f) * Mathf.Pow(length, .85f) * saddles;
            float shoulder = Mathf.Pow(envelope, 1.7f) * .70f;
            // Smooth the shoulder/crest intersection so it does not form a hard seam.
            float difference = spine - shoulder;
            float mass = (spine + shoulder + Mathf.Sqrt(difference * difference + .0016f)) * .5f;

            float warp = (Mathf.PerlinNoise((dx + seed) * 1.7f, (dz + 311) * 1.7f) - .5f) * .45f;
            float tributary = Mathf.Abs(Mathf.Sin((along + warp) * 9f + Mathf.Abs(cross) * 3f));
            float gullies = tributary * tributary * Mathf.Clamp01(Mathf.Abs(cross) * 3);
            // Broad folds belong in geometry; fine rock belongs in the material. Keeping
            // this below the backdrop sample frequency avoids needle-like aliasing.
            float folds = (Mathf.PerlinNoise(dx * 2.8f + seed, dz * 3.4f + 137) - .5f) * 2
                + .25f * (Mathf.PerlinNoise(dx * 6.2f + 71, dz * 7.1f + seed) - .5f) * 2;
            float flank = Mathf.Sin(envelope * Mathf.PI);
            return Mathf.Max(0, height * (mass + (folds * .035f - gullies * .065f) * flank))
                * Mathf.SmoothStep(0, 1, Mathf.Clamp01(envelope * 12));
        }

        static float Gaussian(float x, float z, float sx, float sz) => Mathf.Exp(-.5f * (x * x / (sx * sx) + z * z / (sz * sz)));
        public static Vector3 Ground(float x, float z, float offset = 0) => new Vector3(x, Height(x, z) + offset, z);
        public static Vector3 Trail(float z, float offset = 0) => Ground(TrailX(z), z, offset);
        public static Vector3 Spawn => Trail(-235, .3f);
        public static Vector3 Overlook => Trail(135, .3f);
    }
}
