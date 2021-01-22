Shader "Arcane Waters/Attack Range Cone"
{
    Properties
    {
        _Position("Position", Vector) = (0.0, 0.0, 0.0)
        _Radius("Radius", float) = 1
        _InnerRadius("Inner Radius", float) = 0.1
        _HalfAngle("Half Angle", float) = 20
        _MiddleAngle("Middle Angle", float) = 0
        _ColorChangeWeight("Color Change Weight", float) = 1
        _Color("Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float3 _Position;
            float _Radius;
            float _InnerRadius;
            float _HalfAngle;
            float _MiddleAngle;
            float _ColorChangeWeight;
            fixed4 _Color;

            int angleIsValid(float angle)
            {
                return (cos(angle - radians(_MiddleAngle)) > cos(radians(_HalfAngle)));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldPos = i.worldPos;
                worldPos.z = 0.0;
                float dist = distance(worldPos, _Position);
                float2 toPoint = normalize(i.worldPos - _Position);
                float angle = degrees(atan2(toPoint.x, toPoint.y));
                if (angle < 0)
                {
                    angle += 360;
                }

                float alpha = 1.0;
                float outerDist = dist - _InnerRadius;
                float outerRadius = _Radius - _InnerRadius;

                if (dist > _Radius || dist < _InnerRadius || !angleIsValid(radians(angle))) {
                    alpha = 0.0;
                } else if (outerDist > outerRadius * 0.95) {
                    alpha = 0.5;
                } else if (outerDist > outerRadius * 0.7) {
                    alpha = 0.25;
                } else if (outerDist > outerRadius * 0.5) {
                    alpha = 0.17;
                } else if (outerDist > outerRadius * 0.3) {
                    alpha = 0.13;
                } else if (dist > _InnerRadius) {
                    alpha = 0.1;
                } else {
                    alpha = 0.0;
                }

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
        ENDCG
        }
    }
}
