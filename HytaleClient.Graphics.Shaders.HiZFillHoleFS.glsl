#version 330 core
#extension GL_ARB_gpu_shader5 : enable

#define PIXELSIZE (uResolutions.zw)
#include "Fallback_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
// none : we only write the gl_FragDepth

//-------------------------------------------------------------------------------------------------------------------------

bool isEmpty(float z)
{
    return z > 0.9999;
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    float z = 1;
    const int SIZE = 3;
    float samples[SIZE * SIZE];

    // If we have access to the hardware feature textureGather, we use it to reduce the sample count from 9 to 4.
    // But the sampling pattern remains the same in both scenarios.

#ifndef GL_ARB_gpu_shader5

    // Sampling pattern :
    //
    // X X X
    // X X X 
    // X X X

    samples[0] = textureOffset(uDepthTexture, fragTexCoords, ivec2(-1,-1)).r;
    samples[1] = textureOffset(uDepthTexture, fragTexCoords, ivec2(0,-1)).r;
    samples[2] = textureOffset(uDepthTexture, fragTexCoords, ivec2(1,-1)).r;
    samples[3] = textureOffset(uDepthTexture, fragTexCoords, ivec2(-1,0)).r;
    samples[4] = textureOffset(uDepthTexture, fragTexCoords, ivec2(0,0)).r;
    samples[5] = textureOffset(uDepthTexture, fragTexCoords, ivec2(1,0)).r;
    samples[6] = textureOffset(uDepthTexture, fragTexCoords, ivec2(-1,1)).r;
    samples[7] = textureOffset(uDepthTexture, fragTexCoords, ivec2(0,1)).r;
    samples[8] = textureOffset(uDepthTexture, fragTexCoords, ivec2(1,1)).r;
#else

    // Sampling pattern :
    //
    // B B A A      o X X X
    // B B A A  =>  o X X X
    // C C D D      o X X X
    // C C D D      o o o o

    vec4 zA = textureGatherOffset (uDepthTexture, fragTexCoords, ivec2(0,0), 0);
    vec4 zB = textureGatherOffset (uDepthTexture, fragTexCoords, ivec2(-2,0), 0);
    vec4 zC = textureGatherOffset (uDepthTexture, fragTexCoords, ivec2(-2,-2), 0);
    vec4 zD = textureGatherOffset (uDepthTexture, fragTexCoords, ivec2(0,-2), 0);
    
    // NB : textureGatherOffset returns the value
    // vec4(Sample_i0_j1(P + offset, base).comp,
    //      Sample_i1_j1(P + offset, base).comp,
    //      Sample_i1_j0(P + offset, base).comp,
    //      Sample_i0_j0(P + offset, base).comp);

    samples[0] = zC.y;
    samples[1] = zD.x;
    samples[2] = zD.y;
    samples[3] = zB.z;
    samples[4] = zA.w;
    samples[5] = zA.z;
    samples[6] = zB.y;
    samples[7] = zA.x;
    samples[8] = zA.y;
#endif

    // init z
    z = samples[4];    

    // To fill holes in the reprojection, we cannot rely on a simple 3x3 dilation as Crytek does.
    // So here is what we do instead...
    // We call "empty" a pixel whose value was never written (so z = 1).
    // Otherwise it's "full".
    // The rest of the code is essentially heuristics to decide if we must fill a hole.
    // And if we must do so, we will simply take the farthest (non empty) value in the neighbourhood.

    // get stats: check the 3 pixels above
    int fullPixelAboveCount = 0;
    for (int i = 0; i < SIZE; i++)
    {
        fullPixelAboveCount += !isEmpty(samples[SIZE * SIZE - 1 - i]) ? 1 : 0;

        //// this would check the 3 pixels below... keep it for quick tests
        //fullPixelAboveCount += !isEmpty(samples[i]) ? 1 : 0;
    }

    // now swaps 4 & 0 to make the next "for loops" easier
    float sample0 = samples[0];
    samples[0] = samples[4];
    samples[4] = sample0;

    // get more stats: check all 8 surrounding pixels
    int fullPixelCount = 0;
    for (int i = 1; i < SIZE * SIZE; i++)
    {   
        fullPixelCount += !isEmpty(samples[i]) ? 1 : 0;
    }

    // we have a hole to fill if:
    // it's empty & has enough full pixels around & has some full pixels above (y+1)
    if (isEmpty(z)
    && fullPixelCount >=4
    && fullPixelAboveCount >= 2
    )
    {
        // replace it w/ the max value != 1 in the neighbourhood
        for (int i = 1; i < SIZE * SIZE; i++)
        {
            // process only full pixels
            if (!isEmpty(samples[i]))
            {
                // init z if it was empty so far
                if (isEmpty(z))
                {
                    z = samples[i];
                }
                else
                {
                    z = max(z, samples[i]);
                }
            }
        }
    }

    gl_FragDepth = z; 
}
