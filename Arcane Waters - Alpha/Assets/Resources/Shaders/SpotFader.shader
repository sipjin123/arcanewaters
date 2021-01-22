Shader "Effects/SpotFader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Progress("Progress", Float) = 0
        _SpotPosition("Spot Position", Vector) = (0, 0, 0, 0)
        _ScreenSize("Screen Size", Vector) = (1024, 768, 0, 0)
        _DitherPercent("Dither Percent", Float) = 0.2
    }

    SubShader
    {
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v, out float4 outpos : SV_Position)
            {
                v2f o;                
                o.uv = v.uv;
                outpos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float _Progress;
            sampler2D _MainTex;
            float4 _SpotPosition;
            float4 _ScreenSize;
            float _PixelSize;
            float _DitherPercent;

            fixed greater_than(float x, float y)
            {
                return max(sign(x - y), 0.0);
            }

            fixed less_than(float x, float y)
            {
                return max(sign(y - x), 0.0);
            }

            fixed greater_equals(float x, float y)
            {
                return 1.0 - less_than(x, y);
            }

            fixed less_equals(float x, float y)
            {
                return 1.0 - greater_than(x, y);
            }

            fixed equals(float x, float y)
            {
                return 1.0 - abs(sign(x - y));
            }

            fixed and(float x, float y)
            {
                return x * y;
            }

            fixed or(float x, float y)
            {
                return min(x + y, 1.0);
            }

            fixed not(float x)
            {
                return 1 - x;
            }

            float isOutOfSpot(float2 screenPos, float radius, float2 spotPosition)
            {
                float2 spotPos = (spotPosition.xy * _ScreenSize.xy) * (1 / _PixelSize * 0.5) * 0.5;
                float dist = distance(screenPos, spotPos);
                
                return clamp(lerp(1, 0, sign(radius - dist)), 0, 1);
            }

            fixed getCheckerboard(float2 screenPos, float ditherRadius, float spotRadius, float2 spotPosition)
            {
                float squareSize = 1 / _PixelSize * 0.5;
                screenPos.xy = floor(screenPos.xy * squareSize) * 0.5;
                float checker = ceil(frac(screenPos.x - screenPos.y));
                
                return max(checker * isOutOfSpot(screenPos, ditherRadius, spotPosition), isOutOfSpot(screenPos, spotRadius, spotPosition));
            }

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // The position of the spot in pixel coordinates
                float2 spotPosition = _SpotPosition.xy * _ScreenSize.xy;

                // The position of the current pixel in screen coordinates
                float2 position = i.uv.xy * float2(_ScreenSize.x, _ScreenSize.y);

                // The radius needed for the spot to fill the screen
                float maxRadius = (max(_ScreenSize.x, _ScreenSize.y) * 0.65) / _PixelSize * 0.5;

                // The current radius of the circle
                float radius = (_Progress) * maxRadius;

                // The distance between this pixel and the center of the spot
                float distanceToSpot = distance(position, spotPosition);
                
                // The dither radius, proportional to the radius of the bigger spot
                float innerRadius = radius * _DitherPercent;

                float checker = getCheckerboard(screenPos, innerRadius, radius, float2(_SpotPosition.x, 1 - _SpotPosition.y));

                col *= 1 - checker;

                return col;
            }
            ENDCG
        }
    }
}
