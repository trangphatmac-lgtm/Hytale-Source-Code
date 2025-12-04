#ifndef SHADING_INCLUDE
#define SHADING_INCLUDE

float computeCaustics(sampler2D causticsTexture, float intensity, float scale, float distortion, vec2 projectionUV, vec2 projectionUVOffset, vec3 positionWS, vec3 positionFromCameraWS, float dist, float falloff)
{
    vec2 basePos = positionWS.xz * scale;
    vec2 animatedOffset = (positionWS.yy * vec2(0.02, -0.01)) - projectionUVOffset.xy * 0.25;
    float a = texture(causticsTexture, basePos + animatedOffset).r;
    float b = texture(causticsTexture, basePos - animatedOffset).r;
    float projected = min(a,b) * 1.5;

    projected = mix(0.0, intensity, projected);
    projected *= mix(1.0, 0.0, clamp(dist * falloff, 0, 1));

    return projected;
}

float computeCausticsWithFlowmap(sampler2D flowTexture, sampler2D causticsTexture, float intensity, float scale, float distortion, vec2 projectionUV, vec2 projectionUVOffset, vec3 positionWS, vec3 positionFromCameraWS, float dist, float falloff)
{
    // Get the 2d flow motion vector in [0,1] and remap it to [-1,1]
    vec2 flow = texture(flowTexture, projectionUV + projectionUVOffset.xy).rg;
    flow = vec2(2.0) * flow - vec2(1.0);

    // Distorts the UV 
    // NB : use * (positionWS.yy * vec2(0.01, -0.01)) to avoid a complete top down projection, since it can produce ugly vertical lines
    vec2 distortionStrength = vec2(distortion);
    vec2 uv = (flow * distortionStrength + projectionUV) + (positionWS.yy * vec2(0.01, -0.01));
    float projected = texture(causticsTexture, uv).r;

    // DEBUG flow distorted UV
    //outColor.rgb = vec3(mod(uv, vec2(1)), 0); return;

    projected = mix(0.0, intensity, projected);
    projected *= mix(1.0, 0.0, clamp(dist * falloff, 0, 1));

    return projected;
}

float computeCloudsShadows(sampler2D cloudsTexture, vec2 projectionUV, vec2 projectionUVOffset, float intensity, float mipLevel)
{
    float projected = textureLod(cloudsTexture, projectionUV + projectionUVOffset, mipLevel).a;
    projected = mix(0.0, intensity, projected);

    return projected;
}

vec3 computeSkyAmbient(vec3 normal, vec3 sunDirection, vec3 skyAmbientBackColor, vec3 skyAmbientFrontColor, float skyAmbientIntensity, float sunOcclusion)
{
    // Use a blend of front & back color for fog, according to proximity to the sun
    // & remap from [-1 1]  to  [0 1]        
    float skyBlendCoef = dot(normal, sunDirection) * 0.5 + 0.5;

    vec3 ambientColor = mix(skyAmbientBackColor, skyAmbientFrontColor, skyBlendCoef);

    return ambientColor * skyAmbientIntensity * sunOcclusion;
}

#endif //SHADING_INCLUDE
