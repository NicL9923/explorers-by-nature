using UnityEngine;

namespace ExplorersByNature
{
    public sealed class TrailCompass : MonoBehaviour
    {
        FirstPersonWalker walker;GUIStyle label;
        void Start(){walker=FindFirstObjectByType<FirstPersonWalker>();}
        public static float Bearing(Vector3 from,Vector3 to)=>Mathf.Repeat(Mathf.Atan2(to.x-from.x,to.z-from.z)*Mathf.Rad2Deg,360);
        void OnGUI()
        {
            if(walker==null || walker.MenuOpen || PlayerWardrobe.IsOpen || ReferenceGrove.PhotoMode)return;
            GUI.matrix=Matrix4x4.identity;
            if(label==null)label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=16};
            float heading=walker.transform.eulerAngles.y,cx=Screen.width*.5f;
            GUI.Box(new Rect(cx-220,14,440,84),GUIContent.none);
            string[] points={"N","NE","E","SE","S","SW","W","NW"};
            for(int i=0;i<8;i++)
            {float delta=Mathf.DeltaAngle(heading,i*45);if(Mathf.Abs(delta)<75)GUI.Label(new Rect(cx+delta*2.5f-20,23,40,24),points[i],label);}
            GUI.Label(new Rect(cx-30,43,60,25),Mathf.RoundToInt(heading)+"°",label);
            float home=Mathf.DeltaAngle(heading,Bearing(walker.transform.position,ValleyShape.Spawn));
            GUI.Label(new Rect(cx+Mathf.Clamp(home,-72,72)*2.5f-35,68,70,23),home< -72?"< Home":home>72?"Home >":"Home",label);
        }
    }
}
