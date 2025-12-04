#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTextureLinear;  // full res
uniform sampler2D uColorTexturePoint;   // full res
uniform sampler2D uCoCTexture;          // full res
uniform vec2 uPixelSize;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 outColorMulFar;
layout(location = 2) out vec2 outCoC;
//layout(location = 1) out vec4 out_fragColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    vec2 texCoord00 = fragTexCoords + vec2(-0.25f, -0.25f) * uPixelSize;
    vec2 texCoord10 = fragTexCoords + vec2( 0.25f, -0.25f) * uPixelSize;
    vec2 texCoord01 = fragTexCoords + vec2(-0.25f,  0.25f) * uPixelSize;
    vec2 texCoord11 = fragTexCoords + vec2( 0.25f,  0.25f) * uPixelSize;

    vec2 coc = textureLod(uCoCTexture, texCoord00, 0).xy;
    vec4 color = textureLod(uColorTextureLinear, fragTexCoords, 0);

    float cocFar00 = textureLod(uCoCTexture, texCoord00, 0).y;
    float cocFar10 = textureLod(uCoCTexture, texCoord10, 0).y;
    float cocFar01 = textureLod(uCoCTexture, texCoord01, 0).y;
    float cocFar11 = textureLod(uCoCTexture, texCoord11, 0).y;

    float weight00 = 1000.0f;

    vec4 colorMulCOCFar = weight00 * textureLod(uColorTexturePoint,texCoord00,0);
    float weightsSum = weight00;

    float weight10 = 1.0f / (abs(cocFar00 - cocFar10) + 0.001f);
    colorMulCOCFar += weight10 * textureLod(uColorTexturePoint, texCoord10,0);
    weightsSum += weight10;

    float weight01 = 1.0f / (abs(cocFar00 - cocFar01) + 0.001f);
    colorMulCOCFar += weight01 * textureLod(uColorTexturePoint, texCoord01, 0);
    weightsSum += weight01;

    float weight11 = 1.0f / (abs(cocFar00 - cocFar11) + 0.001f);
    colorMulCOCFar += weight11 * textureLod(uColorTexturePoint, texCoord11, 0);
    weightsSum += weight11;

    colorMulCOCFar /= weightsSum;
    colorMulCOCFar *= coc.y;

    outColor = color.xyz;
    outColorMulFar = colorMulCOCFar.xyz;
    outCoC = coc;
}
