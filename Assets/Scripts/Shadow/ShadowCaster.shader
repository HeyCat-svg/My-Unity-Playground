// Upgrade NOTE: replaced 'defined SHADER_TARGET_GLSL' with 'defined (SHADER_TARGET_GLSL)'
// Upgrade NOTE: replaced 'defined UNITY_REVERSED_Z' with 'defined (UNITY_REVERSED_Z)'

Shader "Custom/ShadowCaster"
{
    Properties{}

    SubShader{
        Tags { "RenderType"="Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct a2v {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 depth : TEXCOORD0;
            };

            uniform float _ShadowBias;

            v2f vert (a2v v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.z += _ShadowBias;
                o.depth = o.vertex.zw;
                return o;
            }

            fixed4 frag (v2f i) : COLOR {
                float depth = i.depth.x / i.depth.y;    // [near, far]->[1, 0]

            // #if defined (SHADER_TARGET_GLSL)
            //     depth = depth * 0.5f + 0.5f;
            // #elif defined (UNITY_REVERSED_Z)
            //     depth = 1 - depth;
            // #endif
                fixed4 col = fixed4(depth, depth, depth, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
