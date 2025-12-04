#version 330 core

#if USE_MOOD_FOG
#include "Fog_inc.glsl"
#endif // USE_MOOD_FOG

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture0;
uniform sampler2D uTexture1;
uniform sampler2D uTexture2;
uniform sampler2D uTexture3;
uniform sampler2D uFlowTexture;

uniform vec4[] uColors;
uniform float[] uUVOffsets;
uniform int uCloudsTextureCount;
uniform vec2 uUVMotionParams;
#define uvMotionScale uUVMotionParams.x
#define uvMotionStrength uUVMotionParams.y

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 fragSpherePosition;
in vec2 fragTexCoords;

#if USE_MOOD_FOG
in vec4 fragFogInfo;
#endif // USE_MOOD_FOG

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

// Same code but separated in 4 functions to have optimal performance

vec4 computeColorFor4Layers(float opacity, vec2 uv)
{
    vec4 textureColor0 = texture(uTexture0, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[0])));
    vec4 textureColor1 = texture(uTexture1, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[1])));
    vec4 textureColor2 = texture(uTexture2, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[2])));
    vec4 textureColor3 = texture(uTexture3, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[3])));

    if (textureColor0.a < 0.01 && textureColor1.a < 0.01 && textureColor2.a < 0.01 && textureColor3.a < 0.01) discard;

    textureColor0 = textureColor0 * uColors[0];
    textureColor1 = textureColor1 * uColors[1];
    textureColor2 = textureColor2 * uColors[2];
    textureColor3 = textureColor3 * uColors[3];

    // Blend the layers of clouds
    float resultA = textureColor0.a + (textureColor1.a * (1.0 - textureColor0.a));
    vec3 resultRGB = (textureColor0.rgb * textureColor0.a + textureColor1.rgb * textureColor1.a * (1.0 - textureColor0.a)) / resultA;
    vec4 result = vec4(resultRGB, resultA);

    resultA = result.a + (textureColor2.a * (1.0 - result.a));
    resultRGB = (result.rgb * result.a + textureColor2.rgb * textureColor2.a * (1.0 - result.a)) / resultA;
    result = vec4(resultRGB, resultA);

    resultA = result.a + (textureColor3.a * (1.0 - result.a));
    resultRGB = (result.rgb * result.a + textureColor3.rgb * textureColor3.a * (1.0 - result.a)) / resultA;
    result = vec4(resultRGB, resultA);

    return result * vec4(1.0, 1.0, 1.0, opacity);
}

vec4 computeColorFor3Layers(float opacity, vec2 uv)
{
    vec4 textureColor0 = texture(uTexture0, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[0])));
    vec4 textureColor1 = texture(uTexture1, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[1])));
    vec4 textureColor2 = texture(uTexture2, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[2])));

    if (textureColor0.a < 0.01 && textureColor1.a < 0.01 && textureColor2.a < 0.01) discard;

    textureColor0 = textureColor0 * uColors[0];
    textureColor1 = textureColor1 * uColors[1];
    textureColor2 = textureColor2 * uColors[2];

    // Blend the layers of clouds
    float resultA = textureColor0.a + (textureColor1.a * (1.0 - textureColor0.a));
    vec3 resultRGB = (textureColor0.rgb * textureColor0.a + textureColor1.rgb * textureColor1.a * (1.0 - textureColor0.a)) / resultA;
    vec4 result = vec4(resultRGB, resultA);

    resultA = result.a + (textureColor2.a * (1.0 - result.a));
    resultRGB = (result.rgb * result.a + textureColor2.rgb * textureColor2.a * (1.0 - result.a)) / resultA;
    result = vec4(resultRGB, resultA);

    return result * vec4(1.0, 1.0, 1.0, opacity);
}

vec4 computeColorFor2Layers(float opacity, vec2 uv)
{
    vec4 textureColor0 = texture(uTexture0, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[0])));
    vec4 textureColor1 = texture(uTexture1, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[1])));

    if (textureColor0.a < 0.01 && textureColor1.a < 0.01) discard;

    textureColor0 = textureColor0 * uColors[0];
    textureColor1 = textureColor1 * uColors[1];

    // Blend the layers of clouds
    float resultA = textureColor0.a + (textureColor1.a * (1.0 - textureColor0.a));
    vec3 resultRGB = (textureColor0.rgb * textureColor0.a + textureColor1.rgb * textureColor1.a * (1.0 - textureColor0.a)) / resultA;
    vec4 result = vec4(resultRGB, resultA);

    return result * vec4(1.0, 1.0, 1.0, opacity);
}

vec4 computeColorFor1Layer(float opacity, vec2 uv)
{
    vec4 textureColor0 = texture(uTexture0, vec2(uv * vec2(8.0, 4.0) + vec2(uUVOffsets[0])));

    if (textureColor0.a < 0.01) discard;

    textureColor0 = textureColor0 * uColors[0];
	
    return textureColor0 * vec4(1.0, 1.0, 1.0, opacity);
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
//    vec2 distortionParams = vec2(30, 0.0005);
//    vec2 distortionParams = vec2(50, 0.001);
    vec2 distortionParams = vec2(uvMotionScale, uvMotionStrength);

    vec2 flow = vec2(0);

    // Optimize : Skip if useless
    if (distortionParams.x != 0 && distortionParams.y != 0)
    {
        flow = texture(uFlowTexture, fragTexCoords * distortionParams.x).rg;
        //outColor = vec4(flow.xy,0,1); return;
    }

    // Decrease opacity as we get closer to the horizon
    float normalizedY = normalize(fragSpherePosition).y;
    float opacity = clamp((normalizedY - 0.5) * 2.0, 0.0, 1.0);

	// Optimize : Avoid all other computation & writing (and blending !) if the color is almost transparent
	if (opacity < 0.01) discard;

    // Get the 2d flow motion vector in [0,1] and remap it to [-1,1]
    flow = vec2(2.0) * flow - vec2(1.0);

    // Distorts the UV 
    vec2 distortionStrength = distortionParams.yy;
    vec2 uv = flow * distortionStrength + fragTexCoords;
	
	switch(uCloudsTextureCount)
	{
	case 4:	outColor = computeColorFor4Layers(opacity, uv);	break;
	case 3:	outColor = computeColorFor3Layers(opacity, uv);	break;
	case 2:	outColor = computeColorFor2Layers(opacity, uv);	break;
	case 1:	outColor = computeColorFor1Layer(opacity, uv);	break;
	}

#if USE_MOOD_FOG
    vec3 smoothMoodFogColor = fragFogInfo.rgb;
    float moodFogThickness = fragFogInfo.a;

#if USE_FOG_DITHERING
    vec3 noise = vec3(n2rand_faster(fragTexCoords));
    smoothMoodFogColor.rgb = ditherRGB(smoothMoodFogColor.rgb, noise, 255.0);
#endif // USE_FOG_DITHERING

    outColor.rgb = mix(outColor.rgb, smoothMoodFogColor, moodFogThickness);
#endif // USE_MOOD_FOG
}
