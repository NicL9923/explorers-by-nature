using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExplorersByNature.Shared;

static class FrontierTests
{
    public static void Run(string directory,Func<object,string> encode,Func<string,RanchState> decode,Action<bool,string> check)
    {
        DeerRaycasts(check);
        string path=Path.Combine(directory,"frontier.json");
        Ranch Open()=>new Ranch(path,encode,decode,(x,z)=>20);
        var player=new Visitor{x=-90,y=20,z=-222};
        void At(FrontierSite site){player.x=site.x;player.z=site.z;}
        Request Shot(int id)=>new Request{action="fire",id=id,ax=0,ay=-.75f,az=10};
        using(var ranch=Open())
        {
            check(ranch.Snapshot.wood==200 && ranch.Snapshot.stone==100 && ranch.Snapshot.meat==0 && !ranch.Snapshot.huntingEnabled,"new ranch starts with building supplies and hunting off");
            check(ranch.Apply(new Request{action="place",kind="foundation",x=-30,z=-74},player,0)=="","foundation purchases with shared supplies");
            check(ranch.Snapshot.wood==192 && ranch.Snapshot.stone==96,"foundation charges wood and stone exactly once");
            int id=ranch.Snapshot.pieces.Single().id;
            check(ranch.Apply(new Request{action="move",kind="foundation",id=id,x=-31,z=-74},player,1)=="" && ranch.Snapshot.wood==192 && ranch.Snapshot.stone==96,"moving built pieces costs no resources");
            check(ranch.Apply(new Request{action="remove",id=id},player,2)=="" && ranch.Snapshot.wood==200 && ranch.Snapshot.stone==100,"removing built pieces refunds the full cost");
            var wood=FrontierSites.Wood[0];At(wood);
            check(ranch.Apply(new Request{action="gather",kind="wood",id=wood.id},player,10)=="" && ranch.Snapshot.wood==208,"axe gathering awards eight wood");
            int gatheredRevision=ranch.Revision;
            check(ranch.Apply(new Request{action="gather",kind="wood",id=wood.id},player,12)!="" && ranch.Revision==gatheredRevision,"depleted node cannot grant twice or advance revision");
            var stone=FrontierSites.Stone[0];At(stone);
            check(ranch.Apply(new Request{action="gather",kind="stone",id=stone.id},player,11)!="","gathering tools enforce a shared two-second cooldown");
            check(ranch.Apply(new Request{action="gather",kind="stone",id=stone.id},player,12)=="" && ranch.Snapshot.stone==106,"pickaxe gathering awards six stone");
            player.x+=5;
            check(ranch.Apply(new Request{action="gather",kind="stone",id=1},player,15)!="","gathering requires being within four metres");
            check(ranch.Apply(new Request{action="gather",kind="wood",id=99},player,15)!="" && ranch.Apply(new Request{action="gather",kind="meat",id=0},player,15)!="","gathering rejects invalid site IDs and resource kinds");
            At(wood);
            check(ranch.Apply(new Request{action="gather",kind="wood",id=wood.id},player,69)!="","wood node remains depleted until regrowth deadline");
            check(ranch.Apply(new Request{action="gather",kind="wood",id=wood.id},player,70)=="" && ranch.Snapshot.wood==216,"wood node regrows after sixty seconds");
            player.mounted=true;At(FrontierSites.Wood[1]);
            check(ranch.Apply(new Request{action="gather",kind="wood",id=1},player,75)!="","riders must dismount to gather");player.mounted=false;
            var deer=FrontierSites.Deer[0];At(deer);player.z-=10;
            check(ranch.Apply(Shot(deer.id),player,80)!="" && ranch.Snapshot.meat==0,"deer hunting starts disabled");
            int beforeShot=ranch.Revision;
            check(ranch.Apply(Shot(-1),player,80)=="" && ranch.Revision==beforeShot,"untargeted shot works with hunting off without saving ranch");
            check(ranch.Apply(Shot(-1),player,81)!="","untargeted shots also enforce reload time");
            check(ranch.Apply(new Request{action="hunting-mode",id=2},player,90)!="","hunting toggle rejects invalid mode");
            check(ranch.Apply(new Request{action="hunting-mode",id=1},player,90)=="" && ranch.Snapshot.huntingEnabled,"hunting requires an explicit shared opt-in");
            var wrongAim=Shot(deer.id);wrongAim.az=-10;
            check(ranch.Apply(wrongAim,player,100)!="" && ranch.Snapshot.meat==0,"shot facing away cannot harvest deer");
            check(ranch.Apply(Shot(deer.id),player,101)!="","misses consume the eight-second reload interval");
            player.z=deer.z-2;
            check(ranch.Apply(new Request{action="fire",id=deer.id,ax=.3f,ay=-1.4f,az=1.26f},player,108)=="" && ranch.Snapshot.meat==2,"near deer body-edge hit awards two meat");
            player.z=deer.z-10;
            check(!FrontierSites.Ready(ranch.Snapshot,"hunt",deer.id,227) && FrontierSites.Ready(ranch.Snapshot,"hunt",deer.id,228),"hunted deer returns after 120 seconds");
            check(ranch.Apply(Shot(deer.id),player,116)!="" && ranch.Snapshot.meat==2,"depleted deer cannot be harvested twice");
            player.z=deer.z-70;
            check(ranch.Apply(Shot(deer.id),player,124)!="","deer beyond sixty metres cannot be harvested");
            player.z=deer.z-10;
            check(ranch.Apply(Shot(99),player,132)!="" && ranch.Apply(Shot(-2),player,132)!="","shots cannot target farm animals or arbitrary IDs");
            player.mounted=true;check(ranch.Apply(Shot(-1),player,132)!="","riders must dismount before firing");player.mounted=false;
            var noAim=Shot(-1);noAim.ax=float.NaN;
            check(ranch.Apply(noAim,player,132)!="","invalid aim vectors cannot fire");
            check(ranch.Apply(Shot(deer.id),player,228)=="" && ranch.Snapshot.meat==4,"returned deer can be hunted again");
            var clone=ranch.Snapshot;clone.wood=0;clone.cooldowns[0].ready=0;
            check(ranch.Snapshot.wood==216 && ranch.Snapshot.cooldowns[0].ready!=0,"resource balances and cooldown snapshots are detached from authority");
        }
        using(var ranch=Open())check(ranch.Snapshot.wood==216 && ranch.Snapshot.stone==106 && ranch.Snapshot.meat==4 && ranch.Snapshot.huntingEnabled && !FrontierSites.Ready(ranch.Snapshot,"hunt",0,229),"restart preserves resources hunting mode and regrowth");
        var empty=new RanchState{wood=0,stone=0};File.WriteAllText(path,encode(empty));
        using(var ranch=Open())
        {
            player.x=-90;player.z=-222;var build=new Request{action="place",kind="foundation",x=-30,z=-74};
            check(Ranch.PlacementReason(ranch.Snapshot,build,player,(x,z)=>20)!="" && ranch.Apply(build,player,300)!="" && ranch.Revision==0,"preview and authority reject unaffordable construction");
            check(ranch.Snapshot.wood==0 && ranch.Snapshot.stone==0,"explicit zero balances are never mistaken for legacy saves");
        }
        var legacy=JsonNode.Parse(encode(new RanchState()));foreach(string field in new[]{"wood","stone","meat","huntingEnabled","cooldowns"})legacy.AsObject().Remove(field);
        File.WriteAllText(path,legacy.ToJsonString());
        RanchState UnityStyleLegacyDecode(string json)
        {
            var state=decode(json);var fields=JsonNode.Parse(json).AsObject();
            if(!fields.ContainsKey("wood"))state.wood=0;if(!fields.ContainsKey("stone"))state.stone=0;if(!fields.ContainsKey("cooldowns"))state.cooldowns=null;return state;
        }
        using(var ranch=new Ranch(path,encode,UnityStyleLegacyDecode,(x,z)=>20))check(ranch.Snapshot.wood==200 && ranch.Snapshot.stone==100 && ranch.Snapshot.cooldowns.Count==0 && !ranch.Snapshot.huntingEnabled,"legacy save migration restores starter supplies even with Unity-style missing defaults");
        foreach(string negative in new[]{"wood","stone","meat"})
        {
            var bad=JsonNode.Parse(encode(new RanchState()));bad[negative]=-1;File.WriteAllText(path,bad.ToJsonString());
            bool rejected=false;try{using var ranch=Open();}catch(InvalidDataException){rejected=true;}check(rejected,"negative saved "+negative+" is rejected");
        }
        var invalid=new RanchState();invalid.cooldowns.Add(new SiteCooldown{kind="wood",id=99,ready=100});File.WriteAllText(path,encode(invalid));
        bool invalidSite=false;try{using var ranch=Open();}catch(InvalidDataException){invalidSite=true;}check(invalidSite,"saved cooldowns require a catalog site");
        File.WriteAllText(path,encode(new RanchState()));
        var options=new JsonSerializerOptions{IncludeFields=true};
        Request DecodeRequest(string json)=>JsonSerializer.Deserialize<Request>(json,options);
        using(var server=new RanchServer(IPAddress.Loopback,0,"frontier",Open(),encode,DecodeRequest))
        {
            TcpClient Join(){var c=new TcpClient("127.0.0.1",server.Port);c.ReceiveTimeout=3000;Wire.Write(c.GetStream(),encode(new Request{action="join",token="frontier"}));Wire.Read(c.GetStream());return c;}
            Reply Send(TcpClient client,Request request){Wire.Write(client.GetStream(),encode(request));return JsonSerializer.Deserialize<Reply>(Wire.Read(client.GetStream()),options);}
            using var a=Join();using var b=Join();var site=FrontierSites.Wood[0];
            Request Gather()=>new Request{action="gather",kind="wood",id=site.id,px=site.x,py=20,pz=site.z,model=PlayerModels.TrailScout};
            var rider=Gather();rider.mounted=true;
            var denied=Send(a,rider);check(!denied.ok && denied.players.Single(p=>p.id==denied.playerId).mounted,"mounted pose reaches authority before gathering validation");
            var collected=Send(a,Gather());var competing=Send(b,Gather());
            check(collected.ok && !competing.ok && competing.state.wood==208 && competing.state.cooldowns.Single().kind=="wood","two clients share gathering rewards and node depletion");
            var mounted=Send(a,new Request{action="poll",mounted=true,px=site.x,py=20,pz=site.z});
            var seen=Send(b,new Request{action="poll",px=site.x,py=20,pz=site.z});
            check(seen.players.Single(p=>p.id==mounted.playerId).mounted,"mounted appearance propagates to the other client");
            var enabled=Send(a,new Request{action="hunting-mode",id=1,px=site.x,py=20,pz=site.z});
            var observed=Send(b,new Request{action="poll",px=site.x,py=20,pz=site.z});
            check(enabled.ok && observed.state.huntingEnabled && observed.state.wood==208,"shared hunting opt-in replicates without resetting supplies");
        }
    }

