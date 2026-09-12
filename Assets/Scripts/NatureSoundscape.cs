using System.Collections.Generic;
using UnityEngine;

namespace ExplorersByNature
{
    public sealed class NatureSoundscape : MonoBehaviour
    {
        static NatureSoundscape current;
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly List<AudioSource> calls=new List<AudioSource>();
        AudioSource wind,rain,steps,feedback;
        FirstPersonWalker walker;
        CharacterController controller;
        Vector3 previous;
        float stride,callTimer,fireTimer;
        readonly List<AudioSource> fires=new List<AudioSource>();
        System.Random random=new System.Random(923);

        void Start()
        {
            current=this;walker=FindFirstObjectByType<FirstPersonWalker>();controller=walker.GetComponent<CharacterController>();previous=walker.transform.position;
            foreach(string name in new[]{"wind","river","rustle","rain","bird","duck","cow","hen","fox","beaver","grass","wood","stone","water","build","milk","eggs","picnic","reward","fire"})clips[name]=Synthesize(name);
            wind=Source("Wind",Vector3.zero,clips["wind"],.08f,0,true);rain=Source("Drizzle",Vector3.zero,clips["rain"],0,0,true);
            steps=Source("Footsteps",Vector3.zero,null,.24f,0,false);feedback=Source("Ranch sounds",Vector3.zero,null,.3f,1,false);
            for(int i=0;i<10;i++){float z=-350+i*80;Source("River current",new Vector3(ValleyShape.RiverX(z),ValleyShape.WaterHeight,z),clips["river"],.3f,1,true,85);}
            foreach(float z in new[]{-245f,-175f,-80f,30f,100f})
            {
                Vector3 p=ValleyShape.Ground(ValleyShape.TrailX(z)-14,z,5);
                Source("Leaves in the breeze",p,clips["rustle"],.18f,1,true,45);
                calls.Add(Source("Woodland birds",p,clips["bird"],.22f,1,false,65));
            }
            calls.Add(Source("Clover calling",ValleyShape.Ground(-99,-226,1),clips["cow"],.16f,1,false,45));
            calls.Add(Source("Hens clucking",ValleyShape.Ground(-108,-226,.5f),clips["hen"],.14f,1,false,35));
            calls.Add(Source("Mallards calling",new Vector3(ValleyShape.RiverX(-150),ValleyShape.WaterHeight,-150),clips["duck"],.2f,1,false,55));
            calls.Add(Source("Beaver chatter",ValleyShape.Ground(ValleyWorld.ShoreX(-115,-1)-1.5f,-115,.3f),clips["beaver"],.12f,1,false,22));
            calls.Add(Source("Fox call",ValleyShape.Ground(-118,-178,.4f),clips["fox"],.10f,1,false,30));
        }
        AudioSource Source(string label,Vector3 position,AudioClip clip,float volume,float spatial,bool loop,float distance=35)
        {
            var go=new GameObject(label);go.transform.SetParent(transform);go.transform.position=position;
            var source=go.AddComponent<AudioSource>();source.clip=clip;source.volume=volume;source.spatialBlend=spatial;source.loop=loop;source.playOnAwake=false;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=distance;
            if(loop&&clip!=null)source.Play();return source;
        }
        public static void Feedback(string cue,Vector3 position)
        {
            if(current==null)return;
            string name=cue=="place"||cue=="move"||cue=="remove"?"build":cue;
            if(!current.clips.TryGetValue(name,out AudioClip clip))clip=current.clips["build"];
            current.feedback.transform.position=position;current.feedback.pitch=1;current.feedback.PlayOneShot(clip);
        }
        void Update()
        {
            if(walker==null)return;
            rain.volume=SkyWeather.RainAmount*.13f;wind.volume=.06f+SkyWeather.RainAmount*.025f;
            Vector3 delta=walker.transform.position-previous;delta.y=0;previous=walker.transform.position;
            if(!walker.Automated&&!walker.MenuOpen&&controller.isGrounded&&delta.magnitude<2)
            {
                stride+=delta.magnitude;
                if(stride>1.8f)
                {
                    stride=0;string surface="grass";Vector3 p=walker.transform.position;
                    if(p.y<ValleyShape.WaterHeight+.3f)surface="water";
                    else if(Physics.Raycast(walker.view.transform.position,Vector3.down,out RaycastHit hit,3))
                    {
                        if(hit.collider.GetComponentInParent<RanchTarget>()!=null)surface="wood";
                        else if(p.y>125)surface="stone";
                    }
                    steps.pitch=.94f+(float)random.NextDouble()*.12f;steps.PlayOneShot(clips[surface]);
                }
            }
            else if(delta.magnitude>=2)stride=0;
            callTimer-=Time.deltaTime;
            if(callTimer<=0&&calls.Count>0){callTimer=3+(float)random.NextDouble()*5;var call=calls[random.Next(calls.Count)];call.pitch=.94f+(float)random.NextDouble()*.12f;call.Play();}
            fireTimer-=Time.deltaTime;
            if(fireTimer<=0)
            {
                fireTimer=2;
                var positions=new List<Vector3>();
                foreach(var target in FindObjectsByType<RanchTarget>(FindObjectsSortMode.None))if(target.name=="campfire"&&Vector3.Distance(target.transform.position,walker.transform.position)<35)positions.Add(target.transform.position);
                while(fires.Count<Mathf.Min(4,positions.Count))fires.Add(Source("Campfire crackle",Vector3.zero,clips["fire"],.13f,1,true,30));
                for(int i=0;i<fires.Count;i++){fires[i].gameObject.SetActive(i<positions.Count);if(i<positions.Count){fires[i].transform.position=positions[i];if(!fires[i].isPlaying)fires[i].Play();}}
            }
        }
        public static AudioClip Synthesize(string kind)
        {
            const int rate=22050;
            bool loop=kind=="wind"||kind=="river"||kind=="rustle"||kind=="rain"||kind=="fire";
            float duration=loop?6:kind=="cow"?1.3f:kind=="bird"?.7f:kind=="picnic"||kind=="reward"?.75f:.3f;
            var samples=new float[Mathf.CeilToInt(duration*rate)];var rng=new System.Random(73+kind.Length*41);float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,raw=(float)rng.NextDouble()*2-1;
                filtered=Mathf.Lerp(filtered,raw,kind=="wind"?.015f:.12f);
                float envelope=loop?1:Mathf.Sin(Mathf.Clamp01(t/duration)*Mathf.PI)*Mathf.Exp(-t*(kind=="cow"?1:3));
                float v=filtered;
                switch(kind)
                {
                    case "wind":v=filtered*(.7f+.3f*Mathf.Sin(t*Mathf.PI/3));break;
                    case "rustle":v=(filtered*.6f+raw*.09f)*(.4f+.3f*Mathf.Sin(t*2.0944f)+.2f*Mathf.Sin(t*6.2832f));break;
                    case "river":v=filtered*.8f+Mathf.Sin(t*740+Mathf.Sin(t*4)*35)*.025f+raw*.05f;break;
                    case "rain":v=filtered*.6f+raw*.12f;break;
                    case "fire":v=filtered*.35f+(rng.NextDouble()>.9992?raw*.7f:0);break;
                    case "bird":v=Mathf.Sin(2*Mathf.PI*(2200*t+700*t*t))*.2f*Mathf.Pow(Mathf.Sin(t*27),2);break;
                    case "duck":v=(Mathf.Sin(t*1800)+Mathf.Sin(t*3600)*.45f)*.15f*Mathf.Pow(Mathf.Sin(t*26),2);break;
                    case "cow":v=(Mathf.Sin(2*Mathf.PI*(85*t-12*t*t))+.35f*Mathf.Sin(2*Mathf.PI*170*t))*.24f;break;
                    case "hen":case "beaver":v=Mathf.Sin(t*(kind=="hen"?4200:2600)+Mathf.Sin(t*40)*3)*.2f*Mathf.Pow(Mathf.Sin(t*32),2);break;
                    case "fox":v=(Mathf.Sin(t*2200)+raw*.2f)*.13f;break;
                    case "wood":case "build":v=(Mathf.Sin(t*850)*.25f+Mathf.Sin(t*1450)*.12f+filtered*.3f)*Mathf.Exp(-t*22);break;
                    case "stone":case "eggs":v=(Mathf.Sin(t*3300)*.15f+raw*.17f)*Mathf.Exp(-t*26);break;
                    case "water":case "milk":v=filtered*.65f+Mathf.Sin(t*(1200-t*1500))*.09f;break;
                    case "picnic":case "reward":v=(Mathf.Sin(t*2*Mathf.PI*523.25f)+Mathf.Sin(t*2*Mathf.PI*659.25f)*.5f)*.12f;break;
                    default:v=(filtered*.6f+raw*.12f)*Mathf.Exp(-t*18);break;
                }
                // Join loop boundaries quietly rather than clicking at the seam.
                if(loop)envelope=Mathf.Min(1,Mathf.Min(t*15,(duration-t)*15));
                samples[i]=Mathf.Clamp(v*envelope,-.8f,.8f);
            }
            var clip=AudioClip.Create("Original "+kind,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){if(current==this)current=null;foreach(var clip in clips.Values)Destroy(clip);}
    }
}
