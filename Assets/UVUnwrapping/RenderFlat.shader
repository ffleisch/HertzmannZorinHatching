
Shader "Unlit/RenderFlat"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#define UNITY_SHADER_NO_UPGRADE 1
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 mixedTangent: TANGENT; //direction of largest curvatue  in xyz and corssfield rotation in w
				float3 normal:NORMAL;
			};


			struct v2g {
								float4 vertex : POSITION;
						float2 uv : TEXCOORD0;
						float3 normal:NORMAL;
				float3 dir: TANGENT;  //vector in a direction of the corssfiel in this vertex
			};

			struct g2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 normal:NORMAL;
				float4 objPos : TEXTCOORD1; //interpolated original position

				float3 d1: TEXCOORD2; //direction of one interpolated crossfield vector
				float3 d2: TEXCOORD3; //direction of the other orthogonal crossfield vector

			};

			sampler2D _MainTex;
			float4 _MainTex_ST;


			//https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Rotate-About-Axis-Node.html
			float3 Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation)
			{
				float s = sin(Rotation);
				float c = cos(Rotation);
				float one_minus_c = 1.0 - c;

				Axis = normalize(Axis);
				float3x3 rot_mat =
				{   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
					one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
					one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
				};
				return mul(rot_mat,  In);
			}



			[maxvertexcount(3)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
			{
				g2f o;
				//recalculate the normal
				float3 normal = normalize(cross(input[1].vertex - input[0].vertex, input[2].vertex - input[0].vertex));


				float3 Axis = normal;//has to be normalized
				float3x3 rot_mat =
				{   Axis.x * Axis.x         , Axis.x * Axis.y - Axis.z, Axis.z * Axis.x + Axis.y,
					Axis.x * Axis.y + Axis.z, Axis.y * Axis.y         , Axis.y * Axis.z - Axis.x,
					Axis.z * Axis.x - Axis.y, Axis.y * Axis.z + Axis.x, Axis.z * Axis.z
				};

				float3 directions[3];
				directions[0] = input[0].dir;



				float maxDot = -1;
				float3 currentVec = input[1].dir;
				
				//test all 4 possible crossvetors for the second vertex to see which one aligns with the first
				for (int i = 0; i < 4; i++) {
					float dotp = dot(directions[0],currentVec);
					if (dotp > maxDot) {
						maxDot = dotp;
						directions[1] = currentVec;
					}
					currentVec = mul(currentVec, rot_mat);
				}

				maxDot = -1;
				currentVec = input[2].dir;
				//test all 4 possible crossvetors for the third vertex to see which one aligns with the first
				for (i = 0; i < 4; i++) {
					float dotp = dot(directions[0],currentVec);
					if (dotp > maxDot) {
						maxDot = dotp;
						directions[2] = currentVec;
					}
					currentVec = mul(currentVec, rot_mat);
				}



				//write out new vertices
				for (i = 2; i >=0; i--)
				{
					float4 vert = input[i].vertex;
					o.normal = input[i].normal;
					o.vertex = float4(float2(-1, -1) + 2 * float2(input[i].uv.x, input[i].uv.y), 0, 1); //UnityObjectToClipPos(vert);
					o.objPos = vert;
					o.uv = input[i].uv;
					o.d1 = directions[i];
					o.d2 = cross(input[i].normal,o.d1);
					triStream.Append(o);
				}


				/*
				for (int i = 0; i < 3; i++)
				{
					float4 vert = input[i].vertex;
					vert.xyz += normal * 0.5;  //math to move tri's out
					UNITY_TRANSFER_FOG(o,o.vertex);
					o.normal = normal;
					o.vertex = UnityObjectToClipPos(vert);
					triStream.Append(o);
				}*/

				//triStream.RestartStrip();

				/*
				//float3 uvCross = cross(float3(input[2].uv - input[0].uv,0),float3( input[1].uv - input[0].uv,0));

				//bool chirality = uvCross.z < 0;
				g2f o;
				for (int i = 0; i < 3; i++)
				{
					int ind = 2 - i;

					o.normal = input[ind].normal;


					o.vertex = input[ind].vertex;//UnityObjectToClipPos(clipPos); //UnityObjectToClipPos(vert);
					o.uv = input[ind].uv;
					triStream.Append(o);
				}*/



			}

			v2g vert(appdata v)
			{
				v2g o;
				o.vertex = v.vertex;//float4(float2(-1,-1) + 2 * float2(v.uv.x,v.uv.y), 0,1);

				o.dir = Unity_RotateAboutAxis_Radians_float(v.mixedTangent.xyz,v.normal,v.mixedTangent.w);

				o.uv = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(g2f i) : SV_Target
			{
			
					//TODO translate fomr d1 d2 normal base to uv base


					float2 dir1 = i.d1.xy;
					float2 dir2 = i.d2.xy;
					float ratio = _ScreenParams.y / (float)_ScreenParams.x;
					return float4(dir1.x,-dir1.y * ratio,dir2.x,-dir2.y * ratio);//float4(dir1.xy,dir2.xy);//,d2_pos.xy);//float4(o.d1[0],o.d1[1],0,0);//,o.d2[0],o.d2[1]);//float4(0,1,depth,1);			}
			}
		ENDCG
	}
	}
}
