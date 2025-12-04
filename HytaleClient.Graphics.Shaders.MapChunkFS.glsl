#version 330 core

// Global scene data
#include "SceneData_inc.glsl"

#include "Reconstruction_inc.glsl"

#if USE_SCENE_DATA_OVERRIDE
#undef Time
#define Time 0
#undef SunLightColor
#define SunLightColor vec4(1)
#undef InvViewportSize
#define InvViewportSize vec2(1)
#endif //USE_SCENE_DATA_OVERRIDE

// Also required for forward rendering !
#include "Deferred_inc.glsl"

uniform sampler2D uTexture;

#if !DEFERRED
#include "Fog_inc.glsl"
#include "Shading_inc.glsl"

#ifndef USE_SKY_AMBIENT
#define USE_SKY_AMBIENT 1
#endif

#ifndef USE_UNDERWATER_CAUSTICS
#define USE_UNDERWATER_CAUSTICS 1
#endif

#ifndef USE_CLOUDS_SHADOWS
#define USE_CLOUDS_SHADOWS 0
#endif

#if USE_CLUSTERED_LIGHTING
#include "LightCluster_inc.glsl"
#endif // USE_CLUSTERED_LIGHTING

#ifndef USE_FORWARD_SUN_SHADOWS
#define USE_FORWARD_SUN_SHADOWS 0
#endif

#if USE_FORWARD_SUN_SHADOWS
#include "ShadowMap_inc.glsl"
#endif

#endif // !DEFERRED

#if ALPHA_BLEND

uniform vec2 uInvTextureAtlasSize;

// Can be differ from scene data during multi res passes.
uniform vec2 uCurrentInvViewportSize;

// In order to let people use & see the tests on WATER, I am using some temp uniform for WaterQuality.
// It will be removed later, once everything is settled !
uniform int uWaterQuality = 1;

uniform sampler2D uNormalsTexture;
uniform sampler2D uDepthTexture;
uniform sampler2D uLowResDepthTexture;
uniform sampler2D uSceneTexture;
uniform sampler2D uRefractionTexture;
uniform sampler2D uCausticsTexture;
uniform sampler2D uCloudShadowTexture;

#ifndef USE_OIT
#define USE_OIT 0
#endif

#if USE_OIT
uniform ivec2 uOITParams;

// Moments OIT related
uniform sampler2D uMomentsTexture;
uniform sampler2D uTotalOpticalDepthTexture;

const int BLEND_MODE_LINEAR = 0;
const int BLEND_MODE_ADD = 1;
const int BLEND_MODE_EROSION = 2;
#include "OIT_inc.glsl"
#endif // USE_OIT
uniform int uDebugOverdraw;

#endif //ALPHA_BLEND

#if ALPHA_BLEND || (NEAR)
float ViewportRatio = (InvViewportSize.x / InvViewportSize.y);
#endif

// Packed data layout : {effectID [6], shadingMode[2], renderConfig[8], padding[16]}
flat in int fragPackedData;
flat in vec3 fragNormalWS;

in vec4 fragStaticLight;
in vec3 fragTintColor;
in vec2 fragTexCoords;

#if ANIMATED || (!ALPHA_BLEND && !ALPHA_TEST) // OPAQUE
in vec2 fragMaskTexCoords;
#endif // OPAQUE

#if !DEFERRED
in vec3 fragPositionWS;
in vec3 fragPositionVS;
in vec4 fragFog;
#if ALPHA_BLEND
in float fragTexOffsetY;
in vec2 fragLargeTexCoords;

vec4 RayCast(vec3 dir, vec3 hitCoord, out vec2 screenCoords);
#endif // ALPHA_BLEND
#endif // !DEFERRED


#if DEFERRED
layout (location = 0) out vec4 outColor0;
layout (location = 1) out vec4 outColor1;
#else //DEFERRED
#if ALPHA_BLEND && USE_OIT
layout (location = 0) out vec4 outColor0;
layout (location = 1) out vec4 outColor1;
#else
layout (location = 0) out vec4 outColor0;
#endif
#endif //DEFERRED

const float FarClipPlane = 1024.0f;
const vec2 NoSideMaskUV = vec2(0.0);

#if (NEAR)
vec4 computeFakeShininess(vec3 baseColor, vec3 normal, vec3 cameraDiection);
#endif

