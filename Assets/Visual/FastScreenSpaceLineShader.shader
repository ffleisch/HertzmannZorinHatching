Shader "Unlit/FastScreenspaceLineShader"
{
	Properties
	{
		_col ("Color",Color)=(0,0,0,1)
		_pixelWidth ("Line Width",Float) = 5
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

			uniform float _pixelWidth;
			uniform float4 _col;
			//[maxvertexcount(4)]
			//void geom(lineadj input[4], inout TriangleStream<g2f> triStream) {

			[maxvertexcount(4)]
			void geom(line v2g input[2], inout TriangleStream<g2f> triStream) {

				if ((input[0].endpiece.x ==2) && (input[1].endpiece.x == 1)) { return; }
				
				for (int i = 0; i < 2; i++) {

					 g2f v1;
					 g2f v2;
					 
					 float2 d1=input[i].mixedAdjacency.xy-input[i].vertex.xy;
					
					 d1 *= _ScreenParams.xy;
						
					 float2 normal = _pixelWidth*input[i].vertex.z*normalize(float2(-d1.y,d1.x))/_ScreenParams.xy;

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
			return _col;
		}
		ENDCG
	}
	}
}
