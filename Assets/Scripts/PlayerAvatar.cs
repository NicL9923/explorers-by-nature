using System;
using System.Linq;
using ExplorersByNature.Shared;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class PlayerAvatar : MonoBehaviour
    {
        public static readonly string[] Ids={"ranch-hand","trail-scout","homesteader","frontiersman"};
        public static readonly string[] Names={"Ranch hand","Trail scout","Homesteader","Frontiersman"};
        public static readonly string[] Descriptions={"A weathered hat, work shirt and leather boots.","A trail-ready hat, neckerchief and practical riding clothes.","A cotton blouse, long skirt and a working apron.","A waistcoat, rolled sleeves and a broad-brimmed hat."};
        static readonly string[] Assets={"RanchHand","TrailScout","Homesteader","Frontiersman"};
        public string ModelId {get;private set;}
        public bool Preview {get;set;}
        public GameObject Model {get;private set;}
        bool mounted;GameObject horse;
        public void SetMounted(bool value,bool showHorse)
        {
            mounted=value;
            if(Model!=null)foreach(var part in Model.GetComponentsInChildren<Transform>(true)){if(part.name=="StandingSkirt")part.gameObject.SetActive(!value);if(part.name.StartsWith("RidingSkirt"))part.gameObject.SetActive(value);}
            if(value && showHorse && horse==null){horse=ModelArt.Instantiate("Horse/Horse",transform,false);if(horse!=null){horse.transform.localPosition=Vector3.down*HorseRiding.RiderHeight;horse.AddComponent<HorseMotion>();}}
            if(horse!=null)horse.SetActive(value && showHorse);
        }
        Transform[] joints;Quaternion[] rest;Vector3 previous;float phase, stride;
        public static int Index(string id)=>Array.IndexOf(Ids,PlayerModels.Normalize(id));
        public void SetModel(string id)
        {
            id=PlayerModels.Normalize(id);if(id==ModelId && Model!=null)return;
            if(Model!=null){Model.SetActive(false);Destroy(Model);}
            ModelId=id;Model=ModelArt.Instantiate("Players/"+Assets[Index(id)],transform,false);
            if(Model==null){Debug.LogError("Missing player model: "+id);return;}
            string[] names={"ArmL","ArmR","LegL","LegR","Torso","Head","KneeL","KneeR"};
            var all=Model.GetComponentsInChildren<Transform>();
            joints=names.Select(n=>all.FirstOrDefault(t=>t.name==n)).ToArray();
            rest=joints.Select(t=>t==null?Quaternion.identity:t.localRotation).ToArray();previous=transform.position;
            SetMounted(mounted,false);
        }
        void LateUpdate()
        {
            if(Model==null)return;
            float distance=Vector3.Distance(previous,transform.position);previous=transform.position;
            float speed=distance<2?distance/Mathf.Max(Time.deltaTime,.001f):0;
            stride=Mathf.Lerp(stride,Preview?0:Mathf.Clamp01(speed/3),1-Mathf.Exp(-Time.deltaTime*12));
            phase+=Time.deltaTime*Mathf.Lerp(2,9,stride);
            float swing=Mathf.Sin(phase)*27*stride;
            for(int i=0;i<joints.Length;i++)if(joints[i]!=null)
            {
                float angle=i<4?swing*(i==0||i==3?1:-1):i<6?Mathf.Sin(Time.time*1.5f)*(i==4?.6f:1.1f):0;
                if(mounted)angle=i<2?-35:i<4?-65:i>=6?75:0;
                // Imported pivots may carry FBX axis correction; express motion in model axes.
                Vector3 axis=joints[i].parent.InverseTransformDirection(transform.right);
                Quaternion pose=Quaternion.AngleAxis(angle,axis)*rest[i];
                if(mounted && (i==2 || i==3))pose=Quaternion.AngleAxis(i==2?-40:40,joints[i].parent.InverseTransformDirection(transform.forward))*pose;
                joints[i].localRotation=pose;
            }
        }
    }
}
