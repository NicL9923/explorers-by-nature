using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class RanchTarget : MonoBehaviour { public int pieceId; public string animal; }
    public static class RanchVisuals
    {
        static Material wood,cream,dark,red,green,petal,nose,beak;
        static Material Mat(string name,Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",.15f);return m; }
        static void Materials()
        {
            if(wood!=null)return;
            wood=Mat("Cedar",new Color(.38f,.21f,.105f));cream=Mat("Warm ivory",new Color(.88f,.81f,.65f));dark=Mat("Iron and hooves",new Color(.065f,.05f,.037f));red=Mat("Hen comb",new Color(.65f,.09f,.055f));green=Mat("Flower stems",new Color(.13f,.32f,.06f));petal=Mat("Wildflowers",new Color(.58f,.32f,.72f));nose=Mat("Soft rose",new Color(.57f,.28f,.24f));beak=Mat("Beak",new Color(.75f,.4f,.055f));
        }
        public static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collider=true)
        { var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.Destroy(go.GetComponent<Collider>());return go; }
        public static GameObject Piece(Piece p,bool ghost=false)
        {
            Materials();var root=new GameObject(p.kind);root.transform.position=ValleyShape.Ground(p.x*3,p.z*3,p.kind=="flower"?.015f:.18f);root.transform.rotation=Quaternion.Euler(0,p.turn*90,0);
            if(!ghost)root.AddComponent<RanchTarget>().pieceId=p.id;
            string resource = p.kind == "foundation" ? "Foundation" : p.kind == "wall" ? "Wall" : p.kind == "door" ? "Door" : p.kind == "roof" ? "Roof" : "Fence";
            GameObject artwork = p.kind == "flower" ? ModelArt.Tree("Woodland/Wildflower", root.transform, false) : ModelArt.Instantiate("Homestead/" + resource, root.transform, false);
            if (artwork != null)
            {
                if (!ghost) PieceColliders(root, p.kind);
                return root;
            }
            void Box(string name,Vector3 pos,Vector3 size,Material mat)=>Part(root.transform,name,PrimitiveType.Cube,pos,size,mat,!ghost);
            switch(p.kind)
            {
                case "foundation":
                    for(int i=0;i<10;i++)Part(root.transform,"Floor plank",PrimitiveType.Cube,new Vector3(-1.35f+i*.3f,.35f,0),new Vector3(.29f,.2f,3),wood,false);
                    if(!ghost) {var floor=root.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.4f,0);floor.size=new Vector3(3,1.7f,3);}
                    foreach(int sign in new[]{-1,1}) { Box("Entry step",new Vector3(0,.02f,sign*1.9f),new Vector3(4.6f,.2f,.8f),wood);Box("Entry step",new Vector3(sign*1.9f,.02f,0),new Vector3(.8f,.2f,3),wood); }
                    foreach(float x in new[]{-1.3f,1.3f})foreach(float z in new[]{-1.3f,1.3f})Box("Pier",new Vector3(x,-.5f,z),new Vector3(.22f,1.7f,.22f),wood);
                    break;
                case "wall":
                    for(int i=0;i<10;i++)Box("Siding",new Vector3(0,.6f+i*.24f,1.45f),new Vector3(3,.235f,.14f),wood);
                    Box("Window shutter",new Vector3(0,1.7f,1.35f),new Vector3(.9f,.75f,.1f),cream);break;
                case "door":
                    Box("Left frame",new Vector3(-1,1.65f,1.45f),new Vector3(1,2.4f,.16f),wood);Box("Right frame",new Vector3(1,1.65f,1.45f),new Vector3(1,2.4f,.16f),wood);Box("Lintel",new Vector3(0,2.7f,1.45f),new Vector3(1,.3f,.16f),wood);break;
                case "roof":
                    foreach(int sign in new[]{-1,1}) { var roof=Part(root.transform,"Shingled roof",PrimitiveType.Cube,new Vector3(sign*.8f,3.15f,0),new Vector3(1.9f,.15f,3.4f),dark,!ghost);roof.transform.localRotation=Quaternion.Euler(0,0,-sign*25); }break;
                case "fence":
                    foreach(float x in new[]{-1.4f,1.4f})Box("Fence post",new Vector3(x,.7f,1.45f),new Vector3(.16f,1.7f,.16f),wood);
                    foreach(float y in new[]{.45f,1f})Box("Fence rail",new Vector3(0,y,1.45f),new Vector3(3,.13f,.12f),wood);break;
                case "flower":
                    for(int i=0;i<7;i++) { float x=Mathf.Sin(i*2.4f)*.55f,z=Mathf.Cos(i*2.4f)*.55f,h=.35f+(i%3)*.12f;Part(root.transform,"Stem",PrimitiveType.Cylinder,new Vector3(x,h/2,z),new Vector3(.025f,h/2,.025f),green,false);Part(root.transform,"Bloom",PrimitiveType.Sphere,new Vector3(x,h,z),new Vector3(.23f,.09f,.23f),i%2==0?petal:cream,false); }
                    if(!ghost) { var col=root.AddComponent<BoxCollider>();col.center=new Vector3(0,.3f,0);col.size=new Vector3(1.5f,.65f,1.5f); }break;
            }
            return root;
        }
        static void ColliderBox(Transform parent, Vector3 position, Vector3 size, float tilt = 0)
        {
            var shape = new GameObject("Collision");
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            shape.AddComponent<BoxCollider>().size = size;
        }

        static void PieceColliders(GameObject root, string kind)
        {
            switch (kind)
            {
                case "foundation":
                    var floor = root.AddComponent<BoxCollider>(); floor.center = new Vector3(0, -.4f, 0); floor.size = new Vector3(3, 1.7f, 3);
                    foreach (int sign in new[] { -1, 1 })
                    {
                        ColliderBox(root.transform, new Vector3(0, .02f, sign * 1.9f), new Vector3(4.6f, .2f, .8f));
                        ColliderBox(root.transform, new Vector3(sign * 1.9f, .02f, 0), new Vector3(.8f, .2f, 3));
                    }
                    break;
                case "wall":
                    foreach (int sign in new[] { -1, 1 }) ColliderBox(root.transform, new Vector3(sign * 1.015f, 1.65f, 1.45f), new Vector3(.97f, 2.4f, .18f));
                    ColliderBox(root.transform, new Vector3(0, .935f, 1.45f), new Vector3(1.06f, .97f, .18f));
                    ColliderBox(root.transform, new Vector3(0, 2.5125f, 1.45f), new Vector3(1.06f, .675f, .18f)); break;
                case "door":
                    foreach (int sign in new[] { -1, 1 }) ColliderBox(root.transform, new Vector3(sign, 1.65f, 1.45f), new Vector3(1, 2.4f, .18f));
                    ColliderBox(root.transform, new Vector3(0, 2.7f, 1.45f), new Vector3(1, .3f, .18f)); break;
                case "roof":
                    foreach (int sign in new[] { -1, 1 }) ColliderBox(root.transform, new Vector3(sign * .8f, 3.15f, 0), new Vector3(1.9f, .15f, 3.4f), -sign * 25); break;
                case "fence":
                    foreach (float x in new[] { -1.4f, 1.4f }) ColliderBox(root.transform, new Vector3(x, .7f, 1.45f), new Vector3(.18f, 1.7f, .18f));
                    foreach (float y in new[] { .45f, 1f }) ColliderBox(root.transform, new Vector3(0, y, 1.45f), new Vector3(3, .15f, .15f)); break;
                case "flower": var flowers = root.AddComponent<BoxCollider>(); flowers.center = new Vector3(0, .3f, 0); flowers.size = new Vector3(1.5f, .65f, 1.5f); break;
            }
        }

        public static GameObject Animal(bool cow,Vector3 position)
        {
            Materials();var root=new GameObject(cow?"Clover the cow":"Juniper's chicken coop");root.transform.position=position;root.AddComponent<RanchTarget>().animal=cow?"milk":"eggs";
            GameObject visual = ModelArt.Instantiate(cow ? "Clover" : "Hen", root.transform, false);
            if (visual != null) visual.transform.localPosition = Vector3.zero;
            else
            {
                Part(root.transform,"Body",PrimitiveType.Sphere,new Vector3(0,cow?1.1f:.4f,0),cow?new Vector3(.9f,1,1.7f):new Vector3(.5f,.6f,.65f),cream,false);
                Part(root.transform,"Head",PrimitiveType.Sphere,new Vector3(0,cow?1.5f:.72f,cow?.95f:.3f),cow?new Vector3(.6f,.65f,.8f):Vector3.one*.3f,cream,false);
                for(int i=0;i<4;i++)Part(root.transform,"Leg",PrimitiveType.Cylinder,new Vector3((i%2==0?-1:1)*(cow?.3f:.15f),cow?.4f:.14f,(i<2?-1:1)*(cow?.55f:.14f)),cow?new Vector3(.13f,.45f,.13f):new Vector3(.04f,.14f,.04f),dark,false);
            }
            var c=root.AddComponent<BoxCollider>();c.center=new Vector3(0,cow?1:.5f,0);c.size=cow?new Vector3(1,2,2):new Vector3(.7f,1,1);
            if(!cow)
            {
                GameObject coop = ModelArt.Instantiate("Homestead/Coop", root.transform, false);
                if (coop != null)
                {
                    coop.transform.localPosition = new Vector3(1.4f, 0, 0);
                    ColliderBox(root.transform, new Vector3(1.4f, .65f, 0), new Vector3(1.4f, 1.3f, 1.2f));
                }
                else Part(root.transform,"Nesting box",PrimitiveType.Cube,new Vector3(1,.35f,0),new Vector3(1,.7f,.9f),wood);
                for(int i=0;i<3;i++)Part(root.transform,"Egg",PrimitiveType.Sphere,new Vector3(.75f+i*.24f,.76f,0),new Vector3(.15f,.2f,.15f),cream,false);
                for(int i=0;i<2;i++)
                {
                    var hen=Object.Instantiate(root.transform.GetChild(0).gameObject,root.transform);
                    hen.transform.localPosition=new Vector3(-1-i*.65f,0,i*.5f);
                    ColliderBox(root.transform,hen.transform.localPosition+Vector3.up*.45f,new Vector3(.7f,.9f,.9f));
                }
            }
            return root;
        }
        public static GameObject Explorer(string name)
        { Materials();var root=new GameObject(name);Part(root.transform,"Coat",PrimitiveType.Capsule,new Vector3(0,.85f,0),new Vector3(.55f,.7f,.4f),green,false);Part(root.transform,"Face",PrimitiveType.Sphere,new Vector3(0,1.65f,0),Vector3.one*.35f,cream,false);Part(root.transform,"Hat brim",PrimitiveType.Cylinder,new Vector3(0,1.83f,0),new Vector3(.65f,.035f,.65f),wood,false);return root; }
    }
}
