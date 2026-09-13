Shader "Explorers/ValleySky"
{
 Properties { _Tint("Tint", Color)=(.2,.45,.7,1) _Daylight("Daylight",Float)=1 _CloudCover("Cloud cover",Float)=0 _SunDirection("Sun direction",Vector)=(0,1,0,0) _SkyQuality("Cloud quality",Float)=1 }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Daylight,_CloudCover,_SkyQuality;float4 _SunDirection;
 struct V {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 V vert(float4 p:POSITION){V o;o.pos=TransformObjectToHClip(p.xyz);o.dir=p.xyz;return o;}
 float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 float noise(float3 p){float3 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(q),hash(q+float3(1,0,0)),f.x),lerp(hash(q+float3(0,1,0)),hash(q+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(q+float3(0,0,1)),hash(q+float3(1,0,1)),f.x),lerp(hash(q+float3(0,1,1)),hash(q+1),f.x),f.y),f.z);}
 float fbm(float3 p){return noise(p)*.57+noise(p*2.03+11)*.28+noise(p*4.17+23)*.15;}
 float hg(float mu,float g){return (1-g*g)/(12.56637*pow(max(.01,1+g*g-2*g*mu),1.5));}
 float density(float3 p)
 {
  float height=saturate((p.y-1800)/1400);
  float profile=smoothstep(0,.12,height)*(1-smoothstep(.45,1,height));
  float3 advected=p+float3(_Time.y*5,0,_Time.y*1.7);
  float weather=fbm(float3(advected.x*.00032,0,advected.z*.00032));
  float shape=fbm(advected*.0015+float3(0,2,0));
  float coverage=lerp(.51,.32,_CloudCover);
  float broad=saturate((weather*.35+shape*.65-coverage)*6.5);
  float erosion=(1-fbm(advected*.0063))*.14*(1-broad);
  return saturate(broad-erosion)*profile;
 }
 half4 frag(V i):SV_Target
 {
  float3 d=normalize(i.dir),sun=normalize(_SunDirection.xyz);float mu=dot(d,sun),height=max(.035,d.y);
  // Analytic optical-depth approximation: wavelength-dependent extinction and
  // separate Rayleigh/Mie phase functions. Not a multiple-scattering LUT solver.
  float3 beta=float3(.0058,.0135,.0331);float airMass=1/(height+.15*pow(max(.01,1.05-height),-1.25));
  float3 trans=exp(-(beta*8+.012)*airMass);
  float rayleigh=.0596831*(1+mu*mu);float mie=hg(mu,.76);
  float3 daylight=(1-trans)*(float3(.35,.57,1.0)*rayleigh*9+float3(1,.85,.66)*mie*.20);
  float3 col=lerp(float3(.035,.06,.115),daylight+float3(.09,.16,.24),_Daylight);
  float dusk=pow(saturate(mu),5)*pow(1-saturate(d.y),3)*(1-_Daylight*.75);
  col+=float3(.48,.15,.025)*dusk;
  float disk=smoothstep(cos(.0047),cos(.0043),mu);
  float3 sunColor=lerp(float3(1,.36,.09),float3(1,.91,.78),saturate(sun.y*2.5));
  sunColor=lerp(float3(.5,.62,1),sunColor,saturate(_Daylight*6));
  col+=disk*sunColor*18;
  if(d.y>.073)
  {
   float start=1800/d.y,end=min(3200/d.y,25000);int steps=_SkyQuality>.5?96:24;
   float stride=(end-start)/steps;float transmission=1;float3 cloudLight=0;
   float jitter=.5; // Stable quadrature avoids unfiltered noise near the horizon.
   [loop] for(int k=0;k<steps;k++)
   {
    float3 p=float3(_WorldSpaceCameraPos.x,0,_WorldSpaceCameraPos.z)+d*(start+(k+jitter)*stride);
    float rho=density(p)*smoothstep(25000,16000,length(p.xz-_WorldSpaceCameraPos.xz));if(rho<.002)continue;
    float lightDepth=0;
    [unroll] for(int j=0;j<6;j++)lightDepth+=density(p+sun*(60+j*100))*100;
    float beer=exp(-lightDepth*.007);
    // Broad secondary lobe approximates intra-cloud scatter without pretending
    // to solve multiple scattering; Beer extinction keeps silhouettes intact.
    float phase=hg(mu,.65)*3+hg(mu,-.2)*.5;
    float silver=pow(1-rho,3)*pow(saturate(mu),12)*.9;
    float3 ambient=lerp(float3(.13,.17,.26),float3(.62,.69,.80),_Daylight)*lerp(.55,1.25,saturate((p.y-1800)/1400));
    float3 lighting=ambient+sunColor*(beer*phase+silver)*lerp(.2,2.6,_Daylight);
    float alpha=1-exp(-rho*stride*.0045);
    cloudLight+=transmission*alpha*lighting;transmission*=1-alpha;
    if(transmission<.01)break;
   }
   col=col*transmission+cloudLight;
  }
  return half4(max(col,0),1);
 }
 ENDHLSL
 }
 }
}
