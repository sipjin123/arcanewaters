Shader "Arcane Waters/Attack Range Circle"
{
   Properties
   {
      _Position ("Position", Vector) = (0.0, 0.0, 0.0)
      _Radius ("Radius", float) = 1
      _Color ("Color", Color) = (1,1,1,1)
   }
   SubShader
   {
      Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
      Blend SrcAlpha OneMinusSrcAlpha
      ColorMask RGB
      ZWrite Off
      Cull Off
     
      Pass
      {
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
             
         #include "UnityCG.cginc"
     
         struct appdata
         {
            float4 vertex : POSITION;
         };
     
         struct v2f
         {
            float4 vertex : SV_POSITION;
            float3 worldPos : TEXCOORD0;
         };
             
         v2f vert (appdata v)
         {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.worldPos = mul (unity_ObjectToWorld, v.vertex);
            return o;
         }
     
         float3 _Position;
         float _Radius;
         fixed4 _Color;
     
         fixed4 frag (v2f i) : SV_Target
         {
            float dist = distance(i.worldPos, _Position);
     
            float alpha = 1.0;

            if (dist > _Radius) {
               alpha = 0.0;
            } else if (dist > _Radius - 0.02) {
               alpha = 0.5;
            } else if (dist > _Radius - 0.08) {
               alpha = 0.23;
            } else if (dist > _Radius - 0.18) {
               alpha = 0.15;
            } else if (dist > _Radius - 0.30) {
               alpha = 0.11;
            } else if (dist > _Radius - 0.44) {
               alpha = 0.05;
            } else {
               alpha = 0.0;
            }
     
            return fixed4(_Color.rgb, _Color.a * alpha);
         }
         ENDCG
      }
   }
}
