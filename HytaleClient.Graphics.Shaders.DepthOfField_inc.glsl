#ifndef DEPTH_OF_FIELD_INCLUDE
#define DEPTH_OF_FIELD_INCLUDE

// Make sure you have :
// 1. enabled the extension GL_ARB_gpu_shader5
// 2. added the right define PIXELSIZE
// 3. included Fallback_inc.glsl

float getLinearDepthFromDepthNDC(float depthNDC, mat4 projectionMatrix)
{
    return projectionMatrix[3][2] / (depthNDC + projectionMatrix[2][2]);
}

vec3 DepthOfFieldNaive(sampler2D colorTexture, vec2 texCoords,
                        vec2 pixelSize, float linearDepth,
                        float nearBlurry, float nearSharp,
                        float farSharp, float farBlurry,
                        float nearBlurMax, float farBlurMax,
                        out bool isSharp
                        )
{
    isSharp = true;
    float coefDirNear = -nearBlurMax / (nearSharp - nearBlurry);
    // float coefDirFar  = farBlurMax / (farBlurry - farSharp);
    float coefDirFar  = (farBlurry == farSharp) ? 0.0f : farBlurMax / (farBlurry - farSharp);

    float CoC = 0;
    vec3 DoF = vec3(0);

    CoC = step(farSharp, linearDepth) * clamp(coefDirFar * (linearDepth - farSharp), 0, farBlurMax) +
            step(linearDepth, farSharp) * clamp(coefDirNear * (linearDepth - nearBlurry) + nearBlurMax, 0, nearBlurMax);

    int sampleCount = 0;
    if(CoC > 0)
    {
        isSharp = false;
        int kernelSize = 10;
        for(int y = -kernelSize ; y <= kernelSize ; y++)
        {
            for(int x = -kernelSize ; x <= kernelSize ; x++)
            {
                vec2 offset2 = vec2(CoC * pixelSize.x * x, CoC * pixelSize.y * y);
                DoF += texture(colorTexture, texCoords.xy + offset2).rgb;
                sampleCount++;
            }
        }
        DoF /= sampleCount;
    }
    return DoF;
}

vec3 DepthOfFieldFast(sampler2D colorTexture, sampler2D blurTexture, 
                    vec2 texCoords,float linearDepth,
                    float nearBlurry, float nearSharp,
                    float farSharp, float farBlurry,
                    out bool isSharp)
{
    isSharp = true;
    float coefDirNear = -1.0f / (nearSharp - nearBlurry);
    float coefDirFar  = (farBlurry == farSharp) ? 0.0f : 1.0f / (farBlurry - farSharp);

    float CoC = 0;
    float CoCFar = 0;
    vec3 DoF = vec3(0);

    CoC = step(linearDepth, farSharp) * clamp(coefDirNear * (linearDepth - nearBlurry) + 1.0f, 0, 1.0f) +
     step(farSharp, linearDepth) * clamp(coefDirFar * (linearDepth - farSharp), 0, 1.0f) ;

    vec3 coeff = vec3(0);
    if(CoC > 0)
    {
        isSharp = false;
        vec3 sharp   = texture(colorTexture,texCoords.xy).rgb;
        vec3 blurred = texture(blurTexture,texCoords.xy).rgb;
        coeff = mix(sharp, blurred, CoC );
    }
    DoF = coeff;

    return DoF;
}

