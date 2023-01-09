Shader "Unlit/phongShading"
{
	Properties
	{
		_ambient("Ambient", Float) = 0
		_diffuse("Diffuse", Float) = 0.5
		_specular("Specular", Float) =0.1
		_specularHardness("Specular Hardness", Float) = 1.5
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

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal:NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normal:NORMAL;
				float4 vertex : SV_POSITION;
			};

			
			float _ambient;
			float _specularHardness;
			float _diffuse;
			float _specular;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.normal = v.normal;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture

				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float3 viewDir = normalize(WorldSpaceViewDir(i.vertex));
				

				float NdotL = dot(i.normal, lightDir);



				float3 halfWay = normalize(lightDir + viewDir);

				float3 NdotH = dot(i.normal, halfWay);


				float specular = min(_specular*pow(clamp(NdotH,0,1),_specularHardness),1);

				float val = clamp(_ambient + clamp(_diffuse,0,1) * NdotL + specular,0,1);
				fixed4 col = float4(val,val,val,1);
				return col;
			}
			ENDCG
		}
	}
}
