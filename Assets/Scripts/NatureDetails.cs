using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class NatureDetails : MonoBehaviour
    {
        readonly List<Transform> rabbits=new List<Transform>();
        readonly List<Transform> birds=new List<Transform>();
        readonly List<Vector3> homes=new List<Vector3>();
        readonly List<Material> materials=new List<Material>();
        Mesh canopy;
        AudioClip ambience;
        FirstPersonWalker walker;
        string discovery="";
        float discoveryUntil;
        Material Material(string name,Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",.1f);materials.Add(m);return m; }
        void Start()
        {
            walker=FindFirstObjectByType<FirstPersonWalker>();
            Material bark=Material("Aspen bark",new Color(.57f,.55f,.43f)),leaf=Material("Aspen leaves",new Color(.22f,.38f,.08f)),fur=Material("Cottontail",new Color(.36f,.27f,.17f)),birdMat=Material("Swallow",new Color(.12f,.14f,.18f));
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);Mesh source=sphere.GetComponent<MeshFilter>().sharedMesh;Destroy(sphere);
            var random=new System.Random(637);
            var combine=new CombineInstance[7];
            for(int i=0;i<combine.Length;i++)combine[i]=new CombineInstance{mesh=source,transform=Matrix4x4.TRS(new Vector3(Mathf.Sin(i*2.4f)*1.5f,i*.5f,Mathf.Cos(i*2.4f)*1.4f),Quaternion.identity,new Vector3(2.8f,2.3f,2.7f))};
            canopy=new Mesh{name="Clustered broadleaf crown"};canopy.CombineMeshes(combine);canopy.RecalculateBounds();
            for(int i=0;i<120;i++)
            {
                float x=-210+(float)random.NextDouble()*230,z=-300+(float)random.NextDouble()*340;
                if(Mathf.Abs(x-ValleyShape.TrailX(z))<10||Mathf.Abs(x-ValleyShape.RiverX(z))<25||Vector2.Distance(new Vector2(x,z),new Vector2(-98,-232))<24)continue;
                var tree=new GameObject("Aspen");tree.transform.SetParent(transform);tree.transform.position=ValleyShape.Ground(x,z);float height=6+(float)random.NextDouble()*5;
                GameObject imported = ModelArt.Tree("Woodland/Aspen", tree.transform);
                if (imported != null)
                {
                    imported.transform.localScale = Vector3.one * height / Mathf.Max(1, ValleyWorld.ModelHeight(imported));
                    var collision = tree.AddComponent<CapsuleCollider>(); collision.center = Vector3.up * height * .4f; collision.height = height * .8f; collision.radius = .16f;
                    continue;
                }
                RanchVisuals.Part(tree.transform,"Trunk",PrimitiveType.Cylinder,new Vector3(0,height*.45f,0),new Vector3(.3f,height*.45f,.3f),bark);
                var crown=new GameObject("Crown");crown.transform.SetParent(tree.transform,false);crown.transform.localPosition=Vector3.up*(height-2);crown.AddComponent<MeshFilter>().sharedMesh=canopy;crown.AddComponent<MeshRenderer>().sharedMaterial=leaf;
                var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,tree.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            }
            for(int i=0;i<6;i++)
            {
                var rabbit=new GameObject("Cottontail rabbit");rabbit.transform.SetParent(transform);Vector3 home=ValleyShape.Ground(-85-i*4,-206+i*9);rabbit.transform.position=home;homes.Add(home);rabbits.Add(rabbit.transform);
                if (ModelArt.Instantiate("Wildlife/Rabbit", rabbit.transform, false) != null) continue;
                RanchVisuals.Part(rabbit.transform,"Body",PrimitiveType.Sphere,new Vector3(0,.25f,0),new Vector3(.32f,.38f,.5f),fur,false);
                RanchVisuals.Part(rabbit.transform,"Head",PrimitiveType.Sphere,new Vector3(0,.44f,.2f),Vector3.one*.26f,fur,false);
                foreach(float x in new[]{-.075f,.075f})RanchVisuals.Part(rabbit.transform,"Ear",PrimitiveType.Capsule,new Vector3(x,.64f,.2f),new Vector3(.075f,.2f,.07f),fur,false);
                RanchVisuals.Part(rabbit.transform,"Tail",PrimitiveType.Sphere,new Vector3(0,.29f,-.24f),Vector3.one*.13f,bark,false);
            }
            for(int i=0;i<8;i++)
            {
                var bird=new GameObject("Swallow");bird.transform.SetParent(transform);birds.Add(bird.transform);
                RanchVisuals.Part(bird.transform,"Body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.1f,.08f,.28f),birdMat,false);
                foreach(int sign in new[]{-1,1})RanchVisuals.Part(bird.transform,"Wing",PrimitiveType.Cube,new Vector3(sign*.15f,0,0),new Vector3(.3f,.025f,.15f),birdMat,false);
            }
            CreateAmbience();
        }
        void CreateAmbience()
        {
            const int rate=22050,seconds=30;float[] samples=new float[rate*seconds];var rng=new System.Random(42);float wind=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate;wind=Mathf.Lerp(wind,(float)rng.NextDouble()*2-1,.012f);float chirp=0;
                for(int b=0;b<6;b++){float start=2+b*4.3f,d=t-start;if(d>0&&d<.65f){float env=Mathf.Sin(d/.65f*Mathf.PI);chirp+=Mathf.Sin(2*Mathf.PI*(2400*d+650*d*d))*env*Mathf.Pow(Mathf.Sin(d*38),2)*.022f;}}
                samples[i]=wind*.15f+chirp;
            }
            ambience=AudioClip.Create("Original wind and birds",samples.Length,1,rate,false);ambience.SetData(samples,0);
            var audio=gameObject.AddComponent<AudioSource>();audio.clip=ambience;audio.loop=true;audio.volume=PlayerPrefs.GetFloat("NatureVolume",.45f);audio.Play();
        }
        void Update()
        {
            for(int i=0;i<rabbits.Count;i++)
            {
                float phase=Time.time*.4f+i*2;Vector3 home=homes[i];float x=home.x+Mathf.Sin(phase)*3,z=home.z+Mathf.Cos(phase*.8f)*3;
                rabbits[i].position=ValleyShape.Ground(x,z,Mathf.Abs(Mathf.Sin(Time.time*5+i))*.12f);rabbits[i].rotation=Quaternion.Euler(0,Mathf.Atan2(Mathf.Cos(phase),-.8f*Mathf.Sin(phase*.8f))*Mathf.Rad2Deg,0);
            }
            for(int i=0;i<birds.Count;i++)
            {
                float t=Time.time*.2f+i*.78f;birds[i].position=ValleyShape.Ground(-70+Mathf.Sin(t)*35,-210+Mathf.Cos(t)*25,12+i*.7f);birds[i].rotation=Quaternion.Euler(0,(t+Mathf.PI/2)*Mathf.Rad2Deg,Mathf.Sin(Time.time*3+i)*12);
                for(int j=1;j<3;j++)birds[i].GetChild(j).localRotation=Quaternion.Euler(0,0,(j==1?-1:1)*Mathf.Sin(Time.time*11+i)*28);
            }
            if(walker==null||walker.Automated)return;
            Discover("MallardBend",ValleyShape.Ground(ValleyWorld.ShoreX(-150,-1)-3,-150),18,"Mallard bend · A good place to stop and watch the ducks.");
            Discover("FoxTrail",ValleyShape.Ground(-118,-178),12,"Fox trail · A quiet neighbor in the pines.");
            Discover("Riverbend",ValleyShape.Ground(0,-140),25,"Riverbend · Water, birdsong, and absolutely no appointments.");
            Discover("AspenGrove",ValleyShape.Ground(-150,-75),30,"Aspen grove · A quiet place for another little homestead.");
            Discover("Pinewatch",ValleyShape.Overlook,15,"Pinewatch Overlook · You made it. The view is yours.");
        }
        void Discover(string key,Vector3 position,float radius,string message)
        {if(PlayerPrefs.GetInt("Discovery."+key,0)==0&&Vector3.Distance(walker.transform.position,position)<radius){PlayerPrefs.SetInt("Discovery."+key,1);PlayerPrefs.Save();discovery=message;discoveryUntil=Time.time+12;}}
        void OnGUI(){if(Time.time<discoveryUntil){GUI.matrix=Matrix4x4.identity;GUI.Box(new Rect(Screen.width/2-260,25,520,55),discovery);}}
        void OnDestroy(){foreach(var m in materials)Destroy(m);if(canopy!=null)Destroy(canopy);if(ambience!=null)Destroy(ambience);}
    }
}
