Shader "Arcane Waters/Rectangular Hole" {
     
     Properties {
         _Center ("Hole Center", Vector) = (0, 0, 0, 0)
         _Size ("Hole Size", Vector) = (10, 10, 0, 0)
         _MainTex ("Main Texture", 2D) = ""
     }
     
     SubShader {
         Tags {"Queue" = "Transparent"}
         Blend SrcAlpha OneMinusSrcAlpha
         Pass {
             CGPROGRAM
			 #pragma vertex vert
			 #pragma fragment frag

			 struct appdata
			 {
				float4 vertex : POSITION;
				half2 texCoord : TEXCOORD;
			 };
		 
			 struct v2f
			 {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			 };
				 
			 uniform half4 _MainTex_ST;

			 v2f vert (appdata v)
			 {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = _MainTex_ST.xy * v.texCoord + _MainTex_ST.zw;
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
				return o;
			 }

             uniform sampler2D _MainTex;
             uniform half2 _Center, _Size;

			 fixed4 frag(v2f i) : COLOR {
				 fixed4 fragColor = tex2D(_MainTex, i.texcoord);

				 half2 isOutsideRect = step(_Size / 2, abs(i.worldPos - _Center));
				 fragColor.a = max(isOutsideRect.x, isOutsideRect.y) * fragColor.a;

				 return fragColor;
			 }
             ENDCG
         }
     }
     
}
