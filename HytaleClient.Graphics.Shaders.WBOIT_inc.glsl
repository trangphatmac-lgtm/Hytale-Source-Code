#ifndef WBOIT_INCLUDE
#define WBOIT_INCLUDE

#define saturate(a) (clamp(a, 0.0f, 1.0f))

// This function is executed in alpha material shaders as the last step before writing out to the MRTs

float maxComponent(vec3 value)
{
    return max(value.r, max(value.g, value.b));
}

void processWBOIT(out vec4 accum, out float revealage, vec4 premultipliedReflect, vec3 transmit, float csZ)
{
//    Modulate the net coverage for composition by the transmission. This does not affect the color channels of the
//    transparent surface because the caller's BSDF model should have already taken into account if transmission modulates
//    reflection. This model doesn't handled colored transmission, so it averages the color channels. See 
// 
//    McGuire and Enderton, Colored Stochastic Shadow Maps, ACM I3D, February 2011
//    http://graphics.cs.williams.edu/papers/CSSM/
// 
//    for a full explanation and derivation.

    vec4 color = premultipliedReflect;
 
    premultipliedReflect.a *= 1.0 - clamp((transmit.r + transmit.g + transmit.b) * (1.0 / 3.0), 0, 1);
 
//    You may need to adjust the w function if you have a very large or very small view volume; see the paper and
//       presentation slides at http://jcgt.org/published/0002/02/09/
    // Intermediate terms to be cubed
    float a = min(1.0, premultipliedReflect.a) * 8.0 + 0.01;
    float b = -gl_FragCoord.z * 0.95 + 1.0;
 
//     If your scene has a lot of content very close to the far plane,
//       then include this line (one rsqrt instruction):
//       b /= sqrt(1e4 * abs(csZ)); 
    float w    = clamp(a * a * a * 1e8 * b * b * b, 1e-2, 3e2);
//    float w    = max(min(1.0, maxComponent(color.rgb) * color.a), color.a) * clamp(0.03 / (1e-5 + pow(csZ / 200, 4.0)), 1e-2, 3e3);
    accum     = premultipliedReflect * w;
    revealage = premultipliedReflect.a;
}

void processWBOIT_color(out vec4 accum, out float revealage, out vec3 modulate, vec4 premultipliedReflect, vec3 transmit, float csZ)
{
	// NEW: Perform this operation before modifying the coverage to account for transmission.
    modulate = premultipliedReflect.aaa * (vec3(1.0) - transmit);
	
	processWBOIT(accum, revealage, premultipliedReflect, transmit, csZ);
}

void processWBOIT_e(out vec4 accum, out float revealage, out float emissive_weight, vec4 premultipliedReflect, vec3 transmit, float csZ)
{
//    Modulate the net coverage for composition by the transmission. This does not affect the color channels of the
//    transparent surface because the caller's BSDF model should have already taken into account if transmission modulates
//    reflection. This model doesn't handled colored transmission, so it averages the color channels. See 
// 
//    McGuire and Enderton, Colored Stochastic Shadow Maps, ACM I3D, February 2011
//    http://graphics.cs.williams.edu/papers/CSSM/
// 
//    for a full explanation and derivation.

    vec4 color = premultipliedReflect;
 
    premultipliedReflect.a *= 1.0 - clamp((transmit.r + transmit.g + transmit.b) * (1.0 / 3.0), 0, 1);
 
//    You may need to adjust the w function if you have a very large or very small view volume; see the paper and
//       presentation slides at http://jcgt.org/published/0002/02/09/
    // Intermediate terms to be cubed
    float a = min(1.0, premultipliedReflect.a) * 8.0 + 0.01;
    float b = -gl_FragCoord.z * 0.95 + 1.0;
 
//     If your scene has a lot of content very close to the far plane,
//       then include this line (one rsqrt instruction):
//       b /= sqrt(1e4 * abs(csZ)); 
    float w    = clamp(a * a * a * 1e8 * b * b * b, 1e-2, 3e2);
//    float w    = max(min(1.0, maxComponent(color.rgb) * color.a), color.a) * clamp(0.03 / (1e-5 + pow(csZ / 200, 4.0)), 1e-2, 3e3);
    accum     = premultipliedReflect * w;
    revealage = premultipliedReflect.a;
    emissive_weight = saturate((transmit.r + transmit.g + transmit.b) * (0.6))/8.0; //we're going to store this into an 8-bit channel, so we divide by the maximum number of additive layers we can support
}

