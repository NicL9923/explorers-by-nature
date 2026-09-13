using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    // Only immutable JSON crosses the worker/main-thread seam.
    public sealed class RanchConnection : IDisposable
    {
        readonly ConcurrentQueue<string> commands=new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> replies=new ConcurrentQueue<string>();
        readonly string host,code,name;
        readonly int port;
        volatile bool running=true;
        string position="";
        TcpClient socket;
        public string Status {get;private set;}="Joining...";
        public bool Connected {get;private set;}
        public Reply Latest {get;private set;}
        public RanchState State {get;private set;}
        public string Message {get;private set;}="";
        public RanchConnection(string host,int port,string code,string name)
        { this.host=host;this.port=port;this.code=code;this.name=name;new Thread(Run){IsBackground=true,Name="Ranch client"}.Start(); }
        public void Send(Request request)
        {
            if(!Connected || commands.Count>=8)return;
            if(request.action=="fire")
            {
                // A shot's aim and origin describe the same instant, even if the
                // worker sends it after the player has moved or mounted a horse.
                string pose=Interlocked.CompareExchange(ref position,null,null);
                if(string.IsNullOrEmpty(pose))return;
                CopyPose(request,JsonUtility.FromJson<Request>(pose));
            }
            commands.Enqueue(JsonUtility.ToJson(request));
        }
        static void CopyPose(Request request,Request pose)
        {request.px=pose.px;request.py=pose.py;request.pz=pose.pz;request.yaw=pose.yaw;request.model=pose.model;request.mounted=pose.mounted;}
        public void SendShot(int targetId,Vector3 aim,Vector3 origin,float yaw,string model=PlayerModels.Default,bool mounted=false)
        {
            if(!Connected || commands.Count>=8)return;
            // Equipment supplies its exact click pose; Update ordering must not
            // make the shot depend on when the last regular Tick occurred.
            commands.Enqueue(JsonUtility.ToJson(new Request{action="fire",id=targetId,ax=aim.x,ay=aim.y,az=aim.z,
                px=origin.x,py=origin.y,pz=origin.z,yaw=yaw,model=PlayerModels.Normalize(model),mounted=mounted}));
        }
        public void Tick(Vector3 p,float yaw,string model=PlayerModels.Default,bool mounted=false)
        {
            Interlocked.Exchange(ref position,JsonUtility.ToJson(new Request{px=p.x,py=p.y,pz=p.z,yaw=yaw,model=PlayerModels.Normalize(model),mounted=mounted}));
            while(replies.TryDequeue(out string json))
            {
                if(json.StartsWith("!")) { Connected=false;Status=json.Substring(1);continue; }
                Reply reply=JsonUtility.FromJson<Reply>(json);Latest=reply;
                if(reply.hasState && reply.state!=null)State=reply.state;
                Connected=true;Status="Connected · "+(reply.players?.Length??1)+" explorers";
                if(!string.IsNullOrEmpty(reply.message)||(!string.IsNullOrEmpty(reply.action)&&reply.action!="poll"))Message=reply.message??"";
            }
        }
        void Run()
        {
            while(running)
            {
                try
                {
                    socket=new TcpClient();socket.NoDelay=true;socket.ReceiveTimeout=5000;socket.SendTimeout=3000;
                    var connect=socket.ConnectAsync(host,port);if(!connect.Wait(4000))throw new TimeoutException();connect.GetAwaiter().GetResult();
                    using(var stream=socket.GetStream())
                    {
                        Wire.Write(stream,JsonUtility.ToJson(new Request{action="join",token=code,name=name}));
                        Reply hello=JsonUtility.FromJson<Reply>(Wire.Read(stream));
                        if(!hello.ok) { replies.Enqueue("!"+hello.message);return; }
                        int revision=-1;
                        while(running)
                        {
                            string pose=Interlocked.CompareExchange(ref position,null,null);
                            if(string.IsNullOrEmpty(pose)) { Thread.Sleep(50);continue; }
                            Request p=JsonUtility.FromJson<Request>(pose);
                            Request request=commands.TryDequeue(out string command)?JsonUtility.FromJson<Request>(command):new Request{action="poll"};
                            if(request.action!="fire")CopyPose(request,p);
                            request.revision=revision;
                            Wire.Write(stream,JsonUtility.ToJson(request));string json=Wire.Read(stream);
                            Reply reply=JsonUtility.FromJson<Reply>(json);if(reply.hasState && reply.state!=null)revision=reply.state.revision;
                            if(replies.Count>32)throw new InvalidOperationException("Client stalled");
                            replies.Enqueue(json);Thread.Sleep(150);
                        }
                    }
                }
                catch(Exception) { if(running)replies.Enqueue("!Disconnected. Reconnecting... (edits paused)"); }
                finally { socket?.Close();while(commands.TryDequeue(out _)){} }
                for(int i=0;i<20 && running;i++)Thread.Sleep(100);
            }
        }
        public void Dispose() { running=false;socket?.Close(); }
    }
}
