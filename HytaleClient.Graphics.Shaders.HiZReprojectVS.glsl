#version 330 core
#extension GL_ARB_gpu_shader5 : enable

#define PIXELSIZE (uResolutions.zw)
#include "Fallback_inc.glsl"
#include "Reconstruction_inc.glsl"

#define USE_LINEAR_Z 1
#define USE_FAST_REPROJECTION 0

#ifndef MIN_REPROJECTION_Z 
#define MIN_REPROJECTION_Z 16
#endif

// Keeping this below 32 should ensure loop unrolling from the compiler
#ifndef MAX_INVALID_AREAS
#define MAX_INVALID_AREAS 10
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

uniform vec4 uResolutions;
uniform mat4 uProjectionMatrix;
uniform mat4 uReprojectMatrix;
uniform vec4 uInvalidScreenAreas[MAX_INVALID_AREAS];

#define RESOLUTION_POINTGRID_X uResolutions.x
#define RESOLUTION_POINTGRID_Y uResolutions.y
#define RESOLUTION_TEXTURE_X uResolutions.z
#define RESOLUTION_TEXTURE_Y uResolutions.w

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
// none : we only write the gl_Position

//-------------------------------------------------------------------------------------------------------------------------

const float FarClip = 1024.0f;

vec4 ProjInfo = vec4(    -2.0f / uProjectionMatrix[0][0],
                          -2.0f / uProjectionMatrix[1][1],
                          ( 1.0f - uProjectionMatrix[0][2]) / uProjectionMatrix[0][0],
                          ( 1.0f + uProjectionMatrix[1][2]) / uProjectionMatrix[1][1]);
                          
vec3 ReprojectToCurrentFrameProjectionSpace(vec3 previousPos, mat4 reprojectMatrix)
{
    vec4 vProjectedPos = vec4(previousPos, 1.0f);
    vec4 prevFrameCoords = reprojectMatrix * vProjectedPos;
    prevFrameCoords.xyz /= prevFrameCoords.w;

    return prevFrameCoords.xyz;
}

bool isInside(vec2 pointSS, vec4 rectangleMinMaxSS)
{
#define minX rectangleMinMaxSS.x
#define minY rectangleMinMaxSS.y
#define maxX rectangleMinMaxSS.z
#define maxY rectangleMinMaxSS.w
    return minX <= pointSS.x && pointSS.x <= maxX && minY <= pointSS.y && pointSS.y <= maxY;
#undef minX
#undef minY
#undef maxX
#undef maxY
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    // We want to reproject pixels from the previous frame z buffer into the current frame occlusion map.
    // The occlusion map is already filled w/ pixels from the nearest occluders.
    // Here we want to complete it with pixels a bit more distant.
    // So :
    // 0. Compute screen pos & reject if in an invalid area
    // 1. Fetch the previous z
    // 2. Reconstruct its position in ViewSpace
    // 3. Reject if too close to the camera
    // 4. Reproject to the current frame

    // 0. Compute screen pos...
    float width = RESOLUTION_POINTGRID_X;
    float height = RESOLUTION_POINTGRID_Y;

    float x = float(gl_VertexID % int(width)) + 0.5;
    float y = float(gl_VertexID / int(width)) + 0.5;


    vec2 texCoords = vec2(x/width, y/height);

    // 0. ... and reject if in one of the invalid rectangles
    int invalidCount = 0;
    for (int i = 0; i < MAX_INVALID_AREAS; i++)
    {
        invalidCount += isInside(texCoords, uInvalidScreenAreas[i]) ? 1 : 0;
    }

    if (invalidCount > 0)
    {
        // Send it out of scope !
        gl_Position = vec4(2,2,2,1);
        return;
    }

    // 1. Fetch the previous z
    // Actually we fetch a few values, and keep only the max (= the more distant one).
    float z = 0;

    // TODO : make sure we read enough / all the necessary texel, depending on the input res.

#if USE_FAST_REPROJECTION
    // Fast reprojection only uses 1 textureGather, which means 2x2 samples around the reprojected pixel.
    // If the input texture is half res downsampled w/ a max (or min-max) filter,
    // then it's roughly equivalent to having a 4x4 kernel.
    vec4 fourZ = TextureGather(uDepthTexture, texCoords, 0);
    z = max(max(fourZ.x, fourZ.y), max(fourZ.z, fourZ.w));
#else
    // Safe reprojection searches for more info, to avoid false occluder pixels.
    // In practice we use a 2x2 textureGather, which means 4x4 samples around the reprojected pixel.
    // If the input texture is a half res downsampled w/ a (max or min-max) filter,
    // then it's roughly equivalent to having a 8x8 kernel.

    vec4 fourZA = TextureGatherOffsetR(uDepthTexture, texCoords, ivec2(0,0));
    vec4 fourZB = TextureGatherOffsetR(uDepthTexture, texCoords, ivec2(0,2));
    vec4 fourZC = TextureGatherOffsetR(uDepthTexture, texCoords, ivec2(2,0));
    vec4 fourZD = TextureGatherOffsetR(uDepthTexture, texCoords, ivec2(2,2));

    z = max(z,max(max(fourZA.x, fourZA.y), max(fourZA.z, fourZA.w)));
    z = max(z,max(max(fourZB.x, fourZB.y), max(fourZB.z, fourZB.w)));
    z = max(z,max(max(fourZC.x, fourZC.y), max(fourZC.z, fourZC.w)));
    z = max(z,max(max(fourZD.x, fourZD.y), max(fourZD.z, fourZD.w)));

#endif

#if USE_LINEAR_Z
//Use a bias on the linear Z data to compensate the lack of precision.
const float linearZPrecisionBias = 0.5;
const float minReprojectionZ = MIN_REPROJECTION_Z + linearZPrecisionBias;
float linearDepth = z * FarClip + linearZPrecisionBias;
#else
float linearDepth = GetLinearDepthFromDepthHW(z, uProjectionMatrix);
const float minReprojectionZ = MIN_REPROJECTION_Z;
#endif

    // 3. Reject if too close to the camera
    // Reject the pixels too close to the camera,
    // to avoid false occlusion due to moving objects near the camera.
    // The near geometry should be filled already when drawing the nearest occluders,
    // which is done before this reprojection pass.
    if (linearDepth < minReprojectionZ)
    {
        // Send it out of scope.
        gl_Position = vec4(2,2,2,1);
        return;
    }

    // 2. Reconstruct its position in ViewSpace
#if USE_LINEAR_Z
    vec3 previousPos = PositionFromLinearDepth(texCoords, -linearDepth, ProjInfo);
#else
    vec3 previousPos = PositionFromDepthHW(z, texCoords, uProjectionMatrix, ProjInfo);
#endif    

    // 3. Reproject to the current frame
    vec3 posPS = ReprojectToCurrentFrameProjectionSpace(previousPos, uReprojectMatrix);

    // DEBUG
    //vec3 posPS = vec3(texCoords.xy, z);
    //posPS = posPS * vec3(2.0) - vec3(1.0);

    gl_Position = vec4(posPS, 1);
//    gl_PointSize = 100;
}
