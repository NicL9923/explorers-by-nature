using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ExplorersByNature.Shared;

namespace ExplorersByNature
{
    [DefaultExecutionOrder(50)]
    public sealed class PlayerWardrobe : MonoBehaviour
    {
        public static PlayerWardrobe Current {get;private set;}
        public static bool IsOpen=>Current!=null && Current.Opened;
        public static string SelectedId=>PlayerModels.Normalize(PlayerPrefs.GetString("ExplorerModel",PlayerModels.Default));
        public bool Opened {get;private set;}
        public string DraftId=>PlayerAvatar.Ids[draft];
        public PlayerAvatar PreviewAvatar {get;private set;}
        FirstPersonWalker walker;GameObject stage;Camera camera;RenderTexture portrait;int draft;float angle=-20;
        const uint PortraitLightLayer=1u<<7;
        readonly List<Texture2D> buttonTextures=new List<Texture2D>();
        GUIStyle title,body,choice,selectedChoice,primary,secondary,small;bool rotate=true;
        void Awake(){Current=this;walker=FindFirstObjectByType<FirstPersonWalker>();}
        public void Open()
        {
            if(Opened)return;Opened=true;draft=PlayerAvatar.Index(SelectedId);walker.SetMenu(true);
            stage=new GameObject("Explorer fitting room");stage.transform.position=new Vector3(0,-2000,0);
            var actor=new GameObject("Explorer preview");actor.transform.SetParent(stage.transform,false);
            PreviewAvatar=actor.AddComponent<PlayerAvatar>();PreviewAvatar.Preview=true;PreviewAvatar.SetModel(DraftId);ConfigurePreview(stage.transform);
            portrait=new RenderTexture(600,720,24){name="Explorer portrait",antiAliasing=2};portrait.Create();
            var lens=new GameObject("Portrait camera");lens.transform.SetParent(stage.transform,false);camera=lens.AddComponent<Camera>();
            camera.targetTexture=portrait;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.043f);
            camera.nearClipPlane=.05f;camera.farClipPlane=8;camera.fieldOfView=34;
            camera.transform.localPosition=new Vector3(0,1.05f,3.4f);camera.transform.LookAt(stage.transform.position+Vector3.up*.94f);
            var data=camera.GetUniversalAdditionalCameraData();data.renderShadows=false;data.renderPostProcessing=false;
            for(int i=0;i<2;i++)
            {
                var lamp=new GameObject("Portrait soft light");lamp.transform.SetParent(stage.transform,false);
                var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.cullingMask=1<<31;light.range=7;light.intensity=i==0?3.3f:1.6f;
                light.color=i==0?new Color(1,.87f,.72f):new Color(.76f,.85f,1);
                light.GetUniversalAdditionalLightData().renderingLayers=PortraitLightLayer;
                lamp.transform.localPosition=new Vector3(i==0?-1.4f:1.5f,2,1.5f);
            }
        }
        public void Choose(int index)
        {
            if(!Opened)return;draft=Mathf.Clamp(index,0,PlayerAvatar.Ids.Length-1);PreviewAvatar.SetModel(DraftId);ConfigurePreview(stage.transform);
        }
        public void Confirm()
        {
            if(!Opened || PreviewAvatar.Model==null)return;
            PlayerPrefs.SetString("ExplorerModel",DraftId);PlayerPrefs.Save();Close();
        }
        public void Close()
        {
            Opened=false;if(camera!=null)camera.targetTexture=null;if(stage!=null)Destroy(stage);
            if(portrait!=null){portrait.Release();Destroy(portrait);}stage=null;camera=null;portrait=null;PreviewAvatar=null;
            if(walker!=null)walker.SetMenu(false);
        }
        void Update()
        {
            if(walker==null)return;
            if(!walker.Automated && Input.GetKeyDown(KeyCode.F4)){if(Opened)Close();else Open();}
            if(!Opened)return;
            if(!walker.MenuOpen){Close();return;}
            if(Input.GetKeyDown(KeyCode.LeftArrow))Choose((draft+3)%4);
            if(Input.GetKeyDown(KeyCode.RightArrow))Choose((draft+1)%4);
            if(Input.GetKeyDown(KeyCode.Return))Confirm();
            if(rotate)angle+=Time.unscaledDeltaTime*12;
            if(PreviewAvatar!=null)PreviewAvatar.transform.localRotation=Quaternion.Euler(0,angle,0);
        }
        static void ConfigurePreview(Transform root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            // Keep the valley's changing sunlight and bright sky probe out of the studio.
            // Rendering layers isolate direct lights without changing the world sun.
            var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.14f,.15f,.16f));
            var block=new MaterialPropertyBlock();block.CopySHCoefficientArraysFrom(new[]{ambient});
            block.CopyProbeOcclusionArrayFrom(new[]{Vector4.one});
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.renderingLayerMask=PortraitLightLayer;
                renderer.lightProbeUsage=LightProbeUsage.CustomProvided;
                renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                renderer.SetPropertyBlock(block);
            }
        }
        void OnGUI()
        {
            if(!Opened)return;GUI.depth=-100;GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            if(title==null)
            {
                title=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.94f,.89f,.77f);
                body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};body.normal.textColor=new Color(.88f,.87f,.81f);
                small=new GUIStyle(body){fontSize=14};
                Texture2D forest=Solid(new Color(.12f,.18f,.15f)),forestHover=Solid(new Color(.20f,.28f,.22f)),forestPressed=Solid(new Color(.08f,.13f,.10f));
                Texture2D selected=Solid(new Color(.25f,.27f,.18f)),selectedHover=Solid(new Color(.32f,.34f,.23f));
                Texture2D ochre=Solid(new Color(.82f,.63f,.34f)),ochreHover=Solid(new Color(.91f,.73f,.45f)),ochrePressed=Solid(new Color(.72f,.53f,.27f));
                Color parchment=new Color(.95f,.91f,.81f);
                choice=ButtonStyle(forest,forestHover,forestPressed,parchment,18,TextAnchor.MiddleLeft);
                selectedChoice=ButtonStyle(selected,selectedHover,forestPressed,parchment,18,TextAnchor.MiddleLeft);
                primary=ButtonStyle(ochre,ochreHover,ochrePressed,new Color(.09f,.12f,.08f),19,TextAnchor.MiddleCenter);primary.fontStyle=FontStyle.Bold;
                secondary=ButtonStyle(forest,forestHover,forestPressed,parchment,15,TextAnchor.MiddleCenter);
            }
            float scale=Mathf.Min(Screen.width/940f,Screen.height/680f);scale=Mathf.Min(scale,1.5f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            Fill(new Rect(0,0,w,h),new Color(.025f,.04f,.035f,1));float x=(w-900)/2,y=(h-630)/2;
            GUI.Label(new Rect(x+24,y+12,850,42),"CHOOSE YOUR EXPLORER",title);
            GUI.Label(new Rect(x+24,y+55,850,30),"Find your look, then head back to the valley.",body);
            if(portrait!=null)GUI.DrawTexture(new Rect(x+24,y+102,365,438),portrait,ScaleMode.ScaleToFit,false);
            if(GUI.Button(new Rect(x+24,y+550,175,34),rotate?"Pause rotation":"Rotate preview",secondary))rotate=!rotate;
            if(GUI.Button(new Rect(x+212,y+550,177,34),"Turn around",secondary))angle+=180;
            float right=x+420;
            for(int i=0;i<4;i++)
            {
                bool selected=draft==i;
                string label=(selected?"✓  ":"    ")+PlayerAvatar.Names[i]+(SelectedId==PlayerAvatar.Ids[i]?"  · Current":"");
                var row=new Rect(right,y+105+i*62,450,52);
                if(selected)Fill(row,new Color(.78f,.61f,.34f));
                var face=selected?new Rect(row.x+2,row.y+2,row.width-4,row.height-4):row;
                if(GUI.Button(face,label,selected?selectedChoice:choice))Choose(i);
            }
            GUI.Label(new Rect(right,y+370,445,60),PlayerAvatar.Descriptions[draft],body);
            GUI.Label(new Rect(right,y+436,445,50),"Your choice is saved on this PC and shown to friends in multiplayer.",small);
            GUI.enabled=PreviewAvatar!=null && PreviewAvatar.Model!=null;
            if(GUI.Button(new Rect(right,y+504,450,44),"Use this explorer",primary))Confirm();GUI.enabled=true;
            if(GUI.Button(new Rect(right,y+559,450,34),"Cancel",secondary))Close();
            GUI.Label(new Rect(x+24,y+600,850,25),"Left / right: browse     Enter: choose     Escape / F4: close",small);
            GUI.matrix=Matrix4x4.identity;
        }
        Texture2D Solid(Color color)
        {
            var texture=new Texture2D(1,1,TextureFormat.RGBA32,false){name="Wardrobe button color",hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixel(0,0,color);texture.Apply(false,true);buttonTextures.Add(texture);return texture;
        }
        static GUIStyle ButtonStyle(Texture2D normal,Texture2D hover,Texture2D active,Color text,int size,TextAnchor alignment)
        {
            var style=new GUIStyle(GUI.skin.button){fontSize=size,alignment=alignment,padding=new RectOffset(16,12,0,0),border=new RectOffset(0,0,0,0)};
            foreach(var state in new[]{style.normal,style.onNormal}){state.background=normal;state.textColor=text;}
            foreach(var state in new[]{style.hover,style.onHover,style.focused,style.onFocused}){state.background=hover;state.textColor=text;}
            foreach(var state in new[]{style.active,style.onActive}){state.background=active;state.textColor=text;}
            return style;
        }
        static void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        void OnDestroy(){if(Opened)Close();foreach(var texture in buttonTextures)if(texture!=null)Destroy(texture);buttonTextures.Clear();if(Current==this)Current=null;}
    }
}
