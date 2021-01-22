#ifndef UNITY_PALETTE_PER_SPRITE_INCLUDED
#define UNITY_PALETTE_PER_SPRITE_INCLUDED

#include "UnityCG.cginc"

half3 swapPalette(float4 col, sampler2D Palette, float threshold)
{    
    half2 xVec = half2((col.r * 255) * (col.g * 255), max(col.b, col.r) * 255);
    half2 yVec = half2((col.g * 255) * (col.b * 255), max(col.r, col.g) * 255);
    float x = round(sqrt(length(xVec))) / 255;
    float y = round(sqrt(length(yVec))) / 255;

    // Sample the palette texture using the same magic formula we use for creating it
    half4 outputCol = tex2D(Palette, float2(x, y));

    // Check whether the color is defined in the texture by making sure the distance from the color to 0 is greater than [threshold]
    half isValid = max(sign(distance(outputCol.a, 0) - threshold), 0.0);

    // If the palette color is defined, change it. Otherwise, set the original color.
    outputCol = lerp(col, outputCol, isValid);

    return outputCol;
}

#endif // UNITY_STANDARD_INPUT_INCLUDED
