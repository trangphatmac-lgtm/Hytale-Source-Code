#ifndef NOISE_INCLUDE
#define NOISE_INCLUDE

#ifndef SAMPLES_COUNT
#define SAMPLES_COUNT 8
#endif

// Warning : Make sure you have defined SAMPLES_COUNT before including this file.
// #define SAMPLES_COUNT   16
//-----------------------------------------------------------
#define PI              3.1415f
#define TWO_PI          2.0f * PI
#define GOLDEN_ANGLE    2.4f

vec2 VogelDiskOffset(int sampleIndex, float phi, out float radiusFactor)
{
    // Note: split out tempA and tempB to try and resolve AMD GLSL compile errors
    float tempA = sqrt(float(sampleIndex) + 0.5f);
    float tempB = sqrt(float(SAMPLES_COUNT));
    float r = tempA / tempB;
    float angle = sampleIndex * GOLDEN_ANGLE + phi;

    float sine = sin(angle);
    float cosine = cos(angle);

    radiusFactor = r;

    return vec2(r * cosine, r * sine);
}

vec2 VogelDiskOffset(int sampleIndex, vec2 sampleData[16], float phi, out float radiusFactor)
{
    // Optimized by computing all 'r' values on the CPU, and sending them as a uniform array
    // also (sampleIndex * GOLDEN_ANGLE) is computed on the CPU
    // float r = sqrt(sampleIndex + 0.5f) / sqrt(SAMPLES_COUNT);
    // float angle = sampleIndex * GOLDEN_ANGLE + phi;

    float r = sampleData[sampleIndex].x;
    float angle = sampleData[sampleIndex].y + phi;

    float sine = sin(angle);
    float cosine = cos(angle);

    radiusFactor = r;

    return vec2(r * cosine, r * sine);
}

float AlchemyNoise(ivec2 posSS)
{
    // Hash function used in the HPG12 AlchemyAO paper
    // This one seems to have a problem... is the input valid ?
    return 30.0f * (posSS.x ^ posSS.y) + 10.0f * (posSS.x * posSS.y);
}

vec2 AlchemySpiralOffset(int sampleIndex, float phi)
{
    float alpha = float(sampleIndex + 0.5f) / SAMPLES_COUNT;
    float angle = 7.0f * TWO_PI*alpha + phi;

    float sine = sin(angle);
    float cosine = cos(angle);

    return vec2(cosine, sine);
}

#undef PI
#undef TWO_PI
#undef GOLDEN_ANGLE

//-----------------------------------------------------------
float InterleavedGradientNoise(vec2 posSS)
{
    const vec3 magic = vec3(0.06711056f, 4.0f * 0.00583715f, 52.9829189f);
    return fract(magic.z * fract(dot(posSS, magic.xy)));
}

float InterleavedGradientNoise3d(vec3 seed)
{
    const vec3 magic = vec3(0.06711056f, 4.0f * 0.00583715f, 52.9829189f);
    return fract(magic.z * fract(dot(seed, magic)));
}


// by inigo quilez - https://www.shadertoy.com/view/Xd23Dh
vec3 hash3(vec2 p)
{
    vec3 q = vec3(dot(p,vec2(127.1,311.7)), 
				   dot(p,vec2(269.5,183.3)), 
				   dot(p,vec2(419.2,371.9)));
	return fract(sin(q)*43758.5453);
}

#endif //NOISE_INCLUDE
