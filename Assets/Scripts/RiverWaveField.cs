using System;
using UnityEngine;

namespace ExplorersByNature
{
    // A local, linearized shallow-water wave field, not a volume-fluid solver. Terrain
    // depth sets wave speed; semi-Lagrangian transport carries disturbances downstream.
    // Solid shores reflect waves, while a sponge at the active-grid edge absorbs them.
    public sealed class RiverWaveField
    {
        public readonly int Width, Height;
        public readonly float Spacing;
        public Vector2 Origin { get; private set; }
        public readonly float[] Elevation, Depth;
        readonly float[] velocity, transported, transportedVelocity, next, nextVelocity;
        readonly Vector2[] flow;
        public RiverWaveField(int width, int height, float spacing)
        {
            if(width<3 || height<3 || spacing<=0) throw new ArgumentOutOfRangeException();
            Width=width; Height=height; Spacing=spacing;
            int n=width*height;
            Elevation=new float[n]; Depth=new float[n]; velocity=new float[n];
            transported=new float[n]; transportedVelocity=new float[n];
            next=new float[n]; nextVelocity=new float[n]; flow=new Vector2[n];
        }
        public void Reset(Vector2 origin, Func<float,float,float> depthAt, Func<float,float,Vector2> flowAt)
        {
            Origin=origin; Array.Clear(Elevation,0,Elevation.Length); Array.Clear(velocity,0,velocity.Length);
            for(int z=0;z<Height;z++) for(int x=0;x<Width;x++)
            {
                int i=z*Width+x; float wx=origin.x+x*Spacing, wz=origin.y+z*Spacing;
                Depth[i]=Mathf.Max(0,depthAt(wx,wz)); flow[i]=flowAt(wx,wz);
            }
        }
        public void Shift(Vector2 origin, Func<float,float,float> depthAt, Func<float,float,Vector2> flowAt)
        {
            if(origin==Origin)return;
            // Reuse the integration scratch buffers, preserving both displacement and
            // momentum in world space. Newly exposed cells start at rest.
            for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
            {
                int i=z*Width+x;float wx=origin.x+x*Spacing,wz=origin.y+z*Spacing;
                float oldX=(wx-Origin.x)/Spacing,oldZ=(wz-Origin.y)/Spacing;
                Depth[i]=Mathf.Max(0,depthAt(wx,wz));flow[i]=flowAt(wx,wz);
                bool overlap=x>0 && x<Width-1 && z>0 && z<Height-1 && Depth[i]>=.025f
                    && oldX>=0 && oldX<=Width-1 && oldZ>=0 && oldZ<=Height-1;
                next[i]=overlap?Interpolate(Elevation,oldX,oldZ):0;
                nextVelocity[i]=overlap?Interpolate(velocity,oldX,oldZ):0;
            }
            Array.Copy(next,Elevation,next.Length);Array.Copy(nextVelocity,velocity,next.Length);
            Origin=origin;
        }
        public float Sample(float x, float z) => Interpolate(Elevation,(x-Origin.x)/Spacing,(z-Origin.y)/Spacing);
        float Interpolate(float[] values,float x,float z)
        {
            x=Mathf.Clamp(x,0,Width-1.001f); z=Mathf.Clamp(z,0,Height-1.001f);
            int ix=(int)x, iz=(int)z, i=iz*Width+ix;
            return Mathf.Lerp(Mathf.Lerp(values[i],values[i+1],x-ix),Mathf.Lerp(values[i+Width],values[i+Width+1],x-ix),z-iz);
        }
        public void Impulse(float wx,float wz,float strength,float radius)
        {
            if(radius<=0 || float.IsNaN(strength) || float.IsInfinity(strength)) return;
            int x0=Mathf.Max(1,Mathf.FloorToInt((wx-radius-Origin.x)/Spacing));
            int x1=Mathf.Min(Width-2,Mathf.CeilToInt((wx+radius-Origin.x)/Spacing));
            int z0=Mathf.Max(1,Mathf.FloorToInt((wz-radius-Origin.y)/Spacing));
            int z1=Mathf.Min(Height-2,Mathf.CeilToInt((wz+radius-Origin.y)/Spacing));
            for(int z=z0;z<=z1;z++) for(int x=x0;x<=x1;x++)
            {
                int i=z*Width+x; if(Depth[i]<.025f)continue;
                float dx=Origin.x+x*Spacing-wx,dz=Origin.y+z*Spacing-wz;
                float r=Mathf.Sqrt(dx*dx+dz*dz)/radius;
                if(r<1)velocity[i]=Mathf.Clamp(velocity[i]+strength*(1-r*r)*(1-r*r),-2,2);
            }
        }
        public void Step(float deltaTime)
        {
            if(deltaTime<=0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))return;
            // Clamp elapsed catch-up work, then enforce the two-dimensional CFL limit.
            float remaining=Mathf.Min(deltaTime,.1f), maxStep=Mathf.Min(1f/60,Spacing*.35f/Mathf.Sqrt(9.81f));
            while(remaining>0)
            {
                float dt=Mathf.Min(remaining,maxStep); remaining-=dt;
                for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
                {
                    int i=z*Width+x;
                    transported[i]=Interpolate(Elevation,x-flow[i].x*dt/Spacing,z-flow[i].y*dt/Spacing);
                    transportedVelocity[i]=Interpolate(velocity,x-flow[i].x*dt/Spacing,z-flow[i].y*dt/Spacing);
                }
                for(int z=1;z<Height-1;z++)for(int x=1;x<Width-1;x++)
                {
                    int i=z*Width+x;
                    if(Depth[i]<.025f){next[i]=nextVelocity[i]=0;continue;}
                    float h=transported[i];
                    float lap=(WetNeighbor(i-1,h)+WetNeighbor(i+1,h)+WetNeighbor(i-Width,h)+WetNeighbor(i+Width,h)-4*h)/(Spacing*Spacing);
                    float edge=Mathf.Min(Mathf.Min(x,Width-1-x),Mathf.Min(z,Height-1-z));
                    float damping=.8f+Mathf.Max(0,7-edge)*.7f;
                    float v=(transportedVelocity[i]+9.81f*Mathf.Min(1,Depth[i])*lap*dt)*Mathf.Exp(-damping*dt);
                    nextVelocity[i]=Mathf.Clamp(v,-2,2); next[i]=Mathf.Clamp(h+v*dt,-.24f,.24f);
                }
                Array.Copy(next,Elevation,next.Length); Array.Copy(nextVelocity,velocity,next.Length);
            }
        }
        float WetNeighbor(int i,float center) => Depth[i]<.025f?center:transported[i];

        // Archimedes-like displaced fraction with drag, for small decorative floating wood.
        // Two times water density relative to the object puts equilibrium at half immersion.
        public static void FloatStep(ref float y,ref float velocityY,float surface,float radius,float dt)
        {
            float remaining=Mathf.Clamp(dt,0,.1f);
            while(remaining>0)
            {
                float step=Mathf.Min(remaining,1f/120);remaining-=step;
                float displaced=Mathf.Clamp01((surface-y+radius)/(2*Mathf.Max(radius,.01f)));
                velocityY+=(9.81f*(2*displaced-1)-velocityY*4)*step;
                y+=velocityY*step;
            }
        }
    }
}
