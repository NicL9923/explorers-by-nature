using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExplorersByNature.Shared
{
    [Serializable] public class Piece { public int id, x, z, level, turn; public string kind; }
    [Serializable] public class RanchState
    {
        public int schema = 1, revision, nextId = 1, milk, eggs, expeditionStage;
        public List<Piece> pieces = new List<Piece>();
        public long cowReady, henReady;
    }
    [Serializable] public class Visitor { public string id, name; public float x, y, z, yaw; }
    [Serializable] public class Request
    {
        public int protocol = 1, revision = -1, id, x, z, level, turn;
        public string action, token, name, kind;
        public float px, py, pz, yaw;
    }
    [Serializable] public class Reply
    {
        public bool ok, hasState; public string message, playerId, action; public RanchState state; public Visitor[] players;
        public long utc;
    }
    // Both solo and dedicated servers cross this interface. Disk commit precedes success.
    public sealed class Ranch : IDisposable
    {
        public static readonly string[] Kinds = { "foundation", "wall", "door", "roof", "fence", "flower", "bench", "lantern", "trough", "flowerbox", "campfire", "alpineflower" };
        readonly string path;
        readonly Func<object, string> encode;
        readonly Func<string, RanchState> decode;
        readonly Func<float, float, float> ground;
        RanchState state;
        FileStream lease;
        public int Revision => state.revision;
        public RanchState Snapshot => decode(encode(state));
        public Ranch(string path, Func<object,string> encode, Func<string,RanchState> decode, Func<float,float,float> ground)
        {
            this.path = path; this.encode = encode; this.decode = decode; this.ground = ground;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            try {
            if(File.Exists(path))
            {
                string saved=File.ReadAllText(path);
                foreach(string field in new[]{"schema","revision","nextId","milk","eggs","pieces","cowReady","henReady"})
                    if(!Regex.IsMatch(saved,"\""+field+"\"\\s*:"))throw new InvalidDataException("Incomplete ranch save. Restore the .bak file before starting.");
                state=decode(saved);
            }
            else state=new RanchState();
            if (state == null || state.schema != 1 || state.pieces == null || state.pieces.Count > 4000 || state.revision<0 || state.milk<0 || state.eggs<0 || state.cowReady<0 || state.henReady<0 || state.expeditionStage<0 || state.expeditionStage>3)
                throw new InvalidDataException("Unsupported or damaged ranch save. Restore the .bak file before starting.");
            if (state.pieces.Any(p=>p==null || !Kinds.Contains(p.kind) || Math.Abs((long)p.x)>140 || Math.Abs((long)p.z)>140 || p.turn<0 || p.turn>3 || p.level!=0 || p.id<1) || state.pieces.Select(p=>p.id).Distinct().Count()!=state.pieces.Count || state.nextId<=state.pieces.Select(p=>p.id).DefaultIfEmpty(0).Max()) throw new InvalidDataException("Invalid pieces in ranch save.");
            } catch { lease.Dispose(); throw; }
        }
        public void Dispose() => lease?.Dispose();
        public string Apply(Request r, Visitor player, long now)
        {
            if (r.action == "poll") return "";
            RanchState next = Snapshot;
            if (r.action == "pack" || r.action == "picnic" || r.action == "claim")
            {
                int required = r.action == "pack" ? 0 : r.action == "picnic" ? 1 : 2;
                if(next.expeditionStage != required) return "Your shared expedition journal has already changed.";
                float x = r.action == "picnic" ? ExpeditionX : HomeX, z = r.action == "picnic" ? ExpeditionZ : HomeZ;
                if(!Near(player,x,ground(x,z),z,9)) return "Walk closer to the picnic basket.";
                next.expeditionStage++;
            }
            else if (r.action == "milk" || r.action == "eggs")
            {
                bool cow = r.action == "milk";
                float x = cow ? -99 : -108, z = -226;
                if (!Near(player, x, ground(x,z), z, 7)) return "Move closer to the animal.";
                long ready = cow ? next.cowReady : next.henReady;
                if (now < ready) return "They are resting. Come back in a little while.";
                if (cow) { next.milk++; next.cowReady = now + 120; }
                else { next.eggs += 3; next.henReady = now + 90; }
            }
            else if (r.action == "place" || r.action == "move" || r.action == "remove")
            {
                if(r.action=="place" && r.id!=0)return "New pieces cannot replace an existing ID.";
                Piece existing = r.action=="place"?null:next.pieces.Find(p => p.id == r.id);
                if(r.action=="move" && existing!=null && r.kind!=existing.kind)return "Moving a piece must preserve its type.";
                if (r.action != "place" && existing == null) return "That piece has already changed.";
                if (existing != null && !Near(player,existing.x*3,ground(existing.x*3,existing.z*3)+existing.level*3,existing.z*3,15)) return "Move closer to that piece.";
                if (r.action == "remove")
                {
                    if (existing.kind == "foundation" && next.pieces.Any(p => p.x == existing.x && p.z == existing.z && p.level == existing.level && p.kind != "foundation" && p.kind != "flower" && p.kind != "fence")) return "Remove the walls and roof first.";
                    next.pieces.Remove(existing);
                }
                else
                {
                    string invalid=PlacementReason(next,r,player,ground);
                    if(invalid!="")return invalid;
                    if(existing!=null)next.pieces.Remove(existing);
                    next.pieces.Add(new Piece { id=existing?.id ?? next.nextId++, kind=r.kind,x=r.x,z=r.z,level=0,turn=r.turn });
                }
            }
            else return "Unknown action.";
            next.revision++;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp=path+".tmp";
            using (var file = new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            using (var writer = new StreamWriter(file)) { writer.Write(encode(next)); writer.Flush(); file.Flush(true); }
            if (File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
            state=next;
            return "";
        }
        public const float HomeZ=-235, ExpeditionZ=135;
        public static readonly float HomeX=-65+(float)Math.Sin(HomeZ*.013f)*18+3, ExpeditionX=-65+(float)Math.Sin(ExpeditionZ*.013f)*18;
        public static string PlacementReason(RanchState state, Request r, Visitor player, Func<float,float,float> ground)
        {
            if(state==null)return "Connecting to the ranch.";
            Piece existing=r.action=="move"?state.pieces.Find(p=>p.id==r.id):null;
            if(r.action=="move" && (existing==null || existing.kind!=r.kind))return "That piece has changed.";
            if (!Kinds.Contains(r.kind) || Math.Abs((long)r.x)>140 || Math.Abs((long)r.z)>140 || r.level != 0 || r.turn<0 || r.turn>3) return "That placement is outside this valley's building rules.";
            if(r.kind=="alpineflower" && state.expeditionStage<3)return "Finish the picnic expedition to bring home alpine flowers.";
            float x=r.x*3,z=r.z*3,y=ground(x,z);
            if(!Near(player,x,y,z,15))return "Build within 15 metres.";
            if(y<13 || Math.Abs(ground(x+1.5f,z)-ground(x-1.5f,z))>2 || Math.Abs(ground(x,z+1.5f)-ground(x,z-1.5f))>2)return "Find dry, gentler ground.";
            if((x+99)*(x+99)+(z+226)*(z+226)<25 || (x+108)*(x+108)+(z+226)*(z+226)<16)return "Leave a little room for the animals.";
            if(existing!=null && existing.kind=="foundation" && state.pieces.Any(p=>p.id!=existing.id && p.x==existing.x && p.z==existing.z && (p.kind=="wall" || p.kind=="door" || p.kind=="roof")))return "Move the walls and roof first.";
            bool edge=r.kind=="wall" || r.kind=="door" || r.kind=="fence";
            bool prop=IsProp(r.kind);
            if(state.pieces.Any(p=>p.id!=r.id && p.x==r.x && p.z==r.z && ((edge && (p.kind=="wall" || p.kind=="door" || p.kind=="fence") && p.turn==r.turn) || (!edge && p.kind==r.kind) || (prop && IsProp(p.kind)))))return "There is already a piece in that slot.";
            if((r.kind=="wall" || r.kind=="door" || r.kind=="roof") && !state.pieces.Any(p=>p.x==r.x && p.z==r.z && p.kind=="foundation"))return "Place a foundation first.";
            if(existing==null && state.pieces.Count>=4000)return "This valley has reached its 4,000-piece limit.";
            return "";
        }
        static bool IsProp(string kind)=>kind=="bench" || kind=="lantern" || kind=="trough" || kind=="flowerbox" || kind=="campfire";
        static bool Near(Visitor p,float x,float y,float z,float distance) => Finite(p.x)&&Finite(p.y)&&Finite(p.z) && (p.x-x)*(p.x-x)+(p.y-y)*(p.y-y)+(p.z-z)*(p.z-z)<=distance*distance;
        public static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
