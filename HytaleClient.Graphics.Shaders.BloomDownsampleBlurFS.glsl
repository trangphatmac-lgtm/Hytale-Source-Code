#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform vec2 uPixelSize;

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
#if METHOD == 3
// 13 fetches method
    vec2 texCoordA = fragTexCoords + vec2(-0.5f,  0.5f) * uPixelSize;
    vec2 texCoordB = fragTexCoords + vec2( 0.5f,  0.5f) * uPixelSize;
    vec2 texCoordC = fragTexCoords + vec2(-0.5f, -0.5f) * uPixelSize;
    vec2 texCoordD = fragTexCoords + vec2( 0.5f, -0.5f) * uPixelSize;

    vec2 texCoordE = fragTexCoords + vec2(-1.0f,  1.0f) * uPixelSize;
    vec2 texCoordF = fragTexCoords + vec2( 0.0f,  1.0f) * uPixelSize;
    vec2 texCoordG = fragTexCoords + vec2( 1.0f,  1.0f) * uPixelSize;
    vec2 texCoordH = fragTexCoords + vec2( 1.0f,  0.0f) * uPixelSize;
    
    vec2 texCoordI = fragTexCoords + vec2( 1.0f, -1.0f) * uPixelSize;
    vec2 texCoordJ = fragTexCoords + vec2( 0.0f, -1.0f) * uPixelSize;
    vec2 texCoordK = fragTexCoords + vec2(-1.0f, -1.0f) * uPixelSize;
    vec2 texCoordL = fragTexCoords + vec2(-1.0f,  0.0f) * uPixelSize;

    vec2 texCoordM = fragTexCoords;

    vec3 colorA = texture(uColorTexture, texCoordA).rgb;
    vec3 colorB = texture(uColorTexture, texCoordB).rgb;
    vec3 colorC = texture(uColorTexture, texCoordC).rgb;
    vec3 colorD = texture(uColorTexture, texCoordD).rgb;

    vec3 colorE = texture(uColorTexture, texCoordE).rgb;
    vec3 colorF = texture(uColorTexture, texCoordF).rgb;
    vec3 colorG = texture(uColorTexture, texCoordG).rgb;
    vec3 colorH = texture(uColorTexture, texCoordH).rgb;

    vec3 colorI = texture(uColorTexture, texCoordI).rgb;
    vec3 colorJ = texture(uColorTexture, texCoordJ).rgb;
    vec3 colorK = texture(uColorTexture, texCoordK).rgb;
    vec3 colorL = texture(uColorTexture, texCoordL).rgb;

    vec3 colorM = texture(uColorTexture, texCoordM).rgb;

    vec3 avg1 = (colorA + colorB + colorC + colorD) * 0.25f;
    vec3 avg2 = (colorE + colorF + colorL + colorM) * 0.25f;
    vec3 avg3 = (colorF + colorG + colorM + colorH) * 0.25f;
    vec3 avg4 = (colorM + colorH + colorJ + colorI) * 0.25f;
    vec3 avg5 = (colorL + colorM + colorK + colorJ) * 0.25f;

    outColor = avg1 * 0.5 + (avg2 + avg3 + avg4 + avg5)*0.125;
#else
#if METHOD == 2
// 4 fetches method
    vec2 texCoord00 = fragTexCoords + vec2(-0.5f, -0.5f) * uPixelSize;
    vec2 texCoord10 = fragTexCoords + vec2( 0.5f, -0.5f) * uPixelSize;
    vec2 texCoord01 = fragTexCoords + vec2(-0.5f,  0.5f) * uPixelSize;
    vec2 texCoord11 = fragTexCoords + vec2( 0.5f,  0.5f) * uPixelSize;

    vec3 color00 = texture(uColorTexture, texCoord00).rgb;
    vec3 color10 = texture(uColorTexture, texCoord10).rgb;
    vec3 color01 = texture(uColorTexture, texCoord01).rgb;
    vec3 color11 = texture(uColorTexture, texCoord11).rgb;

    outColor = (color00 + color01 + color10 + color11) * 0.25f;
#else
#if METHOD == 1
// 2 fetches, alternated 4 fetches method
    bool isEven = mod(gl_FragCoord.x , 2) == 0;
    vec2 texCoord1, texCoord2;

    if(isEven)
    {
        texCoord1 = fragTexCoords + vec2(-0.5f, -0.5f) * uPixelSize;
        texCoord2 = fragTexCoords + vec2( 0.5f,  0.5f) * uPixelSize;
    }
    else
    {
        texCoord1 = fragTexCoords + vec2( 0.5f, -0.5f) * uPixelSize;
        texCoord2 = fragTexCoords + vec2(-0.5f,  0.5f) * uPixelSize;
    }

    vec3 color1 = texture(uColorTexture, texCoord1).rgb;
    vec3 color2 = texture(uColorTexture, texCoord2).rgb;

    outColor = (color1 + color2) * 0.5f;
#else // METHOD == 0
// 1 fetch, basic cheap method
    vec3 color00 = texture(uColorTexture, fragTexCoords).rgb;

    outColor = color00;
#endif
#endif
#endif
}
