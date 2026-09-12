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
        readonly List<float> rabbitClocks=new List<float>();
        readonly List<Mesh> wingMeshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        Mesh canopy;
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
                var rabbit=new GameObject("Cottontail rabbit");rabbit.transform.SetParent(transform);Vector3 home=ValleyShape.Ground(-85-i*4,-206+i*9);rabbit.transform.position=home;homes.Add(home);rabbits.Add(rabbit.transform);rabbitClocks.Add(i * .41f);
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
                foreach(int sign in new[]{-1,1})
                {
                    var wing=new GameObject("Shoulder wing");wing.transform.SetParent(bird.transform,false);wing.transform.localPosition=new Vector3(sign*.035f,0,.015f);
                    MakeWing(wing.transform, new[]{Vector3.zero,new Vector3(sign*.19f,0,-.055f),new Vector3(sign*.15f,0,-.13f),new Vector3(0,0,-.08f)},birdMat);
                    var tip=new GameObject("Flexible wingtip");tip.transform.SetParent(wing.transform,false);tip.transform.localPosition=new Vector3(sign*.19f,0,-.055f);
                    MakeWing(tip.transform,new[]{Vector3.zero,new Vector3(sign*.17f,0,-.14f),new Vector3(-sign*.04f,0,-.075f)},birdMat);
                }
            }
        }
        void MakeWing(Transform parent,Vector3[] points,Material material)
        {
            var mesh=new Mesh{name="Swept swallow feather silhouette"};mesh.vertices=points;
            mesh.triangles=points.Length==4?new[]{0,1,2,0,2,3,2,1,0,3,2,0}:new[]{0,1,2,2,1,0};
            mesh.RecalculateNormals();mesh.RecalculateBounds();wingMeshes.Add(mesh);
            parent.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;parent.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        void Update()
        {
            for(int i=0;i<rabbits.Count;i++)
            {
                float cycle=Mathf.Repeat(Time.time+i*1.7f,9);
                bool near=walker!=null&&Vector3.SqrMagnitude(walker.transform.position-rabbits[i].position)<16;
                bool moving=cycle<2.4f&&!near;
                if(moving)rabbitClocks[i]+=Time.deltaTime;
                float phase=rabbitClocks[i]*.4f+i*2;Vector3 home=homes[i];float x=home.x+Mathf.Sin(phase)*3,z=home.z+Mathf.Cos(phase*.8f)*3;
                float hop=moving?Mathf.Pow(Mathf.Max(0,Mathf.Sin(rabbitClocks[i]*9)),2)*.09f:0;
                rabbits[i].position=ValleyShape.Ground(x,z,hop);
                if(moving)rabbits[i].rotation=Quaternion.Euler(0,Mathf.Atan2(Mathf.Cos(phase),-.8f*Mathf.Sin(phase*.8f))*Mathf.Rad2Deg,0);
            }
            for(int i=0;i<birds.Count;i++)
            {
                float t=Time.time*.2f+i*.78f;birds[i].position=ValleyShape.Ground(-70+Mathf.Sin(t)*35,-210+Mathf.Cos(t)*25,12+i*.7f);birds[i].rotation=Quaternion.Euler(0,(t+Mathf.PI/2)*Mathf.Rad2Deg,Mathf.Sin(Time.time*3+i)*12);
                float flap=Time.time*11+i;
                float flapping=AnimalMotion.IdleEnvelope(Time.time+i,7,1,4);
                for(int j=1;j<3;j++)
                {
                    Transform wing=birds[i].GetChild(j);float sign=j==1?-1:1;
                    wing.localRotation=Quaternion.Euler(0,0,sign*(Mathf.Sin(flap)*38*flapping+8));
                    wing.GetChild(0).localRotation=Quaternion.Euler(0,0,sign*Mathf.Sin(flap-.7f)*18*flapping);
                }
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
        void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var mesh in wingMeshes)Destroy(mesh);if(canopy!=null)Destroy(canopy);}
    }
}