// Unpack data from the VertexShader
void unpackData(int packedData, out int effectID, out int shadingMode, out int packedRenderConfig)
{
    const int maskEffectID = ((1 << 6) - 1);
    const int maskShadingMode = ((1 << 2) - 1);
    const int maskRenderConfig = ((1 << 8) - 1);
    effectID = packedData & maskEffectID;
    shadingMode = (packedData >> 6) & maskShadingMode;
    packedRenderConfig = (packedData >> 8) & maskRenderConfig;
}

bool isWater(int effectID)
{
    return (effectID == EFFECT_WATER || effectID == EFFECT_WATERENVIRONMENTCOLOR || effectID == EFFECT_WATERENVIRONMENTTRANSITION);
}

void main()
{
    vec3 fragTintColorFixed = fragTintColor;
    vec4 texColor;

#if USE_CLUSTERED_LIGHTING
    // NB: fragPositionVS.z is available because USE_CLUSTERED_LIGHTING is true only for !DEFERRED
    int lightIndex; 
    int pointLightCount;
    fetchClusterData(gl_FragCoord.xy * uCurrentInvViewportSize, -fragPositionVS.z, lightIndex, pointLightCount);
#endif

    int effectID, shadingMode, renderConfig;
    unpackData(fragPackedData, effectID, shadingMode, renderConfig);

#if ALPHA_BLEND
    vec2 scrollingTextureSize = 32.0 * uInvTextureAtlasSize;

    // Recover texture origin from the original texture coordinate
    float textureOriginY = fragTexOffsetY == 0.0 ? fragTexCoords.y : floor(fragTexCoords.y / scrollingTextureSize.y) * scrollingTextureSize.y;
    float offsetY = fragTexOffsetY == 0.0 ? 0.0 : mod(fragTexOffsetY, scrollingTextureSize.y);

    // Use a bias on water top faces to remove their texture by using the lowest mip.
    int bias = fragNormalWS.y > 0.8 && isWater(effectID) && uWaterQuality >= 1? 10 : 0;
    texColor = texture(uTexture, vec2(fragTexCoords.x, textureOriginY + offsetY), bias);

#elif ANIMATED || !ALPHA_TEST
    texColor = texture(uTexture, fragTexCoords);
    vec4 maskTexColor = texture(uTexture, fragMaskTexCoords, -2);

    texColor =  (fragMaskTexCoords == NoSideMaskUV) || maskTexColor.a == 0 ? texColor : maskTexColor;
    fragTintColorFixed = (fragMaskTexCoords != NoSideMaskUV) && maskTexColor.a == 0 ? vec3(1.0) : fragTintColor;

#else
    texColor = texture(uTexture, fragTexCoords);

#endif

#if ALPHA_TEST
#if NEAR
    // Discard using for fragment too close to the camera or discard if texel is fully transparent
    if (gl_FragCoord.z < 0.55 || (texColor.a == 0.0)) discard;
#else
    // Discard if texel is fully transparent
    if (texColor.a == 0.0) discard;
#endif
#endif

    vec3 normalWS = gl_FrontFacing ? fragNormalWS : -fragNormalWS;
    vec3 light = fragStaticLight.rgb;
    vec3 albedo = texColor.rgb * fragTintColorFixed;

    // Since we need to clamp for DEFERRED, we also clamp for FORWARD, in order to have consistent results.
    albedo = clamp(albedo, vec3(0), vec3(1));
    
#if NEAR
    if (shadingMode == SHADING_REFLECTIVE)
    {
        vec4 shininess = computeFakeShininess(texColor.rgb, normalWS, CameraDirection.xyz);
        light = max(light, vec3(shininess.a));
        albedo.rgb += shininess.rgb;
    }
#endif // NEAR

#if DEFERRED
    // Use whatever RGB compression was selected inside this function -- PS: albedo must be clamped to 1 before going through encoding.
    outColor0.rg = encodeAlbedoCompressed(albedo, ivec2(gl_FragCoord.xy));

    // Encode the view space normal in the .BA channels
    outColor0.ba = encodeNormal(normalWS);

    outColor1.rgb = light;
    outColor1.a = float(renderConfig)/ 255.0f;

#if USE_LBUFFER_COMPRESSION
    // Write surface similarity
    //float similarity = 25000.0 * fwidth(gl_FragCoord.z);
    outColor1.rgba = vec4(encodeLightCompressed(min(vec3(1), outColor1.rgb), ivec2(gl_FragCoord.xy)), fragStaticLight.a, outColor1.a);
#endif

#else //  DEFERRED

    // Dynamic lighting
    vec3 dynamicLight = vec3(0.0);
#if USE_CLUSTERED_LIGHTING
    const float EarlyOutThreshold = 1.0;
    dynamicLight = computeClusteredLighting(fragPositionVS.xyz, pointLightCount, lightIndex, EarlyOutThreshold); 
    const float DynamicLightMultiplier = 3.0f;
    dynamicLight *= DynamicLightMultiplier;
#endif //USE_CLUSTERED_LIGHTING

    light = clamp(max(dynamicLight, light), vec3(0), vec3(1));

#if USE_SKY_AMBIENT
    vec3 ambientColor = computeSkyAmbient(normalWS, SunPositionWS, AmbientBackColor, AmbientFrontColor, AmbientIntensity, fragStaticLight.a);
    light += ambientColor;
#endif // USE_SKY_AMBIENT

    bool hasBloom = shadingMode == SHADING_FULLBRIGHT;

#if USE_CLOUDS_SHADOWS && 0
    {
    // We can either use the same uv offset as the CloudsFS.glsl to have some kind of sync, or use our own

    // Moves the UV so that the speed & direction of the shadows match the clouds in the sky    
    vec3 positionFromCameraWS = fragPositionWS;
    vec3 positionWS = positionFromCameraWS + CameraPosition;
    float projected = computeCloudsShadows(uCloudShadowTexture, positionWS.xz * CloudsShadowScale, vec2(CloudsShadowAnimTime), CloudsShadowIntensity, CloudsShadowBlurriness);
    projected = hasBloom ? 0 : projected * fragStaticLight.a;

    // Removes shadows darkness
    // line below is <=> but faster (1 MADD) to light.rgb = light.rgb * (1.0 - projected);
    light.rgb = -light.rgb * projected + light.rgb;
    }
#endif

    // Shadow mapping
    float shadow = 1.0;
#if USE_FORWARD_SUN_SHADOWS
    {
    // TEST: this is just to validate the Forward CSM feature
    // NB: fragPositionVS.z is available because USE_CLUSTERED_LIGHTING is true only for !DEFERRED
    bool useCleanShadowBackfaces = effectID > EFFECT_WIND;
    float linearDepth = fragPositionVS.z;
    vec3 positionFromCameraWS = fragPositionWS;
    shadow = computeShadowIntensity(positionFromCameraWS, linearDepth, normalWS, useCleanShadowBackfaces);
    //outColor0.rgb = outColor0.rgb * pow(shadow, 0.5);    

    // Avoid shadow on emissive material.
    shadow = hasBloom ? 1.0 : shadow;
    }
#endif

    // NB: compared to the deferred render path, the missing bits are
    // - the clouds shadows (it would be another texture fetch)
    // - the ssao (it's ~not possible)
    outColor0.rgb = albedo * light * shadow;
    outColor0.a = texColor.a;

//    // Debug
//    outColor0.rgb = light;
//    outColor0.rgb = albedo; 

    // Fog color
    // Use a blend of front & back color for fog, according to proximity to the sun	
    vec3 cameraToFragment = normalize(fragPositionWS);

    vec3 smoothFogColor = getFogColor(FogTopColor.rgb, FogFrontColor.rgb, FogBackColor.rgb, cameraToFragment, SunPositionWS);

#if ALPHA_BLEND
    // Only for Water
    if (isWater(effectID) && uWaterQuality >= 1)
    {
        vec2 screenUV = gl_FragCoord.xy * uCurrentInvViewportSize;
        float depth = -texture(uDepthTexture, screenUV).r;

        // -- Normal computation --
        // Is this a top face ? just make sure fragNormal.y is the highest component
        float isTopFaceValue = normalWS.y > 0.8 ? 1.0 : 0.0;
        float isHorizontalFaceValue = abs(normalWS.y) > 0.8 ? 1.0 : 0.0;

        // NormalMap is sampled differently on horizontal & vertical faces:
        // - for horizontal faces, we use a down scaled version of the large tex coords (32x32 => 8x8)
        // - for bertical faces, we use a stretched version of the tex coords
        vec2 normalsTexCoords = (isHorizontalFaceValue > 0.0 ? fragLargeTexCoords * 4.0 : fragTexCoords * vec2(64.0, 4.0));

        // -- ...& UV animations --
        // Vertical animation for side faces only
        normalsTexCoords.y -= (1-isHorizontalFaceValue) * Time * 0.5;

        vec3 texelNormal = texture(uNormalsTexture, normalsTexCoords + Time * 0.055).rbg;
        vec3 texelNormal2 = texture(uNormalsTexture, normalsTexCoords - Time * 0.0565).rbg;
        vec3 texelNormalLarge = texture(uNormalsTexture, fragLargeTexCoords - Time * 0.01).rbg;
        vec3 texelNormalLarge2 = texture(uNormalsTexture, fragLargeTexCoords + Time * 0.015).rbg;

        //vec3 baseWaterColor = vec3(0.3, 0.6, 0.8);
        vec3 baseWaterColor = outColor0.rgb;

        // Initialize all water color components to the base color,
        // so that we can properly disable sub parts of the code without breaking everything.
        // (compiler will aggressively optimize this anyway)
        vec3 finalUnderWaterColor = baseWaterColor.rgb;
        vec3 finalSurfaceWaterColor = baseWaterColor.rgb;

        // Remember we render the scene with the camera forced to (0,0,0)
        const vec3 vCameraPositionWS = vec3(0.0);
        const vec3 vUpNormal = vec3(0.0, 1.0, 0.0);
        vec3 vFragToCamera = normalize(vCameraPositionWS - fragPositionWS);
        float cosAngle = dot(vFragToCamera, vUpNormal);
        float cosAngleReal = dot(vFragToCamera, normalWS);

        // Useful vars - use float (0.0f or 1.0f) as bool, which allows to avoid some 'if' by using maths
        float isCameraUnderValue = cosAngle > 0 ? 1.0 : 0.0;

        // Constants values used in the computation of the Fresnel term
        const float fresnelN1 = 1.0;
        const float fresnelN2 = 1.5;
        const float fresnelR0 = pow(fresnelN1 - fresnelN2, 2.0) / pow(fresnelN1 + fresnelN2, 2.0);

        float fresnel = fresnelR0 + (1.0 - fresnelR0) * pow(1.0 - cosAngle, 5.0);
        float fresnelReal = fresnelR0 + (1.0 - fresnelR0) * pow(1.0 - cosAngleReal, 5.0);

        // NB : here the "... - vec3(1.0)" is the result of a math simplification,
        // after the xyz value was remapped from [0, 1] (cause it was stored in a texture)
        // to [-1, 1] (cause it's a normal !)
        vec3 smallWavesNormal = normalize((texelNormal + texelNormal2) - vec3(1.0));
        vec3 largeWavesNormal = normalize(texelNormalLarge + texelNormalLarge2 - vec3(1));
        vec3 bentNormal = normalize(smallWavesNormal * 3.0 + largeWavesNormal + vUpNormal * 10.5 * isHorizontalFaceValue);
        //bentNormal = mix(smallWavesNormal, largeWavesNormal, (fragPositionVS.z/20.0) * isTopFaceValue);

        bentNormal *= sign(normalWS.y);

        float cosBentAngle = dot(vFragToCamera, bentNormal);
        float bentFresnel = fresnelR0 + (1.0 - fresnelR0) * pow(1.0 - cosBentAngle, 5.0);

        float deltaZ = fragPositionVS.z - depth * FarClipPlane;

        // -- Refraction + Underwater light scattering --
        {
            // Strength of the texel offset must be proportional to the Z to keep refraction offset consistent in world space
            const float MaxRefraction = 75.0;
            float refractionStrength = MaxRefraction / -fragPositionVS.z * 3.0;

            // Keep the refraction strength under control.
            refractionStrength = clamp(refractionStrength, 0.0, MaxRefraction);

    //        vec2 refractionStrength = vec2(isTopFaceValue > 0 ? vec2(125.0) : vec2(0, 50)) / sqrt(-fragPositionVS.z);

            // Fade out the strength as we are close to geometry underwater
            refractionStrength *= clamp(abs(deltaZ), 0, 1);

            vec2 refractionOffset = smallWavesNormal.xz * InvViewportSize;
            vec2 refractedUV = screenUV + refractionOffset * refractionStrength * 0.5;

            // Do not use the refractedZ if it sampled a pixel that is closer to the camera
            float refractedDepth = -texture(uDepthTexture, refractedUV).r;
            float deltaZRefracted = fragPositionVS.z - refractedDepth * FarClipPlane;
            deltaZ = deltaZRefracted > 0.0 ? deltaZRefracted : deltaZ;
            refractedUV = deltaZRefracted > 0.0 ? refractedUV : screenUV;

            // Color from the refraction map
            float refractionLod = clamp(deltaZ * 0.05, 0, 1) * 2.0;
//            refractionLod = 0;

            vec3 baseRefractedColor = textureLod(uRefractionTexture, refractedUV, refractionLod).rgb;
            
#if USE_UNDERWATER_CAUSTICS
            float dist = -fragPositionVS.z;
//            float dist = length(fragPositionVS);
//            float dist = deltaZRefracted;
            const float MaxCausticsDistance = 64.0;
            const float CausticsFalloff = 1.0 / MaxCausticsDistance;
            if (uWaterQuality >= 2 && IsCameraUnderwater <= 0 && dist < MaxCausticsDistance)
            {
                // Add underwater caustics seen when camera is outside the water volume           
                vec3 positionFromCameraWS = PositionFromLinearDepth(refractedUV, refractedDepth * FarClipPlane, ProjInfos);
                positionFromCameraWS = mat3(InvViewMatrix) * positionFromCameraWS;
                vec3 positionWS = positionFromCameraWS + CameraPosition;

                vec2 projectionUV = positionWS.xz * WaterCausticsScale;
                // We can either use the same uv offset as the CloudsFS.glsl to have some kind of sync, or use our own
                vec2 projectionUVOffset = vec2(WaterCausticsAnimTime);
                float sunOcclusion = fragStaticLight.a;

                float caustics = computeCaustics(uCausticsTexture, WaterCausticsIntensity, WaterCausticsScale, WaterCausticsDistortion, projectionUV, projectionUVOffset, positionWS.xyz, positionFromCameraWS, dist, CausticsFalloff) * sunOcclusion;
                baseRefractedColor += baseRefractedColor * caustics * 1.35;
            }
#endif // USE_UNDERWATER_CAUSTICS

            // Underwater influence amount
            float underwaterDepthFactor = deltaZ * 0.075;
            underwaterDepthFactor = clamp(underwaterDepthFactor, 0.0, 1.0);

            // Change the color (to a darker one) according to underwater depth
            vec3 baseUnderwaterColor = baseWaterColor.rgb * mix(1.0, 0.75, underwaterDepthFactor);
            //vec3 baseUnderwaterColor = vec3(0.3, 0.6, 0.8) * mix(1.0, 0.75, underwaterDepthFactor);

            // Decrease the R component according to underwater depth, cause water tends to minimize it
            baseUnderwaterColor.r *= mix(0.1, 1.0, underwaterDepthFactor);

            // Change the opacity according to underwater depth
            float underwaterCoef = mix(0.7, 0.99, underwaterDepthFactor);
            underwaterCoef -= 0.05 * IsCameraUnderwater;
            finalUnderWaterColor.rgb = mix(baseRefractedColor, baseUnderwaterColor, underwaterCoef);

#if USE_OIT
            outColor0.a = underwaterCoef;
#endif // USE_OIT
        }

        // Various reflection influences - allow this only on top faces seen from above
        if (isHorizontalFaceValue > 0.0)
        {
            // -- Fresnel reflection w/ "Sky Color" --
            // Actually it's  a mix of FogColor & CloudsColor based on the orientation of the camera vs the sun
            if (isCameraUnderValue > 0.0)
            {
                // Proximity to the sun
                // & remap from [-1 1]  to  [0 1]
                float sunProximityFactor = dot(cameraToFragment, SunPositionWS.xyz) * 0.5 + 0.5;
                float sunBlendCoef = pow(sunProximityFactor, 300.0);
//                float sunBlendCoef = isCameraUnderValue > 0 ? pow(sunProximityFactor, 300.0) : 0;

                vec3 customSkyBlendColor = mix(smoothFogColor, SunColor.rgb, sunBlendCoef);

                // Use the information from the static light to cancel the sky color in caves, etc.
//                float skyReflectionCoef = bentFresnel * fragStaticLight.a;
                float skyReflectionCoef = clamp(fresnelReal * fragStaticLight.a, 0, 1);

                finalSurfaceWaterColor.rgb = mix(baseWaterColor.rgb, customSkyBlendColor.rgb, skyReflectionCoef);
            }

            float specular = 0;

            // Sun specular
            {
                float shininess = 2000;
                float specularStrength = 25;
                vec3 lightDir = -SunPositionWS.xyz;
                //vec3 lightDir = -normalize(vec3(-1,0.15,0));//SunPositionWS.xyz;
                vec3 reflectDir = reflect(lightDir, normalize(smallWavesNormal));
                specular = pow(max(0.0, dot(reflectDir, vFragToCamera)), shininess) * specularStrength;

                // blinn phong specular as an alternative
                //vec3 halfAngleDir = normalize(-lightDir + vFragToCamera);
                //specular = pow(max(0.0, dot(halfAngleDir, bentNormal)), shininess/50) * specularStrength/3;

                // Use the information from the static light to cancel the sky color in caves, etc.
                specular *= fragStaticLight.a;

                hasBloom = hasBloom || specular > 1.0;
            }

            // -- SSR (Screen Space Reflection) --
            // This threshold on the Fresnel coef [0;1] is used to trigger the computation of the reflection.
            // The lower value the better quality... & the more expensive.
            float thresholdFresnelReflection = uWaterQuality == 3 ? 0.1 : 0.5;

            if (uWaterQuality >= 2
                && min(fresnel, bentFresnel)> thresholdFresnelReflection)
            {
                // Reflected ray to cast - in view space
                vec3 rayWS = normalize(reflect(-vFragToCamera, bentNormal));
                vec3 rayVS = mat3(ViewMatrix) * rayWS;

                // Process the raycast now
                vec3 hitPos = fragPositionVS.xyz;
                vec2 screenCoords;
                vec4 raycastResult = RayCast(rayVS, hitPos, screenCoords);

#define MAX_ROUGHNESS 2.5
                float reflectionLod = MAX_ROUGHNESS;
#ifdef USE_VAR_ROUGHNESS
                float distanceHit = rayVS.z - raycastResult.z;
                reflectionLod = clamp(abs(distanceHit) * 0.0025, 0, 1) * MAX_ROUGHNESS;
#endif
                // If the reflection is not a clean success, we take a very low mip sample
                // in order to hide the details of that wrong reflection.
                reflectionLod = raycastResult.w == 1 ? reflectionLod : 6;

                // Use temporal reprojection to match the pixel position in the previous frame...
                // but to hide ghosting artefacts, we do a mix w/ the current frame info (which has no ghosting) based on the motion:
                // - blend the reprojection UV between prev frame coords & current frame coords
                // - blend the prev frame reprojected color w/ the current frame color
                vec4 prevFrameCoords = ReprojectionMatrix * vec4(raycastResult.xyz, 1);
                prevFrameCoords.xy /= prevFrameCoords.w;
                prevFrameCoords.xy = prevFrameCoords.xy * 0.5 + 0.5;

                vec4 currFrameCoords = ProjectionMatrix * vec4(raycastResult.xyz, 1);
                currFrameCoords.xy /= currFrameCoords.w;
                currFrameCoords.xy = currFrameCoords.xy * 0.5 + 0.5;

                float d = clamp(15 * distance(prevFrameCoords.xy, currFrameCoords.xy), 0, 1);
                d = d * d;

                prevFrameCoords = mix(prevFrameCoords, currFrameCoords, d);

                vec3 reflectedColor;
                vec3 reflectedColorA = textureLod(uSceneTexture, prevFrameCoords.xy, reflectionLod + 1).rgb;
                vec3 reflectedColorB = textureLod(uRefractionTexture, currFrameCoords.xy, reflectionLod).rgb;
                
                float c = raycastResult.w == 1 ? 1 : 0;
                c *= sqrt(sqrt(d));
                reflectedColor = mix(reflectedColorA, reflectedColorB, c);

                // Max reflectiveness of the water - range [0.0, 1.0]
//                float maxReflectiveness = clamp(0.6 + 0.01 * -fragPositionVS.z, 0.0, 1.0);
                float maxReflectiveness = 0.8;

                float reflectionCoef = 1;
                reflectionCoef = smoothstep(thresholdFresnelReflection, 1.0, fresnel * 1.25);
                reflectionCoef *= maxReflectiveness;

                // Hide screen edges artifacts
                vec2 dCoords = abs(vec2(0.5, 0.5) - screenCoords);
                float discarFactor = clamp((dCoords.x + dCoords.y), 0.0, 1.0);
                reflectionCoef *= 1.0 - (discarFactor * discarFactor);

                // Discard if closer than the original z, to avoid picking a fragment in the foreground
                // here we use the fact that the w component stores the success of the raycast as 0 or 1.
                discarFactor = raycastResult.w > 0.5 ? 1.0 : 0.0;
                reflectionCoef *= discarFactor;

                finalSurfaceWaterColor.rgb = mix(finalSurfaceWaterColor.rgb, reflectedColor.rgb, reflectionCoef);

                // If the surface does not reflects the sky (= blocker found during RayCast), cancel the specular.
                specular = specular * (raycastResult.w == 1.0 ? 0.0 : 1.0);

                // Bilateral box-filter over a quad for free
                //ivec2 ssC = ivec2(gl_FragCoord.xy);
                //finalSurfaceWaterColor.rgb -= dFdx(finalSurfaceWaterColor.rgb) * ((ssC.x & 1) - 0.5);
                //finalSurfaceWaterColor.rgb -= dFdy(finalSurfaceWaterColor.rgb) * ((ssC.y & 1) - 0.5);
            }

            // Add sun specular
            finalSurfaceWaterColor += SunColor.rgb * specular;
        }

        // -- Screen space shore lines rendering (intersection highlights) --
        vec3 shoreLineColor;
        float shoreLineFactor = 0.0;

        {
            const float MaxShoreLineDistance = 128.0;

            const vec3 BrightnessCoefficients = vec3(0.299, 0.587, 0.114);
            float fragmentBrightness = dot(BrightnessCoefficients,finalUnderWaterColor);

            // For foam color, use a color that goes towards white, according to the current fragment brightness
            shoreLineColor = mix(finalUnderWaterColor.rgb, vec3(1.0), fragmentBrightness);

            const float invWidthFactor = 5.0;
            const float maxDist = 0.5;
            const float minDist = -0.025;
            float zDist = deltaZ;
            float zFade = clamp(zDist * invWidthFactor, 0.0, 1.0);

            // equivalent to : if (zDist > minDist && zDist < maxDist && -fragPositionVS.z < MaxShoreLineDistance) shoreLineFactor = 1.0 - zFade;
            shoreLineFactor = (zDist > minDist && zDist < maxDist && -fragPositionVS.z < MaxShoreLineDistance) ? 1.0 - zFade : 0.0;
        }

        // Use a the fresnel term to blend only if this is a TOP face seen from above
//        float blendFactor = bentFresnel * fresnel * isTopFaceValue * isCameraUnderValue;
//        float blendFactor = fresnelReal * isHorizontalFaceValue;
        float blendFactor = bentFresnel * fresnelReal * isHorizontalFaceValue;
//        float blendFactor = bentFresnel * fresnel * isHorizontalFaceValue;

        // Force a saturate to account for when fresnelReal is NaN - can occur under certain camera angles.
        blendFactor = clamp(blendFactor, 0, 1);

        outColor0.rgb = mix(finalUnderWaterColor.rgb, finalSurfaceWaterColor.rgb, blendFactor);
        outColor0.rgb = mix(outColor0.rgb, shoreLineColor.rgb, shoreLineFactor);
        
#if USE_OIT
        outColor0.a = clamp(outColor0.a, 0, 1);
#endif // USE_OIT

        //outColor0.rgb = vec3(bentFresnel);
    }

    // Warning : because of precision issue "with the UVs",
    // when using the combo {Texture Atlas + Point Sampling + UV animation}
    // we cannot discard "just any" fragment, even if it's full transparent.
    // So now, we disable discarding for the water effect.
    if (!(effectID == EFFECT_WATER || effectID == EFFECT_WATERENVIRONMENTCOLOR || effectID == EFFECT_WATERENVIRONMENTTRANSITION) && texColor.a == 0.0 ) discard;
        
#endif // ALPHA_BLEND

#if USE_FOG
    // Apply fog
    outColor0.rgb = mix(outColor0.rgb, fragFog.rgb, fragFog.w);
    hasBloom = hasBloom && fragFog.w < 0.9;
#endif // USE_FOG

#if ALPHA_BLEND && USE_OIT
    vec2 screenUV = gl_FragCoord.xy * uCurrentInvViewportSize;
    float sceneLinearDepth = 0;

    if (uOITMethod == OIT_POIT)
    {
        sceneLinearDepth = -texture(uDepthTexture, screenUV).r * FarClipPlane;
    }

    vec4 color = outColor0;
#if USE_OIT
//    color.a = 0.4;
//    color.a = isWater(effectID) ? 0.8 : 0.4;
    color.a = isWater(effectID) ? color.a : 0.4;
#endif // USE_OIT
    processOIT(color, fragPositionVS.z, sceneLinearDepth, screenUV, BLEND_MODE_LINEAR, outColor0, outColor1);
#endif // ALPHA_BLEND && USE_OIT

#if WRITE_RENDERCONFIG_IN_ALPHA
    // Write "HasBloom" in the A channel
    outColor0.a = hasBloom ? packFragBits(false, true, false) : 0;
#endif // WRITE_RENDERCONFIG_IN_ALPHA

#endif // DEFERRED

    // Screendoor Transparency
    //if(length(fragPositionWS.xz) > FogParams.y - 5 ) if( 0 == mod(gl_FragCoord.x + gl_FragCoord.y,2.0))discard;
}


