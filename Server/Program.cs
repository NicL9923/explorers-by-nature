using System.Net;
using System.Text.Json;
using ExplorersByNature.Shared;
var options = new JsonSerializerOptions { IncludeFields=true };
string Arg(string key,string fallback) { int i=Array.IndexOf(args,key); return i>=0 && i+1<args.Length?args[i+1]:fallback; }
string data=Arg("--data",Path.Combine(AppContext.BaseDirectory,"world"));
Directory.CreateDirectory(data);
string code=Arg("--code",Environment.GetEnvironmentVariable("EXPLORERS_JOIN_CODE")??"");
string address=Arg("--listen","127.0.0.1");
if(address!="127.0.0.1" && code.Length<8) throw new ArgumentException("Remote servers require --code with at least 8 characters.");
string terrain=Arg("--terrain",Path.Combine(AppContext.BaseDirectory,"terrain.bytes"));
byte[] heights=File.ReadAllBytes(terrain);
float Ground(float x,float z) { int ix=Math.Clamp((int)Math.Round(x+450),0,900),iz=Math.Clamp((int)Math.Round(z+450),0,900);return BitConverter.ToSingle(heights,(iz*901+ix)*4); }
string Encode(object value)=>JsonSerializer.Serialize(value,options);
var ranch=new Ranch(Path.Combine(data,"ranch.json"),Encode,s=>JsonSerializer.Deserialize<RanchState>(s,options),Ground);
using var server=new RanchServer(IPAddress.Parse(address),int.Parse(Arg("--port","7777")),code,ranch,Encode,s=>JsonSerializer.Deserialize<Request>(s,options));
Console.WriteLine($"Pinewatch server listening on {address}:{server.Port}. Save: {data}. Remote play requires a trusted LAN or encrypted private network.");
using var stop=new ManualResetEventSlim();Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;stop.Set();};stop.Wait();
