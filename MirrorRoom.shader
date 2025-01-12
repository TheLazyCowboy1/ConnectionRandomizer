//This code is just a copy of Gamer025's shader for flipping rooms in Rain World: Chaos Edition
//The only change is making the shader flip the room horizontally instead of vertically

Shader "TheLazyCowboy1/MirrorRoomImage"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
		 {
			 // No culling or depth
			 Cull Off ZWrite Off ZTest Always

			  GrabPass
			 {
				"_ScreenTexture"
			 }

			 Pass
			 {
				 CGPROGRAM
				 #pragma vertex vert
				 #pragma fragment frag
				 #pragma enable_d3d11_debug_symbols
				 #include "UnityCG.cginc"

				 struct appdata
				 {
					 float4 vertex : POSITION;
					 float2 uv : TEXCOORD0;
				 };

				 struct v2f
				 {
					 float2 grabPos : TEXCOORD0;
					 float4 vertex : SV_POSITION;
				 };

				 float when_gt(float x, float y) {
					 return max(sign(x - y), 0.0);
				 }

				 float when_lt(float x, float y) {
					 return max(sign(y - x), 0.0);
				 }

				 v2f vert(appdata v)
				 {
					 v2f o;
					 o.vertex = UnityObjectToClipPos(v.vertex);
					 o.grabPos = ComputeGrabScreenPos(o.vertex);
					 return o;
				 }


				 sampler2D _MainTex;
				 sampler2D _ScreenTexture;
				 //float Gamer025_YFlip;
				 //float TheLazyCowboy1_XFlip;

				 fixed4 frag(v2f i) : SV_Target
				 {
					 //float closeToMid = 1 - 2 * abs(Gamer025_YFlip - 0.5f);
					 //float closeToMid = 1 - 2 * abs(TheLazyCowboy1_XFlip - 0.5f);
					 //float closeToMid = 0;
					 //float flipYMidpoint = sign(Gamer025_YFlip - 0.5);
					 //float flipXMidpoint = sign(TheLazyCowboy1_XFlip - 0.5);
					 //float flipXMidpoint = 1;
					 //i.grabPos.y = abs((flipYMidpoint + 1) / 2 - i.grabPos.y);
					 i.grabPos.x = abs(1 - i.grabPos.x);
					 //float upperOrLower = sign(i.grabPos.y - 0.5);
					 //float upperOrLower = sign(i.grabPos.x - 0.5);

					 //i.grabPos.y = i.grabPos.y + closeToMid / 2 * upperOrLower;
					 //i.grabPos.x = i.grabPos.x + closeToMid / 2 * upperOrLower;
					 //float outOfBounds = when_gt(i.grabPos.y, 1) + when_lt(i.grabPos.y, 0);
					 float outOfBounds = when_gt(i.grabPos.x, 1) + when_lt(i.grabPos.x, 0);

					 fixed4 col = tex2D(_ScreenTexture, i.grabPos);
					 col.rgb = col.rgb - outOfBounds;
					 return col;
				 }
				 ENDCG
			 }
		 }
}