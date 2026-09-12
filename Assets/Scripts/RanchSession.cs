using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class RanchSession : MonoBehaviour
    {
        public static bool PanelOpen {get;private set;}
        public RanchConnection Connection {get;private set;}
        FirstPersonWalker walker;
        RanchServer local;
        readonly Dictionary<int,GameObject> pieces=new Dictionary<int,GameObject>();
        readonly Dictionary<string,GameObject> visitors=new Dictionary<string,GameObject>();
        int revision=-1,selection,turn,movingId,strokes;
        bool building,book,milking;
        GameObject ghost;
        Piece preview;
        string host="127.0.0.1",port="7777",code="",playerName="Explorer",notice="Welcome home. B opens building; E cares for animals; Tab opens your journal.";
        float milkTime;
        Vector2 journalScroll;
        RanchTarget target;
        public string DataDirectory {get;set;}
        public string SavePath => Path.Combine(DataDirectory ?? Path.Combine(Application.persistentDataPath,"Ranches","Pinewatch"),"ranch.json");
        void Start()
        {
            walker=FindFirstObjectByType<FirstPersonWalker>();
            playerName=PlayerPrefs.GetString("ExplorerName","Explorer");host=PlayerPrefs.GetString("RanchHost","127.0.0.1");
            RanchVisuals.Animal(true,ValleyShape.Ground(-99,-226));RanchVisuals.Animal(false,ValleyShape.Ground(-108,-226));
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--ranch-host");
            if(at>=0&&at+1<args.Length)
            { host=args[at+1];int pi=Array.IndexOf(args,"--ranch-port"),ci=Array.IndexOf(args,"--ranch-code");int remotePort=pi>=0&&pi+1<args.Length&&int.TryParse(args[pi+1],out int n)?n:7777;code=ci>=0&&ci+1<args.Length?args[ci+1]:"";Connection=new RanchConnection(host,remotePort,code,playerName); }
            else StartSolo();
        }
        public void StartSolo()
        {
            Disconnect();
            try
            {
                byte[] heights=Resources.Load<TextAsset>("terrain").bytes;
                float Sample(float x,float z) { int ix=Math.Max(0,Math.Min(900,(int)Math.Round(x+450))),iz=Math.Max(0,Math.Min(900,(int)Math.Round(z+450)));return BitConverter.ToSingle(heights,(iz*901+ix)*4); }
                var ranch=new Ranch(SavePath,JsonUtility.ToJson,s=>JsonUtility.FromJson<RanchState>(s),Sample);
                local=new RanchServer(IPAddress.Loopback,0,"",ranch,JsonUtility.ToJson,s=>JsonUtility.FromJson<Request>(s));
                Connection=new RanchConnection("127.0.0.1",local.Port,"",playerName);notice="Your local ranch saves after every change.";
            }
            catch(Exception e) { notice="Could not open ranch: "+e.Message;Debug.LogException(e); }
        }
        void Disconnect()
        { Connection?.Dispose();Connection=null;local?.Dispose();local=null;revision=-1;foreach(var p in pieces.Values)Destroy(p);pieces.Clear();foreach(var p in visitors.Values)Destroy(p);visitors.Clear(); }
        void OnDestroy(){Disconnect();PanelOpen=false;}
        void Update()
        {
            if(walker==null)return;
            Connection?.Tick(walker.transform.position,walker.transform.eulerAngles.y);
            if(Connection?.State!=null && revision!=Connection.State.revision)Refresh();
            if(Connection?.Latest?.players!=null)UpdateVisitors();
            if(walker.Automated) { if(ghost!=null)ghost.SetActive(false);return; }
            if(Input.GetKeyDown(KeyCode.Tab)) {book=!book;milking=false;walker.SetMenu(book);}
            if((book||milking)&&!walker.MenuOpen){book=false;milking=false;}
            PanelOpen=book||milking;
            if(milking)
            {
                milkTime+=Time.deltaTime;
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    float t=Mathf.PingPong(milkTime*.65f,1);
                    if(t>.28f && t<.72f) {strokes++;notice="Nice and gentle.";}else notice="No rush. Press Space inside the green band.";
                    if(strokes>=3) {Send(new Request{action="milk"});milking=false;PanelOpen=false;walker.SetMenu(false);notice="Fresh milk for the pantry.";}
                }
                return;
            }
            if(walker.MenuOpen){if(ghost!=null)ghost.SetActive(false);return;}
            target=null;
            if(Physics.Raycast(walker.view.transform.position,walker.view.transform.forward,out RaycastHit hit,15))target=hit.collider.GetComponentInParent<RanchTarget>();
            if(Input.GetKeyDown(KeyCode.B)){building=!building;movingId=0;}
            if(Input.GetKeyDown(KeyCode.E)&&target!=null&&!string.IsNullOrEmpty(target.animal)&&hit.distance<6&&Connection?.Connected==true)
            {
                long ready=target.animal=="milk"?Connection.State.cowReady:Connection.State.henReady;
                if(ready>Connection.Latest.utc) {notice="Resting for another "+(ready-Connection.Latest.utc)+" seconds.";return;}
                if(target.animal=="milk"){milking=true;PanelOpen=true;strokes=0;milkTime=0;walker.SetMenu(true);}else{Send(new Request{action="eggs"});notice="Checking the nesting box.";}
            }
            if(building)
            {
                for(int i=0;i<6;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i)){selection=i;movingId=0;}
                if(Input.GetKeyDown(KeyCode.R))turn=(turn+1)%4;
                if(Input.GetKeyDown(KeyCode.M)&&target!=null&&target.pieceId>0)
                {var p=Connection?.State?.pieces.Find(v=>v.id==target.pieceId);if(p!=null){movingId=p.id;selection=Array.IndexOf(Ranch.Kinds,p.kind);turn=p.turn;}}
                if(Input.GetMouseButtonDown(1)&&target!=null&&target.pieceId>0)Send(new Request{action="remove",id=target.pieceId});
                Ray ray=new Ray(walker.view.transform.position,walker.view.transform.forward);
                var terrain=FindFirstObjectByType<ValleyWorld>().Ground.GetComponent<TerrainCollider>();
                if(terrain.Raycast(ray,out RaycastHit groundHit,15))
                {
                    var next=new Piece{kind=Ranch.Kinds[selection],x=Mathf.RoundToInt(groundHit.point.x/3),z=Mathf.RoundToInt(groundHit.point.z/3),turn=turn};
                    if(preview==null||next.kind!=preview.kind||next.x!=preview.x||next.z!=preview.z||next.turn!=preview.turn)
                    {if(ghost!=null)Destroy(ghost);ghost=RanchVisuals.Piece(next,true);foreach(var r in ghost.GetComponentsInChildren<Renderer>()){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.25f,.7f,.65f));r.SetPropertyBlock(block);}preview=next;}
                    if(ghost!=null)ghost.SetActive(true);
                    if(Input.GetMouseButtonDown(0)) {Send(new Request{action=movingId>0?"move":"place",id=movingId,kind=next.kind,x=next.x,z=next.z,turn=next.turn});movingId=0;}
                }
                else if(ghost!=null)ghost.SetActive(false);
            }
            else if(ghost!=null)ghost.SetActive(false);
        }
        void Send(Request r){if(Connection?.Connected==true)Connection.Send(r);else notice="Reconnect before changing the ranch.";}
        void Refresh()
        {
            var state=Connection.State;var keep=new HashSet<int>();
            foreach(var p in state.pieces)
            {
                keep.Add(p.id);string stamp=p.kind+":"+p.x+":"+p.z+":"+p.turn;
                if(pieces.TryGetValue(p.id,out var old)&&old.name==stamp)continue;
                if(old!=null)Destroy(old);var go=RanchVisuals.Piece(p);go.name=stamp;pieces[p.id]=go;
            }
            var removed=new List<int>();foreach(var p in pieces)if(!keep.Contains(p.Key)){Destroy(p.Value);removed.Add(p.Key);}foreach(int id in removed)pieces.Remove(id);
            Physics.SyncTransforms();
            revision=state.revision;
        }
        void UpdateVisitors()
        {
            var keep=new HashSet<string>();
            foreach(var v in Connection.Latest.players)
            {
                if(v.id==Connection.Latest.playerId)continue;keep.Add(v.id);
                if(!visitors.TryGetValue(v.id,out var go)){go=RanchVisuals.Explorer(v.name);go.transform.position=new Vector3(v.x,v.y,v.z);visitors[v.id]=go;}
                go.SetActive(Vector3.Distance(new Vector3(v.x,v.y,v.z),walker.transform.position)>.8f);
                go.transform.position=Vector3.Lerp(go.transform.position,new Vector3(v.x,v.y,v.z),1-Mathf.Exp(-Time.deltaTime*12));go.transform.rotation=Quaternion.Slerp(go.transform.rotation,Quaternion.Euler(0,v.yaw,0),Time.deltaTime*12);
            }
            var removed=new List<string>();foreach(var p in visitors)if(!keep.Contains(p.Key)){Destroy(p.Value);removed.Add(p.Key);}foreach(var id in removed)visitors.Remove(id);
        }
        void OnGUI()
        {
            if(walker==null||walker.Automated)return;
            float scale=Mathf.Clamp(Screen.height/900f,1f,1.6f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            GUI.Box(new Rect(20,130,470,105),GUIContent.none);
            GUI.Label(new Rect(32,138,445,22),(Connection?.Status??"Offline")+"  |  Milk "+(Connection?.State?.milk??0)+" · Eggs "+(Connection?.State?.eggs??0));
            GUI.Label(new Rect(32,162,445,22),building?"BUILD: 1 Floor  2 Wall  3 Doorway  4 Roof  5 Fence  6 Flowers":"B build · E interact · Tab journal / multiplayer");
            GUI.Label(new Rect(32,185,445,22),building?"Click place · Right click remove · R rotate · M move":"Clover and the hens live just west of the starting trail.");
            foreach(var visitor in visitors.Values)
            {
                if(!visitor.activeSelf)continue;Vector3 label=walker.view.WorldToScreenPoint(visitor.transform.position+Vector3.up*2.1f);label.x/=scale;label.y/=scale;
                if(label.z>0&&label.z<40)GUI.Label(new Rect(label.x-60,h-label.y,160,22),visitor.name);
            }
            if(!walker.MenuOpen)
            {
                GUI.Label(new Rect(w/2-5,h/2-12,20,25),"+");
                string prompt=target!=null&&!string.IsNullOrEmpty(target.animal)?(target.animal=="milk"?"E · Milk Clover":"E · Gather eggs"):"";
                GUI.Label(new Rect(w/2-130,h/2+25,300,30),prompt);
            }
            if(book)
            {
                GUILayout.BeginArea(new Rect(w/2-260,100,520,Mathf.Min(h-120,640)),GUI.skin.box);
                journalScroll=GUILayout.BeginScrollView(journalScroll);
                GUI.skin.label.wordWrap=true;
                GUILayout.Label("PINEWATCH · FIELD JOURNAL");GUILayout.Space(10);
                GUILayout.Label("A shared, peaceful place. Build freely, plant flowers, care for animals, or follow the trail north to the mountain overlook.");
                GUILayout.Label("Building: B toggles tools. Look at nearby ground. 1–6 select a piece; R rotates. Walls and roofs need a foundation. M picks up the piece you are looking at; click to move it. Remove walls/roof before their foundation.");
                GUILayout.Label("Discoveries: "+(PlayerPrefs.GetInt("Discovery.Riverbend",0)==1?"✓":"○")+" Riverbend (east, then north) · "+(PlayerPrefs.GetInt("Discovery.AspenGrove",0)==1?"✓":"○")+" Aspen grove (northwest) · "+(PlayerPrefs.GetInt("Discovery.Pinewatch",0)==1?"✓":"○")+" Pinewatch Overlook (follow the trail north)");
                GUILayout.Space(10);GUILayout.Label("PANTRY · "+(Connection?.State?.milk??0)+" milk / "+(Connection?.State?.eggs??0)+" eggs");
                GUILayout.Label("Clover rests 2 minutes after milking; the nesting box refills after 90 seconds. Nothing suffers while you are away.");
                GUILayout.Space(10);GUILayout.Label("PRIVATE SERVER");
                GUILayout.Label("Your name");playerName=GUILayout.TextField(playerName,24);
                GUILayout.Label("Address (trusted LAN or encrypted private network)");host=GUILayout.TextField(host,200);
                GUILayout.Label("Port");port=GUILayout.TextField(port,5);GUILayout.Label("Join code");code=GUILayout.PasswordField(code,'*',128);
                if(GUILayout.Button("Join server",GUILayout.Height(30)))
                {
                    if(int.TryParse(port,out int n)&&n>0&&n<=65535&&!string.IsNullOrWhiteSpace(host))
                    {Disconnect();Connection=new RanchConnection(host,n,code,playerName);PlayerPrefs.SetString("ExplorerName",playerName);PlayerPrefs.SetString("RanchHost",host);book=false;PanelOpen=false;walker.SetMenu(false);notice="Joining your shared ranch...";}else notice="Enter an address and a port from 1 to 65535.";
                }
                if(GUILayout.Button("Return to my solo ranch",GUILayout.Height(30))){StartSolo();book=false;PanelOpen=false;walker.SetMenu(false);}
                if(GUILayout.Button("Back to the valley",GUILayout.Height(30))){book=false;PanelOpen=false;walker.SetMenu(false);}
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            if(milking)
            {
                GUI.Box(new Rect(w/2-240,h/2-120,480,230),"MILKING CLOVER");
                GUI.Label(new Rect(w/2-205,h/2-85,410,50),"Press Space as the marker crosses the green band.\nThree gentle squeezes. Escape cancels.");
                Rect bar=new Rect(w/2-200,h/2-15,400,24);GUI.Box(bar,"");GUI.color=new Color(.4f,.8f,.4f);GUI.DrawTexture(new Rect(bar.x+112,bar.y,176,24),Texture2D.whiteTexture);GUI.color=Color.white;GUI.DrawTexture(new Rect(bar.x+Mathf.PingPong(milkTime*.65f,1)*394,bar.y-4,6,32),Texture2D.whiteTexture);
                GUI.Label(new Rect(w/2-200,h/2+30,400,30),strokes+" / 3 · "+notice);
            }
            GUI.Label(new Rect(20,h-95,Mathf.Min(w-40,900),30),string.IsNullOrEmpty(Connection?.Message)?notice:Connection.Message);
        }
    }
}
