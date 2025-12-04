#version 330 core

// Global scene data
#include "SceneData_inc.glsl"
#include "Distortion_inc.glsl"
#include "Reconstruction_inc.glsl"
#include "Deferred_inc.glsl"
//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
// Required before "OIT_inc.glsl"
const int BLEND_MODE_PREMULT_LINEAR = 0;
const int BLEND_MODE_ADD = 1;
const int BLEND_MODE_LINEAR = 2;

#ifndef USE_OIT
#define USE_OIT 0
#endif
uniform ivec2 uOITParams;

// Moments OIT related
uniform sampler2D uMomentsTexture;
uniform sampler2D uTotalOpticalDepthTexture;

#include "OIT_inc.glsl"

uniform sampler2D uTexture;
uniform sampler2D uDepthTexture;

uniform vec2 uCurrentInvViewportSize;
uniform ivec2 uDrawAndBlendMode;
#define uDrawMode uDrawAndBlendMode.x
#define uBlendMode uDrawAndBlendMode.y

uniform vec4 uColorOpacity;
uniform vec4 uIntersectionHighlightColorOpacity;
uniform float uIntersectionHighlightThickness;
uniform vec2 uUVAnimationSpeed;
uniform int uOutlineMode;
uniform mat4 uProjectionMatrix;

#define uColor (uColorOpacity.rgb)
#define uOpacity (uColorOpacity.a)

#define uIntersectionHighlightColor (uIntersectionHighlightColorOpacity.rgb)
#define uIntersectionHighlightOpacity (uIntersectionHighlightColorOpacity.a)
// Matches the CPU side info
const int DrawModeColor = 0;
const int DrawModeDistortion = 1;
const int OutlineModeNone = 0;
const int OutlineModeUV = 1;
const int OutlineModeNormal = 2;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;
in vec3 fragPositionVS;
in vec3 fragPositionWS;
in vec3 fragNormal;
#define fragDepthVS fragPositionVS.z
#define fragDepthWS fragPositionWS.z

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if USE_OIT
layout (location = 0) out vec4 outColor0;
layout (location = 1) out vec4 outColor1;
#else
layout (location = 0) out vec4 outColor;
#endif

//-------------------------------------------------------------------------------------------------------------------------

mat3 buildTBN(vec3 N, vec3 p, vec2 uv)
{
    // get edge vectors of the pixel triangle
    vec3 dp1 = dFdx( p );
    vec3 dp2 = dFdy( p );
    vec2 duv1 = dFdx( uv );
    vec2 duv2 = dFdy( uv );

    // solve the linear system
    vec3 dp2perp = cross( dp2, N );
    vec3 dp1perp = cross( N, dp1 );
    vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // construct a scale-invariant frame
    float invmax = inversesqrt( max( dot(T,T), dot(B,B) ) );
    return mat3( T * invmax, B * invmax, N );
}

