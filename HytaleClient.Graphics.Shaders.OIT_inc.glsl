#ifndef OIT_INCLUDE
#define OIT_INCLUDE

#include "MomentsOIT_inc.glsl"
#include "WBOIT_inc.glsl"

// This requires some constants / uniforms / varyings to be available:
// uniform sampler2D uMomentsTexture;
// uniform sampler2D uTotalOpticalDepthTexture;
// uniform ivec2 uOITParams;
//
// const int BLEND_MODE_LINEAR = 0;
// const int BLEND_MODE_ADD = 1;

#define uOITFallbackToSingleBufferBlend uOITParams.y
#define uOITMethod uOITParams.x
#define OIT_NONE 0
#define OIT_WBOIT 1
#define OIT_WBOIT_EXTENDED 2
#define OIT_POIT 3
#define OIT_MT_STEP1 4
#define OIT_MT_STEP2 5

void processOIT(vec4 color, float depthVS, float sceneLinearDepth, vec2 screenUV, int blendMode, out vec4 output0, out vec4 output1)
{
#if USE_OIT
    vec3 transmit = vec3(1);

switch (uOITMethod)
{
case OIT_WBOIT:
{
    switch (blendMode)
    {
    case BLEND_MODE_LINEAR:
        color.rgb *= color.a;
        transmit = vec3(color.rgb*0.001);
        break;
    case BLEND_MODE_ADD: 
        color.rgb *= color.a;
//        color.a = 0;
        transmit = vec3(0.75);//color.rgb * 1.5;
        break;
    }

    vec4 accumulation;
    float reveal;
    processWBOIT(accumulation, reveal, color.rgba, transmit, depthVS);
    output0 = accumulation;
    output1 = vec4(reveal, 0, 0, reveal);
}
break;

case OIT_POIT:
{
    switch (blendMode)
    {
    case BLEND_MODE_LINEAR:
//        color.rgb *= color.a;
//        color.a *= 10.5;
        color.a *= 3;
        transmit = vec3(color.rgb * 0.01);
        break;
    case BLEND_MODE_ADD: 
//        color.rgb *= color.a;
        color.a *= 5;
//        color.a *= -30/depthVS;
        transmit = vec3(0.3);//color.rgb* max(color.a, 0.5));//color.rgb * 1.5;
        break;
    }

    vec3 L_r = color.rgb;
    float alpha = color.a;// * (500/abs(depthVS*depthVS));
    vec3 t = transmit;
    float c = 0.88;//0.88;
    float eta;
    vec3 X = vec3(0, 0, depthVS);
    vec3 n = vec3(0,0,1); // normal
    float linearZBuffer = sceneLinearDepth;
    vec4 A;
    vec3 beta;
    float D;
    vec2 delta;
    processPOIT(L_r, alpha, t, c, eta, X, n, linearZBuffer, A, beta, D, delta);
    output0 = A;
    output1 = vec4(beta, D);
}
break;
case OIT_WBOIT_EXTENDED:
{
    float luminance = 1;

    switch (blendMode)
    {
    case BLEND_MODE_LINEAR:
        color.rgb *= color.a;
        transmit = vec3(color.rgb*0.001);
        luminance = 0.5;
        break;
    case BLEND_MODE_ADD: 
        luminance = 0.5;
        color.rgb *= color.a;
//        color.a = 0;
        transmit = vec3(0.75);//color.rgb * 1.5;
        break;
    }

    // ... vec4 premultiplied_alpha_color, float raw_emissive_luminance, float view_depth, float current_camera_exposure
    vec4 accumulation;
    float reveal;
    float addWeight = 0;
    //processWBOIT_e(accumulation, reveal, addWeight, color.rgba, transmit, depthVS);
    processWBOITExtended(accumulation, reveal, addWeight, color.rgba, luminance, depthVS, 1);
    output0 = accumulation;
    output1 = vec4(reveal, 0, 0, addWeight);
}
break;
case OIT_MT_STEP1:
{
    vec4 moments;
    float totalOpticalDepth;

    WriteMoments(
        -depthVS,       //-vViewPos.z,            //  z
        color.a,            //  alpha
        moments,            //  o_moments
        totalOpticalDepth); //  o_opticalDepth

        output0.rgba = moments;
        output1.r = totalOpticalDepth;
}
break;
case OIT_MT_STEP2:
{
    vec4 moments = texture(uMomentsTexture, screenUV);
    float totalOpticalDepth = texture(uTotalOpticalDepthTexture, screenUV).r;

    float weight = w(
        -depthVS,       //-vViewPos.z,    //  z
        color.a,            //  alpha
        moments,            //  moments
        totalOpticalDepth); //  totalOD

    // Hack in some additiveness approximation.
    color.rgb += (blendMode == BLEND_MODE_ADD) ? vec3(0.2) : vec3(0);

    output0 = vec4(color.rgb * color.a, color.a) * weight;
    output1 = vec4(color.a); // TODO: optimize this
}
break;
}

// Hacky fallback: when the GPU does not support draw buffers blend,
// we draw the transparents a second time with a different blend func,
// and have to output the result of the output1 in the only draw buffer,
// i.e. output0.
if (uOITFallbackToSingleBufferBlend == 1)
{
    output0 = output1; 
}
#endif
}

#endif //OIT_INCLUDE
