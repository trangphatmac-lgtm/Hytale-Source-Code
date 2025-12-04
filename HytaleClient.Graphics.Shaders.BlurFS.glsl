#version 330 core

#include "Deferred_inc.glsl"

#ifndef BLUR_CHANNELS
#define BLUR_CHANNELS ra
#endif

#ifndef DEPTH_CHANNELS
#define DEPTH_CHANNELS gb
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform vec2 uPixelSize;
uniform float uBlurScale;
uniform float uHorizontalPass;

// This is a 9x9 gaussian blur, using separate horizontal & vertical passes.
// Using separate passes allow for 9+9 taps instead of 9x9, with exact same result.
// Then, we can reduce the number of taps for each pass from 9 to 5, 
// by leveraging the hardware texture filtering, and selecting smartly taps (and weight) right between 2 texels, 
// and the hardware will do part of the blur for us.
// This latter optimization is not possible if the weight are not const, like when doing edge aware blur.
// NB 1 : the weights are selected using Pascal's triangle, and dropping the extent values thus using only the 9 central values...
// which means we have the weight that should sum up to ~4070 instead of 4096.
// NB 2 : for linear samples, the division by 4070 is included, hence the difference.

// #define USE_EDGE_AWARENESS 0

#if USE_EDGE_AWARENESS
// Discrete samples - allows for edge aware blur (e.g. with depth)
const int N = 5;
const float offset[N] = float[]( 0.0, 1.0, 2.0, 3.0, 4.0 );
//const float weight[N] = float[]( 0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162 );
const float weight[N] = float[]( 923.99, 792, 494.99, 220, 65.99 );
#else
// Linear samples - optimized using the hardware linear texture filtering to minimize the number of fetches for similar quality
const int N = 3;
const float offset[N] = float[]( 0.0, 1.3846153846, 3.2307692308 );
const float weight[N] = float[]( 0.2270270270, 0.3162162162, 0.0702702703 );
//const float weight[N] = float[]( 923.99, 1286.99, 286 );
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 texel = texture( uColorTexture, fragTexCoords);
    
    vec4 color = texel;
    float linear01Depth = unpackDepth(texel.gb);

    float depthVS = linear01Depth * 1024.0;
    float maxScale = uBlurScale;//1.4;

#if USE_EDGE_AWARENESS && USE_CUSTOM_CHANNELS

    // Skip the sky - could be avoided but rendering a fullscreen quad with appropriate Z
    if (linear01Depth < 1)
    {

        float totalWeight = weight[0];
#endif

        color *= weight[0];

        float horizontalFactor = uHorizontalPass;
        float verticalFactor = 1.0f - horizontalFactor;

        for (int i = 1; i < N; i++) 
        {
            vec2 offsetA = vec2(offset[i] * verticalFactor, offset[i] * horizontalFactor);
            vec2 offsetB = vec2(-offset[i] * verticalFactor, -offset[i] * horizontalFactor);
        
        #if USE_EDGE_AWARENESS && USE_CUSTOM_CHANNELS

            float scale = clamp(( 0.01 * uBlurScale / linear01Depth), 0.1, maxScale);
            // scale = 1 * maxScale;
            vec2 texCoordA = fragTexCoords + offsetA * uPixelSize * scale;
            vec2 texCoordB = fragTexCoords + offsetB * uPixelSize * scale;

            vec4 colorA = texture( uColorTexture, texCoordA);
            vec4 colorB = texture( uColorTexture, texCoordB);

            float deltaDepthA = abs(unpackDepth(colorA.gb) - linear01Depth);
            float deltaDepthB = abs(unpackDepth(colorB.gb) - linear01Depth);

            // Edge aware blur
            const float EPSILON = 0.00001;
            float weightDepthA = 1.0f / (EPSILON + deltaDepthA);
            float weightDepthB = 1.0f / (EPSILON + deltaDepthB);
            // float weightDepthA = 1.0;
            // float weightDepthB = 1.0;
            // float weightDepthA = 1-step(EPSILON, deltaDepthA);
            // float weightDepthB = 1-step(EPSILON, deltaDepthB);
            // float weightDepthA = 1-smoothstep(0.0, EPSILON, deltaDepthA);
            // float weightDepthB = 1-smoothstep(0.0, EPSILON, deltaDepthB);

            // Another test
            //const float EPSILON = 0.001;
            //float weightDepthA = 1.0 / (depth * EPSILON + deltaDepthA);
            //float weightDepthB = 1.0 / (depth * EPSILON + deltaDepthB);

            totalWeight = weight[i] * (weightDepthA + weightDepthB) + totalWeight;
            color.BLUR_CHANNELS += (colorA.BLUR_CHANNELS * weightDepthA + colorB.BLUR_CHANNELS * weightDepthB) * weight[i];

        #elif USE_CUSTOM_CHANNELS
            vec2 texCoordA = fragTexCoords + offsetA * uPixelSize * uBlurScale;
            vec2 texCoordB = fragTexCoords + offsetB * uPixelSize * uBlurScale;

            color.BLUR_CHANNELS += texture( uColorTexture, texCoordA).BLUR_CHANNELS * weight[i];
            color.BLUR_CHANNELS += texture( uColorTexture, texCoordB).BLUR_CHANNELS * weight[i];
        #else
            color += texture( uColorTexture, fragTexCoords + offsetA * uPixelSize * uBlurScale) * weight[i];
            color += texture( uColorTexture, fragTexCoords + offsetB * uPixelSize * uBlurScale) * weight[i];
        #endif
        }

#if USE_EDGE_AWARENESS && USE_CUSTOM_CHANNELS

        color.BLUR_CHANNELS /= totalWeight;
    }
#endif

    // For debug
// if (uHorizontalPass) color.r = pow(smoothstep(0.4, 1.0, color.r), 2) ;

#if USE_CUSTOM_CHANNELS
    outColor.rgba = texel.rgba;
    outColor.BLUR_CHANNELS = color.BLUR_CHANNELS;

    //outColor.rgba = vec4(color.r, texel.gba);
    //outColor.rg = vec2(mix(texel.r, color.r, clamp(0.5, 1.0, 1-depth*10)), texel.g);
    //outColor.rgb = vec3(mix(texel.r, color.r, 1-depth*10), texel.gb);
#else
    outColor.rgba = color;
#endif
}