    static void DeerRaycasts(Action<bool,string> check)
    {
        var site=FrontierSites.Deer[0];var player=new Visitor{x=site.x,y=20,z=site.z-2};
        bool Hit(int id,float x,float y,float z)=>FrontierSites.HitDeer(id,player,x,y,z,(px,pz)=>20);
        check(Hit(0,.3f,-1.4f,1.26f),"near deer box edge is hittable without aiming at its center");
        player.z=site.z-50;
        check(!Hit(0,5,-.75f,50),"distant shot beside deer box misses even within the former aim cone");
        check(!Hit(0,0,0,-1),"deer box behind the ray cannot be hit");
        site=FrontierSites.Deer[1];player.x=site.x+.6f;player.y=19.25f;player.z=site.z-10;
        check(Hit(1,0,0,1),"rotated deer length accepts a world-x offset beyond unrotated width");
        player.x=site.x+.9f;
        check(!Hit(1,0,0,1),"shot outside rotated deer box misses");
        site=FrontierSites.Deer[0];player.x=site.x;player.z=site.z-60.5f;
        check(Hit(0,0,0,1),"sixty-metre ray can reach a deer box whose center is farther away");
        player.z=site.z-61;
        check(!Hit(0,0,0,1),"ray cannot reach a deer box beyond sixty metres");
        player.z=site.z;
        check(!Hit(0,0,0,1),"ray starting inside deer box matches Unity's no-hit behavior");
    }
}
