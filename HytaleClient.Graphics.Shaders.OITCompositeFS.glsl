#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uAccumulationQuarterResTexture;
uniform sampler2D uRevealAddQuarterResTexture;
uniform sampler2D uAccumulationHalfResTexture;
uniform sampler2D uRevealAddHalfResTexture;
uniform sampler2D uAccumulationTexture;
uniform sampler2D uRevealAddTexture;
uniform sampler2D uBackgroundTexture;

uniform int uOITMethod;
#define OIT_NONE 0
#define OIT_WBOIT 1
#define OIT_WBOIT_EXTENDED 2
#define OIT_POIT 3
#define OIT_MOIT 4

uniform vec4 uInputResolutionUsed;
#define uUseInputFullRes (uInputResolutionUsed.x == 1)
#define uUseInputHalfRes (uInputResolutionUsed.y == 1)
#define uUseInputQuarterRes (uInputResolutionUsed.z == 1)

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

#define saturate(a) (clamp(a, 0.0f, 1.0f))

float maxComponent(vec4 value)
{
    return max(value.r, max(value.g, max(value.b, value.a)));
}

float maxComponent(vec3 value)
{
    return max(value.r, max(value.g, value.b));
}

float minComponent(vec3 value)
{
    return min(value.r, min(value.g, value.b));
}

float square(float value)
{
    return value*value;
}

//-------------------------------------------------------------------------------------------------------------------------

