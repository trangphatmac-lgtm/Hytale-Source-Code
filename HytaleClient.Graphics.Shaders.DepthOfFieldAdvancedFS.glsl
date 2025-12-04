#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uSceneColorLowResTexture;
uniform sampler2D uColorMulFarCoCLowResTextureLinear;
uniform sampler2D uCoCLowResTextureLinear;
uniform sampler2D uColorMulFarCoCLowResTexturePoint;
uniform sampler2D uCoCLowResTexturePoint;
uniform sampler2D uNearCoCBlurredLowResTexture;

uniform vec2 uPixelSize;
uniform float uFarBlurMax;
uniform float uNearBlurMax;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec4 outNearField;
layout(location = 1) out vec4 outFarField;

//-------------------------------------------------------------------------------------------------------------------------

vec4 Far(vec2 texcoords);
vec4 Near(vec2 texcoords);

void main(void)
{
    float cocNearBlurred = textureLod(uNearCoCBlurredLowResTexture, fragTexCoords, 0).x;
    float cocFar = textureLod(uCoCLowResTexturePoint, fragTexCoords, 0).y;
    vec4 color = textureLod(uSceneColorLowResTexture, fragTexCoords, 0);

    outNearField = (cocNearBlurred > 0.0f) ? Near(fragTexCoords) : color;
    outFarField = (cocFar > 0.0f) ? Far(fragTexCoords) : vec4(0.0f);
}

const int NB_SAMPLES = 48;
const vec2 offsets[NB_SAMPLES] = vec2[](
        2.0f * vec2(1.000000f, 0.000000f),
        2.0f * vec2(0.707107f, 0.707107f),
        2.0f * vec2(-0.000000f, 1.000000f),
        2.0f * vec2(-0.707107f, 0.707107f),
        2.0f * vec2(-1.000000f, -0.000000f),
        2.0f * vec2(-0.707106f, -0.707107f),
        2.0f * vec2(0.000000f, -1.000000f),
        2.0f * vec2(0.707107f, -0.707107f),

        4.0f * vec2(1.000000f, 0.000000f),
        4.0f * vec2(0.923880f, 0.382683f),
        4.0f * vec2(0.707107f, 0.707107f),
        4.0f * vec2(0.382683f, 0.923880f),
        4.0f * vec2(-0.000000f, 1.000000f),
        4.0f * vec2(-0.382684f, 0.923879f),
        4.0f * vec2(-0.707107f, 0.707107f),
        4.0f * vec2(-0.923880f, 0.382683f),
        4.0f * vec2(-1.000000f, -0.000000f),
        4.0f * vec2(-0.923879f, -0.382684f),
        4.0f * vec2(-0.707106f, -0.707107f),
        4.0f * vec2(-0.382683f, -0.923880f),
        4.0f * vec2(0.000000f, -1.000000f),
        4.0f * vec2(0.382684f, -0.923879f),
        4.0f * vec2(0.707107f, -0.707107f),
        4.0f * vec2(0.923880f, -0.382683f),

        6.0f * vec2(1.000000f, 0.000000f),
        6.0f * vec2(0.965926f, 0.258819f),
        6.0f * vec2(0.866025f, 0.500000f),
        6.0f * vec2(0.707107f, 0.707107f),
        6.0f * vec2(0.500000f, 0.866026f),
        6.0f * vec2(0.258819f, 0.965926f),
        6.0f * vec2(-0.000000f, 1.000000f),
        6.0f * vec2(-0.258819f, 0.965926f),
        6.0f * vec2(-0.500000f, 0.866025f),
        6.0f * vec2(-0.707107f, 0.707107f),
        6.0f * vec2(-0.866026f, 0.500000f),
        6.0f * vec2(-0.965926f, 0.258819f),
        6.0f * vec2(-1.000000f, -0.000000f),
        6.0f * vec2(-0.965926f, -0.258820f),
        6.0f * vec2(-0.866025f, -0.500000f),
        6.0f * vec2(-0.707106f, -0.707107f),
        6.0f * vec2(-0.499999f, -0.866026f),
        6.0f * vec2(-0.258819f, -0.965926f),
        6.0f * vec2(0.000000f, -1.000000f),
        6.0f * vec2(0.258819f, -0.965926f),
        6.0f * vec2(0.500000f, -0.866025f),
        6.0f * vec2(0.707107f, -0.707107f),
        6.0f * vec2(0.866026f, -0.499999f),
        6.0f * vec2(0.965926f, -0.258818f)
            );

vec4 Far(vec2 texcoords)
{
    float kernelScale = uFarBlurMax;
    vec4 result = textureLod(uColorMulFarCoCLowResTexturePoint, texcoords, 0);
    float weightSum = textureLod(uCoCLowResTexturePoint, texcoords, 0).y;
    float cocFar = weightSum;

    for(int i = 0; i < NB_SAMPLES; i++)
    {
        vec2 offset = kernelScale * offsets[i] * uPixelSize * cocFar;
        float cocSample = textureLod(uCoCLowResTextureLinear, texcoords + offset, 0).y;
        vec4 vsample = textureLod(uColorMulFarCoCLowResTextureLinear, texcoords + offset, 0);

        result += vsample;

        weightSum += cocSample;
    }
    return result / weightSum;
}

vec4 Near(vec2 texcoords)
{
    vec4 result = textureLod(uSceneColorLowResTexture, texcoords, 0);
    float kernelScale = uNearBlurMax;
    float cocSample = textureLod(uNearCoCBlurredLowResTexture, texcoords, 0).x;

    for(int i = 0; i < NB_SAMPLES; i++)
    {
        vec2 offset = kernelScale * offsets[i] * uPixelSize * cocSample;
        vec4 vsample = textureLod(uSceneColorLowResTexture, texcoords + offset, 0);
        result += vsample;
    }
    return result / (NB_SAMPLES +1);
}
