using System;
using System.Linq;

namespace ExplorersByNature.Shared
{
    public sealed class FrontierSite
    {
        public readonly int id;
        public readonly float x,z;
        public FrontierSite(int id,float x,float z){this.id=id;this.x=x;this.z=z;}
    }
    [Serializable] public sealed class SiteCooldown { public string kind; public int id; public long ready; }
    public static class FrontierSites
    {
        public static readonly FrontierSite[] Wood={
            new FrontierSite(0,-79,-245),new FrontierSite(1,-85,-248),new FrontierSite(2,-92,-251),new FrontierSite(3,-101,-248),
            new FrontierSite(4,-112,-244),new FrontierSite(5,-118,-236),new FrontierSite(6,-119,-222),new FrontierSite(7,-113,-214),
            new FrontierSite(8,-102,-212),new FrontierSite(9,-91,-214),new FrontierSite(10,-83,-217),new FrontierSite(11,-78,-227)};
        public static readonly FrontierSite[] Stone={
            new FrontierSite(0,-65,-243),new FrontierSite(1,-59,-248),new FrontierSite(2,-53,-243),new FrontierSite(3,-50,-235),
            new FrontierSite(4,-54,-227),new FrontierSite(5,-60,-219),new FrontierSite(6,-67,-214),new FrontierSite(7,-75,-211),
            new FrontierSite(8,-81,-205),new FrontierSite(9,-69,-202),new FrontierSite(10,-58,-207),new FrontierSite(11,-49,-216)};
        public static readonly FrontierSite[] Deer={new FrontierSite(0,-145,-191),new FrontierSite(1,-161,-206),new FrontierSite(2,-139,-224),new FrontierSite(3,-166,-178)};
        public static bool TryGet(string kind,int id,out FrontierSite site)
        {
            var sites=kind=="wood"?Wood:kind=="stone"?Stone:kind=="hunt"?Deer:null;
            site=sites==null?null:sites.FirstOrDefault(s=>s.id==id);return site!=null;
        }
        public static bool Ready(RanchState state,string kind,int id,long now)
        {return state!=null && !state.cooldowns.Any(c=>c.kind==kind && c.id==id && c.ready>now);}
        // Match FrontierWorld's rotated deer BoxCollider and the walker's eye height.
        public static bool HitDeer(int id,Visitor player,float ax,float ay,float az,Func<float,float,float> ground)
        {
            if(!TryGet("hunt",id,out var site) || player==null || !Ranch.Finite(player.x) || !Ranch.Finite(player.y) || !Ranch.Finite(player.z)
                || !Ranch.Finite(ax) || !Ranch.Finite(ay) || !Ranch.Finite(az))return false;
            double length=Math.Sqrt((double)ax*ax+(double)ay*ay+(double)az*az);
            if(length<.001)return false;
            double yaw=site.id*83*Math.PI/180,cos=Math.Cos(yaw),sin=Math.Sin(yaw);
            double wx=(double)player.x-site.x,wz=(double)player.z-site.z;
            double ox=cos*wx-sin*wz,oy=(double)player.y+1.65-(ground(site.x,site.z)+.9),oz=sin*wx+cos*wz;
            double dx=(cos*ax-sin*az)/length,dy=ay/length,dz=(sin*ax+cos*az)/length;
            // Unity raycasts do not report a collider when the ray starts inside it.
            if(Math.Abs(ox)<.325 && Math.Abs(oy)<.725 && Math.Abs(oz)<.75)return false;
            double enter=0,leave=60;
            return Slab(ox,dx,.325,ref enter,ref leave) && Slab(oy,dy,.725,ref enter,ref leave) && Slab(oz,dz,.75,ref enter,ref leave);
        }
        static bool Slab(double origin,double direction,double halfSize,ref double enter,ref double leave)
        {
            if(Math.Abs(direction)<1e-12)return origin>=-halfSize && origin<=halfSize;
            double near=(-halfSize-origin)/direction,far=(halfSize-origin)/direction;
            if(near>far){double swap=near;near=far;far=swap;}
            enter=Math.Max(enter,near);leave=Math.Min(leave,far);return enter<=leave;
        }
    }
}
