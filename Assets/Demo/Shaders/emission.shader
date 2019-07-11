Shader "Custom/Emission"
{
	SubShader
	{
		//ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			// Properties
            sampler2D_float _CameraDepthTexture;
			sampler2D _DepthTexture;

			struct vertexInput
			{
				float4 vertex : POSITION;
				float3 texCoord : TEXCOORD0;
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float3 texCoord : TEXCOORD0;
                float linearDepth : TEXCOORD1;

                float4 screenPos : TEXCOORD2;
				float2 uv_depth : TEXCOORD3;
			};

			vertexOutput vert(vertexInput v)
			{
				vertexOutput output;
				output.pos = UnityObjectToClipPos(v.vertex);
                output.texCoord = v.texCoord;                
				output.uv_depth = v.texCoord;

                output.screenPos = ComputeScreenPos(output.pos);
				output.screenPos.z = COMPUTE_DEPTH_01;
                output.linearDepth = -(UnityObjectToViewPos(v.vertex).z * _ProjectionParams.w);

                return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
                float4 c = float4(1, 1, 1, 1);

				// ------------------ CASE 1 -------------------------
				float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_DepthTexture, UNITY_PROJ_COORD(input.screenPos));
				float depth = Linear01Depth (rawDepth);
                float diff = saturate(input.screenPos.z - depth);
                if(diff < 0.001)
				{
                    c = float4(1, 0, 0, 1);
				}
				else
				{
					c = float4(0, 1, 0, 1);
					discard;
				}
					

				// ------------------ CASE 2 -------------------------
                // decode depth texture info
				/*
				float2 uv = input.screenPos.xy / input.screenPos.w; // normalized screen-space pos
				float camDepth = SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv);
				camDepth = Linear01Depth (camDepth); // converts z buffer value to depth value from 0..1

                float diff = saturate(input.linearDepth-camDepth);
                if(diff < 0.1)
                    c = float4(1, 0, 0, 1);
				else
					c = float4(0, 1, 0, 1);
                return c;
				*/

                //return float4(camDepth, camDepth, camDepth, 1); // test camera depth value
                //return float4(input.linearDepth, input.linearDepth, input.linearDepth, 1); // test our depth
                //return float4(diff, diff, diff, 1);

                return c;
			}

			ENDCG
		}
    }
}