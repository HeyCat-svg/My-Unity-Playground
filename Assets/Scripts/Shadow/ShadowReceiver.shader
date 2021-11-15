// Upgrade NOTE: replaced 'defined SHADER_TARGET_GLSL' with 'defined (SHADER_TARGET_GLSL)'
// Upgrade NOTE: replaced 'defined UNITY_REVERSED_Z' with 'defined (UNITY_REVERSED_Z)'

Shader "Custom/ShadowReceiver" {
    Properties{}

    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest 	

            #include "UnityCG.cginc"

            struct a2v{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float eyeZ : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
            };

            uniform float4x4 _WorldToShadow;
            uniform sampler2D _gShadowMapTexture;
            uniform float4 _gShadowMapTexture_TexelSize;

            uniform float _ShadowStrength;

            float PCFSample(float depth, float2 uv) {
				float shadow = 0.0;
				for (int x = -1; x <= 1; ++x) {
					for (int y = -1; y <= 1; ++y) {
						float sampleDepth = tex2D(_gShadowMapTexture, uv + float2(x, y) * _gShadowMapTexture_TexelSize.xy).r;
						shadow += depth < sampleDepth ? _ShadowStrength : 1;
					}
				}
				return shadow /= 9;
			}

            v2f vert (a2v v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.eyeZ = o.vertex.w;    
                return o;
            }

            fixed4 frag (v2f i) : COLOR0 {
                float4 shadowCoord = mul(_WorldToShadow, i.worldPos);
                shadowCoord.xy /= shadowCoord.w;
                shadowCoord.xy = shadowCoord.xy * 0.5 + 0.5;
                // float sampleDepth = tex2D(_gShadowMapTexture, shadowCoord.xy).r;
                float depth = shadowCoord.z / shadowCoord.w;    // [near, far]->[1, 0]

            // #if defined (SHADER_TARGET_GLSL)
            //     depth = depth * 0.5 + 0.5;
            // #elif defined (UNITY_REVERSED_Z)
            //     depth = 1 - depth;
            // #endif
                float shadow = PCFSample(depth, shadowCoord.xy);

                // float4 tmpCoord = mul(UNITY_MATRIX_VP, i.worldPos);
                // float tmp = (tmpCoord.z / tmpCoord.w);
                // return fixed4(tmp, tmp, tmp, 1);
                // return fixed4(sampleDepth, sampleDepth, sampleDepth, 1);
                // return fixed4(depth, depth, depth, 1);
                return fixed4(shadow, shadow, shadow, 1);
            }
            ENDCG
        }
    }
}