// Full-screen composite pixel shader
void main()
{
if (uOITMethod == OIT_WBOIT || uOITMethod == OIT_MOIT)
{
    float reveal = 1;
    reveal = uUseInputFullRes ? min(reveal, texture(uRevealAddTexture, fragTexCoords).r) : reveal;
    reveal = uUseInputHalfRes ? min(reveal, texture(uRevealAddHalfResTexture, fragTexCoords).r ): reveal;
    reveal = uUseInputQuarterRes ? min(reveal, texture(uRevealAddQuarterResTexture, fragTexCoords).r) : reveal;

    if (reveal == 1.0)
    {
        discard; 
    }

    vec4 accumulation = vec4(0);
    accumulation += uUseInputFullRes ? texture(uAccumulationTexture, fragTexCoords).rgba : vec4(0);
    accumulation += uUseInputHalfRes ? texture(uAccumulationHalfResTexture, fragTexCoords).rgba : vec4(0);
    accumulation += uUseInputQuarterRes ? texture(uAccumulationQuarterResTexture, fragTexCoords).rgba : vec4(0);

    // Suppress overflow
    if (any(isinf(accumulation)))
    {
        accumulation.rgb = vec3(accumulation.a);
    }
    vec3 averageColor = accumulation.rgb / max(accumulation.a, 0.00001);
  
    // dst' =  (accum.rgb / accum.a) * (1 - revealage) + dst * revealage
    outColor = vec4(averageColor, 1 - reveal);
}
else
if (uOITMethod == OIT_WBOIT_EXTENDED)
{
    ivec3 ipos = ivec3(gl_FragCoord.xy, 0);
#if (defined(_PS4) || defined(_XBOX3)) && defined(USE_CMASK_OPT)
    // skip some work for pixels that we didn't write to at all
    const bool hires_written = decoded_cmask.Load(uvec3(ipos.x/4,ipos.y/4,0))!=0.0f;
#else
    const bool hires_written = true;
#endif

    float revealage = 1.0;
    float additiveness = 0.0;
    vec4 accum = vec4(0.0,0.0,0.0,0.0);

    // high-res alpha
    //[branch]
    if(hires_written)
    {
        vec2 tempFull = uUseInputFullRes ? texture(uRevealAddTexture, fragTexCoords).ra : vec2(1,0);
        vec2 tempHalf = uUseInputHalfRes? texture(uRevealAddHalfResTexture, fragTexCoords).ra : vec2(1,0);
        vec2 tempQuarter = uUseInputQuarterRes? texture(uRevealAddQuarterResTexture, fragTexCoords).ra : vec2(1,0);

        revealage = tempFull.x * tempHalf.x * tempQuarter.x;
//        revealage = min(tempFull.x, min(tempHalf.x, tempQuarter.x));
        additiveness = tempFull.y + tempHalf.y + tempQuarter.y;

        accum = uUseInputFullRes ? texture(uAccumulationTexture, fragTexCoords).rgba : vec4(0);
        accum += uUseInputHalfRes ? texture(uAccumulationHalfResTexture, fragTexCoords).rgba : vec4(0);
        accum += uUseInputQuarterRes ? texture(uAccumulationQuarterResTexture, fragTexCoords).rgba : vec4(0);
    }

    // low-res alpha
    ////vec4 temp = input_accum2_subpass.SampleLevel(Sampler_filter_clamp, input.uv, 0);
    //vec4 temp = textureLod(input_accum2_subpass, input.uv, 0);
    //revealage = revealage * temp.r;
    //additiveness = additiveness + temp.w;
    //
    ////accum = accum + input_accum1_subpass.SampleLevel(Sampler_filter_clamp, input.uv, 0);
    //accum = accum + textureLod(input_accum1_subpass, input.uv, 0);

    // weighted average (weights were applied during accumulation, and accum.a stores the sum of weights)
    vec3 average_color = accum.rgb / max(accum.a, 0.00001);

    // Amplify based on additiveness to try and regain intensity we lost from averaging things that would formerly have been additive.
    // Revealage gives a rough estimate of how much "alpha stuff" there is in the pixel, allowing us to reduce the additive amplification when mixed in with non-additive
    float emissive_amplifier = (additiveness * 8.0f); //The constant factor here must match the constant divisor in the material shaders!
    emissive_amplifier = mix(emissive_amplifier * 0.25, emissive_amplifier, revealage); //lessen, but do not completely remove amplification when there's opaque stuff mixed in

    // Also add in the opacity (1-revealage) to account for the fact that additive + non-additive should never be darker than the non-additive by itself
    emissive_amplifier += saturate((1.0-revealage)*2.0); //constant factor here is an adjustable thing to indicate how "sensitive" we should be to the presence of opaque stuff

    average_color *= max(emissive_amplifier,1.0); // NOTE: We max with 1 here so that this can only amplify, never darken, the result

    // Suppress overflow (turns INF into bright white)
    if (any(isinf(accum.rgb)))
    {
        average_color = vec3(1.0f);
    }

//    average_color = pow(average_color, vec3(1.0/2.2));

    outColor = vec4(average_color, 1.0 - revealage);
}
else
if (uOITMethod == OIT_POIT)
{
    // Pixels per unit diffusion std dev
    const float PPD = 200.0;
    const int maxDiffusionPixels = 16;

    #define fetch(a, b) texelFetch(a, b, 0)

    vec2 bkgSize = textureSize(uBackgroundTexture, 0).xy;
    ivec2 C = ivec2(gl_FragCoord.xy);

    vec4 BD;
    vec4 BDFull = uUseInputFullRes ? texture(uRevealAddTexture, fragTexCoords).rgba : vec4(1,1,1,0);
    vec4 BDHalf = uUseInputHalfRes ? texture(uRevealAddHalfResTexture, fragTexCoords).rgba : vec4(1,1,1,0);
    vec4 BDQuarter = uUseInputQuarterRes ? texture(uRevealAddQuarterResTexture, fragTexCoords).rgba : vec4(1,1,1,0);

    BD.rgb = min(BDFull.rgb, min(BDHalf.rgb, BDQuarter.rgb));
    BD.a = BDFull.a + BDHalf.a + BDQuarter.a;

    vec3 B = BD.rgb;

    if (minComponent(B) == 1.0)
    {
//        discard;
        // No transparency
        outColor.rgb = texelFetch(uBackgroundTexture, C, 0).rgb;
        outColor.a = 1;
        return;
    }

    float D2 = BD.a * square(PPD);
    vec4 A = vec4(0);
    A += uUseInputFullRes ? texture(uAccumulationTexture, fragTexCoords) : vec4(0);
    A += uUseInputHalfRes ? texture(uAccumulationHalfResTexture, fragTexCoords) : vec4(0);
    A += uUseInputQuarterRes ? texture(uAccumulationQuarterResTexture, fragTexCoords) : vec4(0);

    // Suppress under- and over-flow
    if (isinf(A.a)) A.a = maxComponent(A.rgb);
    if (isinf(maxComponent(A.rgb)))

    A = vec4(isinf(A.a) ? 1.0 : A.a);

    // Self-modulation
    A.rgb *= vec3(0.5) + max(B, vec3(1e-3)) / max(2e-3, 2 * maxComponent(B));

    // Refraction
    vec3 bkg = vec3(0);
    vec2 delta = vec2(0);

    {
        // No diffusion (+ fractional refraction)
        bkg = textureLod(uBackgroundTexture, delta + gl_FragCoord.xy / bkgSize, 0).rgb;
    }
    
    vec3 averageColor = A.rgb / max(A.a, 0.00001);

    //outColor = bkg * B + (vec3(1) - B) * averageColor;
    //RT = averageColor * (vec3(1) - B) + RT * B;
    
    outColor.rgb = averageColor * (vec3(1) - B) + bkg * B;
    outColor.a = 1;

//    outColor.rgba = vec4(averageColor, length(vec3(1) - B));

//    // Pixels per unit diffusion std dev
//    const float PPD = 200.0;
//    const int maxDiffusionPixels = 16;
//
//    #define fetch(a, b) texelFetch(a, b, 0)
//
//    vec2 bkgSize = textureSize(bkgTexture, 0).xy;
//    ivec2 C = ivec2(gl_FragCoord.xy);
//
//    vec4 BD = texelFetch(BDTex, C, 0);
//    vec3 B = BD.rgb;
//
//    if (minComponent(B) == 1.0)
//    {
//        // No transparency
//        result = fetch(bkgTexture, C).rgb;
//    return;
//    }
//
//    float D2 = BD.a * square(PPD);
//    vec2 delta = fetch(deltaTex, C).xy * 0.375;
//    vec4 A = fetch(ATex, C);
//
//    // Suppress under- and over-flow
//    if (isinf(A.a)) A.a = maxComponent(A.rgb);
//    if (isinf(maxComponent(A.rgb)))
//
//    A = vec4(isinf(A.a) ? 1.0 : A.a);
//
//    // Self-modulation
//    A.rgb *= vec3(0.5) + max(B, vec3(1e-3)) /
//    max(2e-3, 2 * maxComponent(B));
//
//    // Refraction
//    vec3 bkg = vec3(0);
//
//    // Diffusion
//    if (D2 > 0)
//    { 
//        C += ivec2(delta * bkgSize);
//
//        // Tap spacing
//        const float stride = 2;
//
//        // Kernel radius
//        int R = int(min(sqrt(D2), maxDiffusionPixels) / float(stride)) * stride;
//        float weightSum = 0;
//
//        for (vec2 q = vec2(-R); q.x <= R; q.x+=stride)
//        {
//            for (q.y = -R; q.y <= R; q.y+=stride)
//            {
//                float radius2 = dot(q, q);
//
//                if (radius2 <= D2)
//                {
//                    ivec2 tap = C + ivec2(q);
//                    float t = fetch(BDTex, tap).a;
//                    float bkgRadius2 = t * PPD * PPD;
//
//                    if (radius2 <= bkgRadius2)
//                    {
//                        // Disk filter (faster, looks similar)
//                        float w = 1.0 / bkgRadius2 + 1e-5;
//
//                        // True Gaussian filter
//                        //float w=exp(-radius2 / (8*bkgRadius2)) /
//                        // sqrt(4 * PI * t);
//
//                        bkg += w * fetch(bkgTexture, tap).rgb;
//                        weightSum += w;
//                    }
//                }
//            }
//        }
//
//        bkg /= weightSum;
//    }
//    else
//    {
//        // No diffusion (+ fractional refraction)
//        bkg = textureLod(bkgTexture,
//        delta + gl_FragCoord.xy / bkgSize, 0).rgb;
//    }
//
//    outColor = bkg * B + (vec3(1) - B) * A.rgb / max(A.a, 0.00001);
}
}