#if ALPHA_BLEND
// Limit the max number of steps & make big steps
const int maxSteps = 10;
const float rayStep = 50.0;

#define USE_SSR_RAYMARCH_VAR 1
#define USE_SSR_WITH_LINEAR_Z 1

vec4 RayCast(vec3 dir, vec3 hitCoord, out vec2 screenCoords)
{
    // This function will return more info in the vec4.w component:
    //  w = 0 : no collision
    //  w = 0.5 : false collision w/ foreground
    //  w = 1 : valid collision
    vec4 result = vec4(0.0);

#if USE_SSR_RAYMARCH_VAR
    float rStep = 5.0;
#else
    dir *= rayStep;
#endif

    float zInit = hitCoord.z;
    float depth;

    vec4 bestResult = vec4(0);

    for (int i = 0; i < maxSteps; i++)
    {
        hitCoord += dir;
        vec4 projectedCoord = ProjectionMatrix * vec4(hitCoord, 1.0);

        projectedCoord.xy /= projectedCoord.w;
        screenCoords = projectedCoord.xy * 0.5 + 0.5;

        // Use a low resolution depth texture for better performances
        depth = texture(uLowResDepthTexture, screenCoords.xy).r;

#if USE_SSR_WITH_LINEAR_Z
        depth *= -FarClipPlane;
#else
        depth = -GetLinearDepthFromDepthHW(depth, ProjectionMatrix);
#endif

        float deltaDepth = hitCoord.z - depth;

        // Make sure the result is between valid z bounds,
        // i.e. not on the far plane, not on the foreground
        if (deltaDepth < 0.0 && depth > -FarClipPlane)
        {
            float hint = depth < zInit ? 1.0 : 0.5;
            bestResult = vec4(hitCoord.xyz, hint);

            if (depth < zInit)
                break;
        }
#if USE_SSR_RAYMARCH_VAR
        rStep += rStep;
        dir *= rStep;
#endif
    }

    result = bestResult;

    return result;
}
#endif // ALPHA_BLEND

