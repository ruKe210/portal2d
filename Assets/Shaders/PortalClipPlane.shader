Shader "Portal/ClipPlane"
{
    // 根据一条裁剪线（世界空间）裁剪 sprite
    // _ClipPos: 裁剪线上的一个点（传送门位置）
    // _ClipNormal: 裁剪线法线方向
    // _ClipSide: 1 = 保留法线正面, -1 = 保留法线背面
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ClipPos ("Clip Position", Vector) = (0,0,0,0)
        _ClipNormal ("Clip Normal", Vector) = (1,0,0,0)
        _ClipSide ("Clip Side (1 or -1)", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _ClipPos;
            float4 _ClipNormal;
            float _ClipSide;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 计算像素相对于裁剪线的位置
                float2 diff = i.worldPos.xy - _ClipPos.xy;
                float dist = dot(diff, _ClipNormal.xy);

                // _ClipSide=1: 保留法线正面 (dist>0)
                // _ClipSide=-1: 保留法线背面 (dist<0)
                clip(dist * _ClipSide);

                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