// usage : vec3 normal = normalMap(fragNormal, normalize(fragPositionVS), fragTexCoords);
vec3 normalMap(vec3 N, vec3 V, vec2 texcoord)
{
    vec3 normal = texture(uTexture, texcoord + vec2(0,-Time * 0.1)).rgb;

    mat3 TBN = buildTBN( N, -V, texcoord );
    return normalize( TBN * normal );
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec4 color;

    vec4 texel = texture(uTexture, fragTexCoords + uUVAnimationSpeed * Time );

    // Draw Color
    if(DrawModeColor == uDrawMode)
    {
        float thresholdScale = 5.0;
        float fadeScale =  (1.1 - uIntersectionHighlightThickness) * thresholdScale;
        float meshOpacity = uOpacity;
        vec3 meshColor = uColor;
        vec3 highlightColor = uIntersectionHighlightColor;
        float highlightOpacity = clamp(uIntersectionHighlightOpacity, meshOpacity, 1.0);

        vec2 screenUV = gl_FragCoord.xy * uCurrentInvViewportSize;
        float depthVS = -texture(uDepthTexture, screenUV).r * FarClip;

        color.rgb = meshColor * texel.rgb;
        color.a = meshOpacity;

        if(uIntersectionHighlightThickness != 0)
        {
            // do not apply forcefield to the first person view 
            if(-depthVS < 0.5) discard;

    #if DISCARD_FACE_HIGHLIGHT
            // if no normals in vertex data, we generate them.
            vec3 toolNormalWS = fragNormal == vec3(0) ? fastNormalFromPosition(fragPositionWS) : fragNormal;

            // world space normal
            vec3 pos = PositionFromLinearDepth(screenUV, depthVS, ProjInfos);
            pos = (InvViewMatrix * vec4(pos, 1.0)).xyz;
            vec3 sceneNormalWS = fastNormalFromPosition(pos);

            float toolNdotSceneN = 1.0 - abs(dot(normalize(toolNormalWS), normalize(sceneNormalWS)));
            toolNdotSceneN = toolNdotSceneN < 0.3 ? 0 : 1.0;

            float zDist = fragDepthVS - depthVS;
            zDist = gl_FrontFacing && zDist < -0.1 ? zDist : abs(zDist);

            toolNdotSceneN = zDist < 0 && gl_FrontFacing ? 1.0 : toolNdotSceneN;

            float zFade = clamp((1.0 - clamp(zDist * fadeScale, 0.0, 1.0)) * toolNdotSceneN, 0.0, 1.0);
    #else
            float zDist = fragDepthVS - depthVS;
            float zFade = clamp((1.0 - clamp(zDist * fadeScale, 0.0, 1.0)), 0.0, 1.0);
    #endif // DISCARD_FACE_HIGHLIGHT

    #if USE_UNDERGROUND_COLOR
            //Color underground pixels with red but only on front faces.
            vec3 undergroundColor = vec3(0.5);
            highlightColor = zDist < 0 && gl_FrontFacing ? undergroundColor : highlightColor;
    #endif //USE_UNDERGROUND_COLOR

            // Shape outline based on normal - ok for sphere
            float shapeOutlineNormal = 1.0-clamp(abs(dot(normalize(fragNormal), vec3(0,0,1))), 0.0, 1.0);
            // Shape outline based on UV - ok for quads & cubes
            float shapeOutlineUV = smoothstep(0.4, 0.5, max(abs(fragTexCoords.x - 0.5), abs(fragTexCoords.y - 0.5)));

            float shapeOutline = OutlineModeNormal == uOutlineMode ? shapeOutlineNormal : OutlineModeUV == uOutlineMode ? shapeOutlineUV : 0.0;
            color.rgb = mix(color.rgb, highlightColor, zFade);
            color.a = mix(meshOpacity, highlightOpacity, zFade);

            color.rgb = mix(color.rgb, highlightColor, shapeOutline);
            color.a = max(color.a, shapeOutline * highlightOpacity);
        }

        if (color.a == 0.0) discard;

        int blendMode = uBlendMode;

#if USE_OIT
        // Hacky, for POIT only.
        float sceneLinearDepth = depthVS;
        processOIT(color, fragDepthVS, sceneLinearDepth, screenUV, blendMode, outColor0, outColor1);
#else
        // Blending w/ Premultiplied Alpha :
        // - to get linear blend we can use linear blend w/ premultiplied alpha, i.e. glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA) and RGB = RGB * A
        // - to get additive blend we can use the same setup and simply force the alpha to 0 (cf proof below)
        switch (blendMode)
        {
        case BLEND_MODE_PREMULT_LINEAR:
            color.rgb *= color.a; 
            break;
        case BLEND_MODE_ADD: 
            color.rgb *= color.a;
            color.a = 0; 
            break;
        case BLEND_MODE_LINEAR:
            break;
        }

        outColor = color;
#endif
    }
    else
    {
        color.rg = computeDistortion(uCurrentInvViewportSize, fragPositionVS, texel, 1.0);
        color.ba = vec2(0,1);

        if (color.a == 0.0) discard;

#if USE_OIT
        // For now, we simply ignore outColor1 when drawing distortion when using OIT.
        outColor0 = color;
#else
        outColor = color;
#endif
    }
}
