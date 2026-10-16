Shader "Vertigo/UI Dim Cutout"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,0.75)
        _DimSize ("Dim Size", Vector) = (1920,1080,0,0)
        _CutoutA ("Cutout A (cx,cy,halfW,halfH)", Vector) = (0,-99999,0,0)
        _CutoutB ("Cutout B (cx,cy,halfW,halfH)", Vector) = (0,-99999,0,0)
        _CutoutRadius ("Cutout Radius", Float) = 24
        _CutoutSoftness ("Cutout Softness", Float) = 3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _DimSize;
            float4 _CutoutA;
            float4 _CutoutB;
            float _CutoutRadius;
            float _CutoutSoftness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float RoundedBox(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - halfSize + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            float CutoutMask(float2 p, float4 c)
            {
                float d = RoundedBox(p - c.xy, c.zw, _CutoutRadius);
                return smoothstep(0.0, _CutoutSoftness, d);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = (IN.texcoord - 0.5) * _DimSize.xy;
                float mask = CutoutMask(p, _CutoutA) * CutoutMask(p, _CutoutB);

                fixed4 col = IN.color;
                col.a *= mask;
                return col;
            }
            ENDCG
        }
    }
}
