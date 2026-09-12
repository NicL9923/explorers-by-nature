using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ExplorersByNature.Shared
{
    public static class Wire
    {
        public static string Read(Stream stream, int limit = 1048576)
        {
            byte[] size=new byte[4]; ReadAll(stream,size);
            int length=IPAddress.NetworkToHostOrder(BitConverter.ToInt32(size,0));
            if (length<1 || length>limit) throw new InvalidDataException("Invalid frame length.");
            byte[] bytes=new byte[length]; ReadAll(stream,bytes); return Encoding.UTF8.GetString(bytes);
        }
        static void ReadAll(Stream stream,byte[] bytes) { int offset=0; while(offset<bytes.Length) { int n=stream.Read(bytes,offset,bytes.Length-offset); if(n==0) throw new EndOfStreamException(); offset+=n; } }
        public static void Write(Stream stream,string text)
        {
            byte[] bytes=Encoding.UTF8.GetBytes(text), size=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
            stream.Write(size,0,4); stream.Write(bytes,0,bytes.Length);
        }
    }
    public sealed class RanchServer : IDisposable
    {
        readonly TcpListener listener;
        readonly Ranch ranch;
        readonly Func<object,string> encode;
        readonly Func<string,Request> decode;
        readonly string token;
        readonly Dictionary<TcpClient,Visitor> clients = new Dictionary<TcpClient,Visitor>();
        readonly object gate=new object();
        volatile bool running=true;
        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public RanchServer(IPAddress address,int port,string token,Ranch ranch,Func<object,string> encode,Func<string,Request> decode)
        {
            this.ranch=ranch; this.encode=encode; this.decode=decode; this.token=token;
            listener=new TcpListener(address,port);
            try { listener.Start(20);new Thread(Accept) { IsBackground=true,Name="Ranch connections" }.Start(); }
            catch { listener.Stop();ranch.Dispose();throw; }
        }
        void Accept()
        {
            while(running) try
            {
                TcpClient client=listener.AcceptTcpClient(); client.NoDelay=true; client.ReceiveTimeout=10000;client.SendTimeout=2000;
                lock(gate) { if(clients.Count>=20) { client.Close(); continue; } clients.Add(client,null); }
                new Thread(()=>Serve(client)) { IsBackground=true,Name="Ranch visitor" }.Start();
            }
            catch(SocketException) { if(!running) return; }
            catch(ObjectDisposedException) { return; }
        }
        void Serve(TcpClient client)
        {
            try
            {
                using(NetworkStream stream=client.GetStream())
                {
                    Request hello=decode(Wire.Read(stream,4096));
                    if(hello==null || hello.protocol!=1 || hello.action!="join" || hello.token!=token)
                    { Wire.Write(stream,encode(new Reply { message="Wrong join code or incompatible game version." })); return; }
                    Visitor player=new Visitor {id=Guid.NewGuid().ToString("N"),name=CleanName(hello.name),x=-69,y=30,z=-235};
                    lock(gate) clients[client]=player;
                    Wire.Write(stream,encode(new Reply {ok=true,playerId=player.id,message="Connected"}));
                    DateTime window=DateTime.UtcNow; int requests=0;
                    while(running)
                    {
                        Request request=decode(Wire.Read(stream,4096));
                        if(request==null) break;
                        if((DateTime.UtcNow-window).TotalSeconds>=1) { requests=0; window=DateTime.UtcNow; }
                        if(++requests>30) break;
                        Reply reply;
                        lock(gate)
                        {
                            if(!running) break;
                            if(!Ranch.Finite(request.px)||!Ranch.Finite(request.py)||!Ranch.Finite(request.pz)||!Ranch.Finite(request.yaw)||Math.Abs(request.px)>450||Math.Abs(request.pz)>450||request.py<0||request.py>400) break;
                            player.x=request.px;player.y=request.py;player.z=request.pz;player.yaw=request.yaw;
                            long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            string error;
                            try { error=ranch.Apply(request,player,now); }
                            catch(IOException) { error="Save failed. No change was made; check server storage."; }
                            catch(UnauthorizedAccessException) { error="Save failed. Check server folder permissions."; }
                            reply=new Reply {hasState=request.revision!=ranch.Revision,action=request.action,ok=error.Length==0,message=error,utc=now,playerId=player.id,state=request.revision!=ranch.Revision?ranch.Snapshot:null,players=clients.Values.Where(p=>p!=null).Select(p=>new Visitor {id=p.id,name=p.name,x=p.x,y=p.y,z=p.z,yaw=p.yaw}).ToArray()};
                        }
                        Wire.Write(stream,encode(reply));
                    }
                }
            }
            catch(Exception) { /* A malformed frame or disconnected peer ends only this connection. */ }
            finally { lock(gate) clients.Remove(client); client.Close(); }
        }
        static string CleanName(string name) => new string((name??"Explorer").Where(c=>char.IsLetterOrDigit(c)||c==' '||c=='_').Take(24).ToArray());
        public void Dispose() { running=false;listener.Stop();lock(gate) { foreach(var client in clients.Keys.ToArray()) client.Close(); ranch.Dispose(); } }
    }
}
