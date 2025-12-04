#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform sampler2D uColorLowResTexture;
uniform vec2 uPixelSize;
uniform float uScale;
uniform vec2 uIntensity;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
#if METHOD ==2
// 9 fetches, tent filter method
    mat3 weight;
    weight[0] = vec3(1.0f, 2.0f, 1.0f) * 0.0625f;
    weight[1] = vec3(2.0f, 4.0f, 2.0f) * 0.0625f;
    weight[2] = vec3(1.0f, 2.0f, 1.0f) * 0.0625f;

    vec2 offsets[9];
    offsets[0] = vec2(-1,  1);
    offsets[1] = vec2(-1,  0);
    offsets[2] = vec2(-1, -1);
    offsets[3] = vec2( 0,  1);
    offsets[4] = vec2( 0,  0);
    offsets[5] = vec2( 0, -1);
    offsets[6] = vec2( 1,  1);
    offsets[7] = vec2( 1,  0);
    offsets[8] = vec2( 1, -1);

    vec3 color = vec3(0);
    for(int i = 0; i<3; i++)
    for(int j = 0; j<3; j++)
    {
        color += texture(uColorLowResTexture, fragTexCoords + uScale * uPixelSize * offsets[i+ 3*j]).rgb * weight[i][j];
    }
    color *= uIntensity.x;
    color += texture(uColorTexture, fragTexCoords).rgb * uIntensity.y;

    outColor = color;
#else
#if METHOD == 1
// 5 fetches, cheap tent filter method
    vec2 texCoordA = fragTexCoords + vec2(-1.0f,  0.0f) * uPixelSize;
    vec2 texCoordB = fragTexCoords + vec2( 1.0f,  0.0f) * uPixelSize;
    vec2 texCoordC = fragTexCoords + vec2( 0.0f, -1.0f) * uPixelSize;
    vec2 texCoordD = fragTexCoords + vec2( 0.0f,  1.0f) * uPixelSize;

    vec3 colorA = texture(uColorLowResTexture, texCoordA).rgb;
    vec3 colorB = texture(uColorLowResTexture, texCoordB).rgb;
    vec3 colorC = texture(uColorLowResTexture, texCoordC).rgb;
    vec3 colorD = texture(uColorLowResTexture, texCoordD).rgb;

    vec3 colorO = texture(uColorLowResTexture, fragTexCoords).rgb;

    vec3 color = (colorA + colorB + colorC + colorD) / 6;
    color += colorO / 3;

    color *= uIntensity.x;
    color += texture(uColorTexture, fragTexCoords).rgb * uIntensity.y;

    outColor = color;
#else // METHOD == 0
// 1 fetch, basic cheap method
    vec3 color1 = texture(uColorTexture, fragTexCoords).rgb;
    vec3 color2 = texture(uColorLowResTexture, fragTexCoords).rgb;

    outColor = color1 * uIntensity.y + color2 * uIntensity.x;
#endif
#endif
}
