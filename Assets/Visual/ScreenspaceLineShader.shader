Shader "Unlit/ScreenspaceLineShader"
{
	Properties
	{
		col("Color",Color)=(0,0,0,1)
		pixelWidth("Line Width",float) = 5
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 mixedAdjacency : TANGENT;

				float2 endpiece:TEXCOORD1;
			};

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float4 mixedAdjacency : TANGENT;
				float2 endpiece:TEXCOORD1;
			};

			struct g2f {
				float4 vertex : POSITION;
			};

			uniform float pixelWidth;
			uniform float4 col;
			//[maxvertexcount(4)]
			//void geom(lineadj input[4], inout TriangleStream<g2f> triStream) {

			[maxvertexcount(4)]
			void geom(line v2g input[2], inout TriangleStream<g2f> triStream) {

				if ((input[0].endpiece.x ==2) && (input[1].endpiece.x == 1)) { return; }
				
				for (int i = 0; i < 2; i++) {

					 g2f v1;
					 g2f v2;
					 
					 float2 d1=input[i].mixedAdjacency.xy-input[i].vertex.xy;
					 float2 d2=input[i].mixedAdjacency.zw-input[i].vertex.xy;
					
					 //if (dot(d1, d1) < 0.000001f) { return; }
					 //if (dot(d2, d2) < 0.000001f) { return; }
					
					
					
					 d1 *= _ScreenParams.xy;
					 d2 *= _ScreenParams.xy;

					 float2 normal = normalize(d1)-normalize(d2);
					 //normal = float2(1,1);
					 float len = length(normal);
					 if (len < 0.3f) {
						 triStream.RestartStrip();
						 return; // normal = normalize(normal) * 0.1f;
					 }
					 normal =input[i].vertex.z*pixelWidth*float2(-normal.y,normal.x)/(len*len);
					 
					 normal /= _ScreenParams.xy;


					 v1.vertex =float4( input[i].vertex.xy+ normal,1,1);
					 v2.vertex = float4(input[i].vertex.xy - normal, 1, 1);


					 triStream.Append(v1);
					 triStream.Append(v2);

					}

				triStream.RestartStrip();
			}



			v2g vert(appdata v)
			{
				v2g o;
				//o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex = v.vertex;
				o.mixedAdjacency =v.mixedAdjacency;
				o.endpiece =v.endpiece;
				return o;
			}

			fixed4 frag(g2f i) : SV_Target
			{
			// sample the texture
			// apply fog
			return col;
		}
		ENDCG
	}
	}
}
