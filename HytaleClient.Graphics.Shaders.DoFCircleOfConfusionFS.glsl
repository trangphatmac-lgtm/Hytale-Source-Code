#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

uniform float uNearBlurry;
uniform float uNearSharp;
uniform float uFarSharp;
uniform float uFarBlurry;

#if USE_LINEAR_Z
uniform float uFarClip = 1024.0f;
#else
uniform mat4 uProjectionMatrix;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec2 outCoC;

//-------------------------------------------------------------------------------------------------------------------------

#if !USE_LINEAR_Z
float getLinearDepthFromDepthNDC(float depthNDC)
{
    return uProjectionMatrix[3][2] / (depthNDC + uProjectionMatrix[2][2]);
}
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
#if USE_LINEAR_Z
    float depth = texture(uDepthTexture, fragTexCoords.xy).r;
    float linearDepth = depth * uFarClip;
#else
    float depth = texture(uDepthTexture, fragTexCoords.xy).r * 2.0 - 1.0;
    float linearDepth = getLinearDepthFromDepthNDC(depth);
#endif
    float CoCNear = 0.0f;
    if(uNearSharp != uNearBlurry)
    {
        float coefDirNear = -1.0f / (uNearSharp - uNearBlurry);
        CoCNear = step(linearDepth, uFarSharp)*clamp(coefDirNear * (linearDepth - uNearBlurry) + 1.0f, 0, 1.0f);
    }

    float coefDirFar  = (uFarBlurry == uFarSharp) ? 0.0f : 1.0f / (uFarBlurry - uFarSharp);

    float CoCFar = step(uFarSharp, linearDepth)*clamp(coefDirFar * (linearDepth - uFarSharp), 0, 1.0f);
 
    outCoC.r = CoCNear;
    outCoC.g = CoCFar;
}
