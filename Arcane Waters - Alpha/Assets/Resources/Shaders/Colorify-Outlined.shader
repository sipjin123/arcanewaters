Shader "Arcane Waters/Colorify Outlined" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _MainTex2 ("_MainTex2", 2D) = "white" {}
    _PatCol ("Pattern Color", Color) = (1,1,1,1)
    _NewColor ("New Color", Color) = (1,1,1,1)
    _Range ("Range", Range (0.0, 2.0)) = 0.01
    _PatCol2 ("Pattern Color 2", Color) = (1,1,1,1)
    _NewColor2 ("New Color 2", Color) = (1,1,1,1)
    _Range2 ("Range 2", Range (0.0, 2.0)) = 0.01
    _WaterHeight("Water Height", Range(0, 1)) = 0.0
    _WaterAlpha("Water Alpha", Range(0, 1)) = 0.0

    // Outline section
    [Header(Outline)]
    [Toggle(OUTLINE_ENABLED)]

    // Keyword to enable/disable outline
    _OutlineEnabled("Outline Enabled", Float) = 0

    // The color of the outline
    _OutlineColor("Outline Color", Color) = (1,1,1,1)
    [Toggle(ALPHA_THRESHOLD)]

    // Keyword to enable/disable alpha threshold (for outline only)
    _UseAlphaThreshold("Enable Alpha Threshold", float) = 0

    // The minimum alpha value to be considered for the outline
    _AlphaThreshold("Alpha Threshold", Range(0, 1)) = 0.25
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100
    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off
    ZWrite On

    Pass
    {
        Stencil {
            Ref 125
            Comp Always
            Pass Replace
        }

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature SWAP_TEXTURE

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _MainTex2;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _PatCol;
            fixed4 _NewColor;
            half _Range;
            fixed4 _PatCol2;
            fixed4 _NewColor2;
            half _Range2;
            half _SwapTex;
            uniform float _WaterHeight;
            uniform float _WaterAlpha;
            float _AlphaThreshold;
            float _UseAlphaThreshold;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                sampler2D mainTex = _MainTex;

                #ifdef SWAP_TEXTURE
                    mainTex = _MainTex2;
                #endif

                fixed4 c = tex2D(mainTex, i.texcoord) * _Color;

                // Discard pixels that shouldn't be outlined
                if (c.a < _AlphaThreshold) {
                    clip(-1);
                } else {
                    c.rgb = lerp(lerp(c.rgb,(_NewColor.rgb - _PatCol.rgb + c.rgb),
                             saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range))),
                        (_NewColor2.rgb - _PatCol2.rgb + c.rgb),
                        saturate(1 - ((c.r - _PatCol2.r)*(c.r - _PatCol2.r) + (c.g - _PatCol2.g)*(c.g - _PatCol2.g) + (c.b - _PatCol2.b)*(c.b - _PatCol2.b)) / (_Range2 * _Range2)));

                    if ((i.texcoord.y * 6) % 1 < _WaterHeight && c.a > 0) {
                        c.rgb = lerp(c.rgb, half3(.24, .39, .62), .90);
                        c.a = lerp(0.0, c.a, _WaterAlpha);
                    }
                }
                return c;
            }
        ENDCG
    }

    Pass
    {
        // This pass only renders pixels that were discarded in the previous pass do to alpha threshold, without any modifications
        Stencil {
            Ref 125
            Comp NotEqual
            Pass IncrSat
        }

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"


            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct appdata {
                float4 vertex: POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v) {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }


            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }
        ENDCG
    }

    Pass
    {
        //This pass will only run if the pixel was skipped by alpha threshold
        Stencil {
            Ref 125
            Comp NotEqual
        }

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _OutlineEnabled;
            fixed4 _OutlineColor;
            float _AlphaThreshold;
            float _UseAlphaThreshold;

            v2f vert(appdata v) {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target  {
                fixed4 c = tex2D(_MainTex, i.uv);

                // Whether or not we should check for alpha values
                float checkAlpha = _UseAlphaThreshold;

                // Whether or not the pixel is eligible for outline (if it's completely transparent or less than _AlphaThreshold)
                float isValid = checkAlpha ? c.a <= _AlphaThreshold : c.a == 0;

                // Only outline if Outline is enabled and the pixel is eligible to be an outline pixel
                // Discard the pixel otherwise
                if (_OutlineEnabled > 0 && isValid) {
                    // The alpha value a pixel should have to be outlined
                    float a = checkAlpha ? _AlphaThreshold : 0;

                    // We grab the "pixels" (technically texels) in every direction
                    fixed4 up = tex2D(_MainTex, i.uv + fixed2(0, _MainTex_TexelSize.y));
                    fixed4 down = tex2D(_MainTex, i.uv - fixed2(0, _MainTex_TexelSize.y));
                    fixed4 right = tex2D(_MainTex, i.uv + fixed2(_MainTex_TexelSize.x, 0));
                    fixed4 left = tex2D(_MainTex, i.uv - fixed2(_MainTex_TexelSize.x, 0));

                    // If any of the texels is *not* transparent (or alpha is lower than _AlphaThreshold), we paint it.
                    // Otherwise we discard the pixel
                    if (up.a > a || down.a > a || right.a > a || left.a > a) {
                        c.rgba = fixed4(1, 1, 1, 1) * _OutlineColor;
                    } else {
                        clip (-1);
                    }
                } else {
                    clip(-1);
                }

                return c;
            }
        ENDCG
    }
}
}
