#version 330 core

#include "Deferred_inc.glsl"
#include "ModelVFX_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture0;
uniform sampler2D uTexture1;
uniform sampler2D uTexture2;

#if USE_BIAS_METHOD_2
uniform mat4 uViewMatrix;
#endif

#if USE_DRAW_INSTANCED
#define MAX_CASCADES 4
uniform vec2 uViewportInfos[MAX_CASCADES];
#endif

#if USE_MODEL_VFX
uniform sampler2D uNoiseTexture;

uniform float uTime;

#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
flat in int fragAtlasIndex;

#if USE_BIAS_METHOD_2
in vec3 fragPositionVS;
#endif

#if USE_DRAW_INSTANCED
flat in int fragCascadeId;
#endif

#if USE_MODEL_VFX
in vec3 fragTexCoords;
#define HeightFactor fragTexCoords.z

flat in ivec2 fragModelVFXUnpackedData;
#define modelVFXDirection (fragModelVFXUnpackedData.x)
#define modelVFXSwitchTo (fragModelVFXUnpackedData.y)

flat in vec4 fragModelVFXNoiseParams;
#define modelVFXNoiseScale (fragModelVFXNoiseParams.xy)
#define modelVFXNoiseScrollSpeed (fragModelVFXNoiseParams.zw)

flat in vec2 fragInvModelHeightAnimationProgress;
#define invModelHeight fragInvModelHeightAnimationProgress.x
#define modelVFXAnimationProgress fragInvModelHeightAnimationProgress.y

#else
in vec2 fragTexCoords;
#endif // USE_MODEL_VFX

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
// none : we write in gl_FragDepth (explicitely or not)

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    // We can't use a ternary conditional operator inside the texture(...) call here
    // because some drivers will fail to link the program with it
    vec4 texel;
    if (fragAtlasIndex == 0) texel = texture(uTexture0, fragTexCoords.xy);
    else if (fragAtlasIndex == 1) texel = texture(uTexture1, fragTexCoords.xy);
    else texel = texture(uTexture2, fragTexCoords.xy);

    // Discard for fragment too close to the camera or discard if texel is fully transparent 
    if (gl_FragCoord.z < 0.55 || (texel.a == 0.0)) discard;

#if USE_MODEL_VFX
    if(modelVFXAnimationProgress != 0.0)
    {
        bool vfxDiscard = false;
        bool hasHighlight = false;
        vec3 highlightColor = vec3(0);
        vec4 color = vec4(0);

        const int SWITCH_TO_DISAPPEAR = 0;
        const int SWITCH_TO_POST_COLOR = 1;
        const int SWITCH_TO_DISTORTION = 2;

        bool useDiscard = modelVFXSwitchTo == SWITCH_TO_DISAPPEAR || modelVFXSwitchTo == SWITCH_TO_DISTORTION;
        bool usePostDisappearColor = modelVFXSwitchTo == SWITCH_TO_POST_COLOR;
        
        vfxDiscard = ComputeModelVFX(fragTexCoords.xy, uNoiseTexture, invModelHeight, HeightFactor, modelVFXAnimationProgress, modelVFXDirection, highlightColor, false , false, 0.01, modelVFXNoiseScale, modelVFXNoiseScrollSpeed, vec4(0), color, hasHighlight, uTime);

        if (useDiscard && vfxDiscard) discard;
    }
#endif // USE_MODEL_VFX

    // To reduce acne, we use some biasing during the shadow map generation.
    // Method 0 takes place on the CPU, by leveraging the GPU hardware, with the use of gl.PolygonOffset(x,y).

    // Method 1 - hacky
#if USE_BIAS_METHOD_1
    float slopeBias = max( abs(dFdx(gl_FragCoord.z)), abs(dFdy(gl_FragCoord.z)));
    slopeBias *= 10;
    gl_FragDepth = gl_FragCoord.z + slopeBias;// + 0.0001;
#endif

#if USE_BIAS_METHOD_2
    // Method 2 - okayish
    // ...variable bias
    float bias = 0.00000005;
    vec3 lightDir = vec3(0,0,-1);//uViewMatrix[3].xyz; //
    lightDir = uViewMatrix[3].xyz; // 
    vec3 normalVS = fastNormalFromPosition(fragPositionVS);
    float cosTheta = clamp( dot( normalVS, lightDir ), 0,1 );
    bias *= tan(acos(cosTheta)); // bias *= sqrt(1.0 - cosTheta * cosTheta) / cosTheta;
    bias = clamp(bias, 0,0.01);    
    gl_FragDepth = gl_FragCoord.z + bias ;//slopeBias;// + 0.0001;
#endif

    //float dx = abs(dFdx(gl_FragCoord.z));
    //float dy = abs(dFdy(gl_FragCoord.z));
    //gl_FragDepth = gl_FragCoord.z + sqrt(dx*dx + dy*dy);

#if USE_DRAW_INSTANCED
    vec2 scaleSize = uViewportInfos[0];
    float texelX = (gl_FragCoord.x / scaleSize.y) / scaleSize.x;
    if (texelX < fragCascadeId || texelX > (fragCascadeId + 1)) discard;
#endif
}