void processPOIT(vec3 L_r, float alpha, vec3 t, float c, float eta, vec3 X, vec3 n, float sceneLinearZ, out vec4 A, out vec3 beta, out float D, out vec2 delta)
{
    float netCoverage = alpha * (1.0 - dot(t, vec3(1.0/3.0)));
    float tmp = (1.0 - gl_FragCoord.z * 0.99) * netCoverage * 10.0;

    float w = clamp(tmp * tmp * tmp, 0.01, 30.0);

    A = vec4(L_r * alpha, netCoverage) * w;
    beta = alpha * (vec3(1.0) - t) * (1.0 / 3.0);

    const float k_0 = 120.0 / 63.0;
    const float k_1 = 0.05;
    vec2 refractPix = vec2(0.5);//refractOffset(n, X, eta);
    float z_B = sceneLinearZ;//depthToZ(texelFetch(depthBuffer, ivec2(gl_FragCoord.xy), 0).r, clipInfo);

    D = k_0 * netCoverage * (1.0 - c) * (1.0 - k_1 / (k_1 + X.z - z_B)) / abs(X.z);
    D *= D; // Store D2, variance, during summation
    if (D > 0.0) D = max(D, 1.0 / 256.0);

    delta = refractPix * netCoverage * (1.0 / 63.0);
}

void processWBOITExtended(out vec4 accum, out float revealage, out float emissive_weight, vec4 premultiplied_alpha_color, float raw_emissive_luminance, float view_depth, float current_camera_exposure)
{
const float opacity_sensitivity = 3.0; // Should be greater than 1, so that we only downweight nearly transparent things. Otherwise, everything at the same depth should get equal weight. Can be artist controlled
const float weight_bias = 5.0; //Must be greater than zero. Weight bias helps prevent distant things from getting hugely lower weight than near things, as well as preventing floating point underflow
const float precision_scalar = 10000.0;  //adjusts where the weights fall in the floating point range, used to balance precision to combat both underflow and overflow
const float maximum_weight = 20.0;  //Don't weight near things more than a certain amount to both combat overflow and reduce the "overpower" effect of very near vs. very far things
const float maximum_color_value = 1000.0;
const float additive_sensitivity = 10.0; //how much we amplify the emissive when deciding whether to consider this additively blended

// Exposure changes relative importance of emissive luminance (whereas it does not for opacity)
float relative_emissive_luminance = raw_emissive_luminance * current_camera_exposure;

//Emissive sensitivity is hard to pin down
//On the one hand, we want a low sensitivity so we don't get dark halos around "feathered" emissive alpha that overlap with eachother
//On the other hand, we want a high sensitivity so that dim emissive holograms don't get overly downweighted.
//We expose this to the artist to let them choose what is more important.
const float emissive_sensitivity = 1; //<<artist controlled value between 0.01 and 1>>;

float clamped_emissive = saturate(relative_emissive_luminance);
float clamped_alpha = saturate(premultiplied_alpha_color.a);

// Intermediate terms to be cubed
// NOTE: This part differs from McGuire's sample code:
// since we're using premultiplied alpha in the composite, we want to
// keep emissive values that have low coverage weighted appropriately
// so, we'll add the emissive luminance to the alpha when computing the alpha portion of the weight
// NOTE: We also don't add a small value to a, we allow it to go all the way to zero, so that completely invisible portions do not influence the result
float a = saturate((clamped_alpha*opacity_sensitivity) + (clamped_emissive*emissive_sensitivity));

// NOTE: This differs from McGuire's sample code. In order to avoid having to tune the algorithm separately for different
// near/far plane values, we produce a "canonical" depth value from the view-depth, using an fixed near plane and a tunable far plane
const float canonical_near_z = 0.5;
const float canonical_far_z = 300.0;
float range = canonical_far_z-canonical_near_z;
float canonical_depth = saturate(canonical_far_z/range - (canonical_far_z*canonical_near_z)/(view_depth*range));
float b = 1.0 - canonical_depth;

// clamp color to combat overflow (weight will be clamped too)
vec3 clamped_color = min(premultiplied_alpha_color.rgb, maximum_color_value);

float w = precision_scalar * b * b * b; //basic depth based weight
w += weight_bias; //NOTE: This differs from McGuire's code. It is an alternate way to prevent underflow and limits near/far weight ratio
w = min(w, maximum_weight); //clamp by maximum weight BEFORE multiplying by opacity weight (so that we'll properly reduce near faint stuff in weight)
w *= a * a * a; //incorporate opacity weight as the last step

//    a = min(1.0, premultiplied_alpha_color.a) * 8.0 + 0.01;
//    b = -gl_FragCoord.z * 0.95 + 1.0;
//       b /= sqrt(1e4 * abs(view_depth)); 
//    float w    = clamp(a * a * a * 1e8 * b * b * b, 1e-2, 3e2);

accum = vec4(clamped_color*w, w); //NOTE: This differs from McGuire's sample code because we want to be able to handle fully additive alpha (e.g. emissive), which has a coverage of 0 (revealage of 1.0)
revealage = clamped_alpha; //blend state will invert this to produce actual revealage
emissive_weight = saturate(relative_emissive_luminance*additive_sensitivity)/8.0f; //we're going to store this into an 8-bit channel, so we divide by the maximum number of additive layers we can support
}

#endif //WBOIT_INCLUDE
