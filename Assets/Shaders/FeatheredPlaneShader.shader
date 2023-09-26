Shader "Custom/FeatheredPlaneShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TexTintColor("Texture Tint Color", Color) = (1,1,1,1)
        _PlaneColor("Plane Color", Color) = (1,1,1,1)
        _Tiling("Texture Tiling", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 uv2 : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TexTintColor;
            fixed4 _PlaneColor;
            float4 _Tiling;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy;
                o.uv2 = v.uv2;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, frac(i.uv)) * _TexTintColor;
                col = lerp(_PlaneColor, col, col.a);
                col.a *= 1 - smoothstep(1, _Tiling.z, i.uv2.x);
                return col;
            }
            ENDCG
        }
    }
    CustomEditor "ShaderGUI"
}