vec3 DepthOfFieldFastBis(sampler2D colorTexture, sampler2D nearBlurTexture, sampler2D farBlurTexture,
                    vec2 texCoords,float linearDepth,
                    float nearBlurry, float nearSharp,
                    float farSharp, float farBlurry,
                    out bool isSharp)
{
    float coefDirNear = -1.0f / (nearSharp - nearBlurry);
    float coefDirFar  = (farBlurry == farSharp) ? 0.0f : 1.0f / (farBlurry - farSharp);

    float CoCNear = 0;
    float CoCFar = 0;
    vec3 DoF = vec3(0);

    CoCNear = step(linearDepth, farSharp) * clamp(coefDirNear * (linearDepth - nearBlurry) + 1.0f, 0, 1.0f);
    CoCFar = step(farSharp, linearDepth) * clamp(coefDirFar * (linearDepth - farSharp), 0, 1.0f) ;

    vec3 sharp   = texture(colorTexture, texCoords.xy).rgb;
    vec3 blurred;

    vec3 coeff = sharp;

    if(CoCNear > 0)
    {
        isSharp = false;
        blurred = texture(nearBlurTexture, texCoords.xy).rgb;
        coeff = mix(sharp, blurred, CoCNear);
    }
    else if(CoCFar > 0)
    {
        isSharp = false;
        blurred = texture(farBlurTexture,texCoords.xy).rgb;
        coeff = mix(sharp, blurred, CoCFar);

    }
    else
    {
        isSharp = true;
    }
    DoF = coeff;

    return  DoF;
}


vec3 DepthOfFieldAdvanced(sampler2D colorTexture, sampler2D cocTexture, sampler2D cocLowResTexture, sampler2D nearCoCBlurredLowResTexture,
                            sampler2D nearFieldLowResTexture, sampler2D farFieldLowResTexture, vec2 texCoords,vec2 pixelSize, out bool isSharp)
{
    isSharp = true;
    vec4 result = textureLod(colorTexture, texCoords, 0);

    vec2 halfPixelOffset = vec2(-pixelSize.x * 0.5f);
    vec2 farFieldBaseTexCoords = texCoords + halfPixelOffset;

    vec2 texCoord00 = farFieldBaseTexCoords;
    vec2 texCoord10 = farFieldBaseTexCoords + vec2(pixelSize.x, 0.0f);
    vec2 texCoord01 = farFieldBaseTexCoords + vec2(0.0f, pixelSize.y);
    vec2 texCoord11 = farFieldBaseTexCoords + vec2(pixelSize.x, pixelSize.y);

    float cocFar = textureLod(cocTexture, texCoords, 0).y;
    vec4 cocsFar_x4 = TextureGatherG(cocLowResTexture, texCoord00).wzxy;
    vec4 cocsFarDiffs = abs(vec4(cocFar) - cocsFar_x4);
    if(cocFar > 0)
        isSharp = false;

    // far field blending
    vec4 dofFar00 = textureLod(farFieldLowResTexture, texCoord00, 0);
    vec4 dofFar10 = textureLod(farFieldLowResTexture, texCoord10, 0);
    vec4 dofFar01 = textureLod(farFieldLowResTexture, texCoord01, 0);
    vec4 dofFar11 = textureLod(farFieldLowResTexture, texCoord11, 0);

    vec2 imageCoord = texCoords / pixelSize;
    vec2 fractional = fract(imageCoord);
    float a = (1.0f - fractional.x) * (1.0f - fractional.y);
    float b = fractional.x * (1.0f - fractional.y);
    float c = (1.0f - fractional.x) * fractional.y;
    float d = fractional.x * fractional.y;

    vec4 dofFar = vec4(0.0f);
    float weightsSum = 0.0f;

    float weight00 = a / (cocsFarDiffs.x + 0.001f);
    dofFar += weight00 * dofFar00;
    weightsSum += weight00;

    float weight10 = b / (cocsFarDiffs.y + 0.001f);
    dofFar += weight10 * dofFar10;
    weightsSum += weight10;

    float weight01 = c / (cocsFarDiffs.z + 0.001f);
    dofFar += weight01 * dofFar01;
    weightsSum += weight01;

    float weight11 = d / (cocsFarDiffs.w + 0.001f);
    dofFar += weight11 * dofFar11;
    weightsSum += weight11;

    dofFar /= weightsSum;

    float blend = 1.0f;
    result = mix(result, dofFar, blend * cocFar);

    // near field blending
    float cocNear = textureLod(nearCoCBlurredLowResTexture, texCoords, 0).x;
    if(cocNear > 0)
        isSharp = false;
    vec4 dofNear = textureLod(nearFieldLowResTexture, texCoords, 0);

    result = mix(result, dofNear, blend * cocNear);
    return  result.rgb;
}

#endif //DEPTH_OF_FIELD_INCLUDE
