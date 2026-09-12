using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ExplorersByNature.Shared;
var json=new JsonSerializerOptions {IncludeFields=true};
string Encode(object o)=>JsonSerializer.Serialize(o,json);
T Decode<T>(string s)=>JsonSerializer.Deserialize<T>(s,json);
string dir=Path.Combine(Path.GetTempPath(),"explorers-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);
Ranch Make()=>new Ranch(Path.Combine(dir,"ranch.json"),Encode,Decode<RanchState>,(x,z)=>20);
TcpClient Join(int port) { var c=new TcpClient("127.0.0.1",port);c.ReceiveTimeout=5000;Wire.Write(c.GetStream(),Encode(new Request {action="join",token="test-code",name="Visitor"}));Check(Decode<Reply>(Wire.Read(c.GetStream())).ok,"join");return c; }
Reply Send(TcpClient c,Request r) { r.px=-99;r.py=20;r.pz=-226;Wire.Write(c.GetStream(),Encode(r));return Decode<Reply>(Wire.Read(c.GetStream())); }
void Check(bool condition,string label) { if(!condition)throw new Exception("FAIL: "+label);Console.WriteLine("PASS: "+label); }
try
{
 using(var server=new RanchServer(IPAddress.Loopback,0,"test-code",Make(),Encode,Decode<Request>))
 {
  using var a=Join(server.Port);using var b=Join(server.Port);
  bool locked=false;try{using var duplicate=Make();}catch(IOException){locked=true;}Check(locked,"second writer cannot open the same save");
  using(var malformed=new TcpClient("127.0.0.1",server.Port)){malformed.ReceiveTimeout=2000;Wire.Write(malformed.GetStream(),"{broken json");Check(malformed.GetStream().ReadByte()==-1,"malformed JSON disconnects only its sender");}
  var requests=new[]{a,b}.Select(c=>Task.Run(()=>Send(c,new Request {action="place",kind="foundation",x=-30,z=-74}))).ToArray();
  await Task.WhenAll(requests);Check(requests.Count(t=>t.Result.ok)==1,"conflicting placement commits once");
  int foundation=requests.First(t=>t.Result.ok).Result.state.pieces[0].id;
  Check(!Send(a,new Request{action="place",id=foundation,kind="flower",x=-30,z=-74}).ok,"placement cannot overwrite an existing ID");
  Check(!Send(a,new Request{action="move",id=foundation,kind="flower",x=-30,z=-74}).ok,"move cannot change a piece type");
  var eggs=new[]{a,b}.Select(c=>Task.Run(()=>{var r=new Request {action="eggs",px=-108,py=20,pz=-226};Wire.Write(c.GetStream(),Encode(r));return Decode<Reply>(Wire.Read(c.GetStream()));})).ToArray();
  await Task.WhenAll(eggs);Check(eggs.Count(t=>t.Result.ok)==1,"concurrent collection grants once");
  Check(!Send(a,new Request {action="place",kind="roof",x=-31,z=-73}).ok,"unsupported roof rejected");
  Check(!Send(a,new Request {action="place",kind="foundation",x=int.MinValue,z=0}).ok,"hostile coordinate rejected");
  var peers=new List<TcpClient>();for(int i=0;i<18;i++)peers.Add(Join(server.Port));
  await Task.WhenAll(peers.Select(c=>Task.Run(()=>{for(int i=0;i<10;i++){var r=Send(c,new Request {action="poll"});if(r.players.Length!=20)throw new Exception("Missing visitors");Thread.Sleep(110);}})));
  Check(true,"20 concurrent visitors poll consistent world");foreach(var c in peers)c.Dispose();
  using var oversized=new TcpClient("127.0.0.1",server.Port);oversized.ReceiveTimeout=2000;oversized.GetStream().Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(9999999)));Check(oversized.GetStream().ReadByte()==-1,"oversized frame disconnected");
 }
 using(var server=new RanchServer(IPAddress.Loopback,0,"test-code",Make(),Encode,Decode<Request>))
 {
  using var c=Join(server.Port);var reply=Send(c,new Request {action="poll"});
  Check(reply.state.pieces.Count==1 && reply.state.eggs==3 && reply.state.revision==2,"restart and reconnect preserve world and products");
  Wire.Write(c.GetStream(),Encode(new Request {action="eggs",px=-108,py=20,pz=-226}));Check(!Decode<Reply>(Wire.Read(c.GetStream())).ok,"production cooldown survives restart");
  using var bad=new TcpClient("127.0.0.1",server.Port);Wire.Write(bad.GetStream(),Encode(new Request {action="join",token="wrong"}));Check(!Decode<Reply>(Wire.Read(bad.GetStream())).ok,"wrong join code rejected");
 }
 File.WriteAllText(Path.Combine(dir,"ranch.json"),"{}");bool incomplete=false;try{using var empty=Make();}catch(InvalidDataException){incomplete=true;}Check(incomplete,"missing save fields never silently reset");
 File.WriteAllText(Path.Combine(dir,"ranch.json"),"null");bool corrupt=false;try{Make();}catch(InvalidDataException){corrupt=true;}Check(corrupt,"damaged save never silently resets");
}
finally { Directory.Delete(dir,true); }
