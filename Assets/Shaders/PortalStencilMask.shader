Shader "Portal/StencilMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _StencilRef ("Stencil Reference", Int) = 1
    }
    SubShader
    {
        // 必须在 Transparent queue 里，但比普通 sprite 早渲染
        // 这样 stencil 值在 sprite 渲染时已经写好了
        Tags { "RenderType"="Transparent" "Queue"="Transparent-1" }

        ColorMask 0
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref [_StencilRef]
            Comp Always
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 只在 sprite 不透明的区域写入 stencil
                fixed4 c = tex2D(_MainTex, i.uv);
                clip(c.a - 0.01);
                return 0;
            }
            ENDCG
        }
    }
}
