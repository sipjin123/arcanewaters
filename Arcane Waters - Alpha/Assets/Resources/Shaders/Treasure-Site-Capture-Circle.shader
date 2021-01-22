Shader "Arcane Waters/Treasure Site Capture Circle"
{
   Properties
   {
      _Position ("Position", Vector) = (0.0, 0.0, 0.0)
      _Rotation ("Rotation", float) = 0
      _Radius ("Radius", float) = 1
      _FillAmount ("Fill Amount", range(0, 1)) = 0.5
      _Color ("Color", Color) = (1,1,1,1)
      _DashLength ("Dash Length", float) = 10
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
         float _Rotation;
         float _Radius;
         float _FillAmount;
         fixed4 _Color;
         float _DashLength;
     
         fixed4 frag (v2f i) : SV_Target
         {
            float dist = distance(i.worldPos, _Position);
            float3 relPos = i.worldPos - _Position;
            float angle = degrees(atan2(relPos.y, relPos.x)) + _Rotation;
     
            // Set the base alpha
            float alpha = 0.05 * step(dist, _Radius);

            // Fill the circle
            alpha += 0.15 * step(dist, _FillAmount * _Radius);

            // Draw the dotted line
            float angleSegment = frac(angle/(2 * _DashLength));
            if (dist > _Radius - 0.02 && angleSegment < 0.5) {
               alpha = 0.5;
            }
            
            // Set alpha to zero outside of the circle
            alpha *= step(dist, _Radius);
     
            return fixed4(_Color.rgb, _Color.a * alpha);
         }
         ENDCG
      }
   }
}
