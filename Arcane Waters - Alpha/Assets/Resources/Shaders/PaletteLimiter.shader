Shader "Hidden/PaletteLimiter"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Palette ("Palette", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _Palette;
            float4 _Palette_TexelSize;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 tex = 0;

                tex.x = clamp(floor(col.r * 15.0f), 0, 15); //RED
                tex.x /= 255.0f; //RED
                tex.y = 1.0f - col.g; //GREEN
                uint bucket = clamp((floor(col.b * 255.0f)) / 16, 0, 15); //BLUE
                tex.x += (float)bucket / 16.0f; //BLUE

                return float4(tex2D(_Palette, tex).rgb, col.a);
				
                // Iteration version of palette limit shader - use nearest product based on float3 distance

                //const uint width = (uint)_Palette_TexelSize.z;
                //const uint height = (uint)_Palette_TexelSize.w;
                //const float texelSize = 1.0f / width;

                //float4 nearestColor = float4(0, 0, 0, 0);
                //float dotDiff = 100;

                //for (uint x = 0; x < width; x++) {
                //    for (uint y = 0; y < height; y++) {
                //        float3 texSample = tex2D(_Palette, float2((float)x / width, (float)y / height));
                //        float3 tmpDiff = float3(abs(col.r - texSample.r), abs(col.g - texSample.g), abs(col.b - texSample.b));
                //        float tmpDiffDot = dot(tmpDiff, tmpDiff);

                //        if (tmpDiffDot < dotDiff) {
                //            nearestColor = float4(texSample, col.a);
                //            dotDiff = tmpDiffDot;
                //        }

                //    }
                //}
                //return nearestColor;
            }
            ENDCG
        }
    }
}
