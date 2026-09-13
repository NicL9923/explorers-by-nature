using UnityEngine;

namespace ExplorersByNature
{
    /// <summary>Root-relative tip displacement with world-space inertia and a damped bending spring.</summary>
    public struct FurSpring
    {
        public Vector3 displacement, velocity;
        public void Step(Vector3 rootTravel, Vector3 wind, Vector3 normal, float length, float dt)
        {
            if (dt <= 0) return;
            if (dt > .2f || rootTravel.sqrMagnitude > .25f) { displacement=velocity=Vector3.zero; return; }
            // Root movement leaves the free tip behind. The restoring force pulls it back.
            displacement -= Vector3.ProjectOnPlane(rootTravel,normal);
            int steps=Mathf.Max(1,Mathf.CeilToInt(dt*120));float h=dt/steps;
            float drag=Mathf.Clamp(length*length/(.026f*.026f),.5f,16f);
            Vector3 force=Vector3.ProjectOnPlane(wind,normal)*(.35f*drag);
            for(int i=0;i<steps;i++)
            {
                velocity += (force-displacement*155f-velocity*20f)*h;
                displacement += velocity*h;
                displacement=Vector3.ProjectOnPlane(displacement,normal);
                float maximum=length*.8f;
                if(displacement.sqrMagnitude>maximum*maximum)
                {
                    displacement=displacement.normalized*maximum;
                    float outward=Vector3.Dot(velocity,displacement.normalized);
                    if(outward>0)velocity-=displacement.normalized*outward;
                }
            }
        }
    }
}
