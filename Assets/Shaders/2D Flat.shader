Shader "Custom/2D Flat"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Lambert vertex:vert

        fixed4 _Color;
        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainText;
        };

        void vert(inout appdata_full v)
        {
            v.normal = float3(0, 0, -1);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainText);
            o.Albedo = texColor.rgb * _Color.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}