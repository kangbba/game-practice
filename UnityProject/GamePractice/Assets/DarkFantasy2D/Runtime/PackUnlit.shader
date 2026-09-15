Shader "DarkFantasy2D/UnlitTransparent"
{
 Properties { _MainTex ("Sprite Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _FlashAmount ("Flash", Range(0,1)) = 0 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
  Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   sampler2D _MainTex; fixed4 _Color; fixed _FlashAmount;
   v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
   fixed4 frag(v2f i):SV_Target { fixed4 c = tex2D(_MainTex,i.uv)*i.color; c.rgb = lerp(c.rgb, fixed3(1,1,1), _FlashAmount); return c; }
   ENDCG
  }
 }
}
