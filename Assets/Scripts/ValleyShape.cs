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
                float carved = Mathf.Max(AlpinePeak(x+205,z-355,130,160,222,.3f),
                    Mathf.Max(AlpinePeak(x-155,z-385,145,155,272,1.4f),AlpinePeak(x-355,z-255,125,175,180,2.1f)));
                float foothills=peaks*.24f;
                peaks=Mathf.Lerp(peaks,Mathf.Max(carved,foothills),alpine);
            }
            float terrain = 18f + hills + ridge + peaks;
            float riverDistance = Mathf.Abs(x - RiverX(z));
            float valley = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(7, 52, riverDistance));
            return Mathf.Lerp(8f, terrain, valley);
        }

        static float AlpinePeak(float x,float z,float width,float depth,float height,float phase)
        {
            float dx=x/width,dz=z/depth,angle=Mathf.Atan2(dz,dx);
            float radius=Mathf.Sqrt(dx*dx+dz*dz);
            float spur=1+.18f*Mathf.Cos(angle*5+phase)+.07f*Mathf.Sin(angle*8-phase);
            float mass=Mathf.Max(0,1-radius/(spur*1.45f));
            float warp=Mathf.PerlinNoise((x+phase*175)*.013f,(z+phase*237)*.011f)*32;
            float erosion=(1-Mathf.Abs(Mathf.PerlinNoise((x+713+warp+phase*130)*.025f,(z+415+phase*215)*.029f)*2-1))-.6f;
            float broad=Mathf.PerlinNoise((x+phase*143)*.01f,(z+phase*191)*.014f)-.5f;
            return Mathf.Max(0,height*Mathf.Pow(mass,1.4f)+(erosion*12+broad*24)*mass);
        }

        static float Gaussian(float x, float z, float sx, float sz) => Mathf.Exp(-.5f * (x * x / (sx * sx) + z * z / (sz * sz)));
        public static Vector3 Ground(float x, float z, float offset = 0) => new Vector3(x, Height(x, z) + offset, z);
        public static Vector3 Trail(float z, float offset = 0) => Ground(TrailX(z), z, offset);
        public static Vector3 Spawn => Trail(-235, .3f);
        public static Vector3 Overlook => Trail(135, .3f);
    }
}
