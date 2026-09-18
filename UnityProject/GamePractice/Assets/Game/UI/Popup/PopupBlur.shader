// 팝업 뒤에 까는 흐린 화면. ScreenBlurManager 가 작게 줄여 둔 화면을 이중선형 9탭(1-2-1 텐트)으로 한 번 더 번지게 늘인다.
// 정점 색을 곱하므로 RawImage 색으로 어둡기를 정한다.
Shader "Sayne/UI/PopupBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Radius (texels)", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Radius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _Radius;

                fixed3 sum = tex2D(_MainTex, i.uv).rgb * 4.0;
                sum += tex2D(_MainTex, i.uv + float2( d.x, 0)).rgb * 2.0;
                sum += tex2D(_MainTex, i.uv + float2(-d.x, 0)).rgb * 2.0;
                sum += tex2D(_MainTex, i.uv + float2(0,  d.y)).rgb * 2.0;
                sum += tex2D(_MainTex, i.uv + float2(0, -d.y)).rgb * 2.0;
                sum += tex2D(_MainTex, i.uv + d).rgb;
                sum += tex2D(_MainTex, i.uv - d).rgb;
                sum += tex2D(_MainTex, i.uv + float2( d.x, -d.y)).rgb;
                sum += tex2D(_MainTex, i.uv + float2(-d.x,  d.y)).rgb;

                return fixed4(sum / 16.0, 1.0) * i.color;
            }
            ENDCG
        }
    }
}