#if (NEAR)
float pow3(float value) { return value * value * value; }

vec4 computeFakeShininess(vec3 baseColor, vec3 normal, vec3 cameraDirection)
{
    // This function has been slighty micro-optimized using MADD, free functions, changing div for mul, etc
    float cosine = clamp(dot(normal, -cameraDirection), 0.0, 1.0);

    // Shine by going through quads randomly
    const float shineIdPeriod = 6.0;
    const float shineIdHalfPeriod = shineIdPeriod / 2.0;

    // opti : do this on the CPU
    float shineIdScan = mod(Time * 0.125, shineIdPeriod);

    float shineId = mod(float( gl_PrimitiveID >> 1), shineIdPeriod);
    float shineIdDiff = shineIdScan - shineId;

    // conditional assignment is fast on all GPUs
    float shineModificator = (shineIdDiff < -shineIdHalfPeriod) ? shineIdPeriod : 0.0;
    shineModificator = (shineIdDiff > shineIdHalfPeriod) ? -shineIdPeriod : shineModificator;
    shineIdDiff += shineModificator;

    // Swipe shining quads
    vec2 dxy = gl_FragCoord.xy * InvViewportSize.xy;
    dxy.y = dxy.y * -ViewportRatio + 1.0;

    float target = fract(Time * 0.25 + shineId * 0.166);

    float swipeDistance = clamp(abs(length(dxy) - 0.2 - target), 0.0, 1.0);
    swipeDistance = (swipeDistance > 0.5) ? -2.0 * swipeDistance + 2.0 : 2.0 * swipeDistance;

    float shineAmountRandom = pow3(clamp(abs(shineIdDiff) * 0.333, 0.0, 1.0));

    float shineFactor = (-cosine * shineAmountRandom + cosine) * (0.6 * pow3(swipeDistance));

    return vec4(baseColor.rgb * shineFactor, shineFactor);
}
#endif // (NEAR)

//#endif // ALPHA_BLEND
