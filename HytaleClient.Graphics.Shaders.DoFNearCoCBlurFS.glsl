#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uCoCTexture;
uniform float uHorizontalPass;
uniform vec2 uPixelSize;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out float outCoC;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    const int loopMin = -6;
    const int loopMax = 6;

    float cocNear = texture(uCoCTexture, fragTexCoords).x;
    float avgCoC = 0.0f;
    float count = 0;

    vec2 offset = vec2(0);
    for(int i = loopMin; i<=loopMax; i++)
    {
        offset = vec2( uPixelSize.x * i * uHorizontalPass,
                       uPixelSize.y * i * (1 - uHorizontalPass));

        float CoC = texture(uCoCTexture, fragTexCoords + offset).x;
        avgCoC += CoC;

    }
    avgCoC/= abs(loopMax - loopMin);
    outCoC = avgCoC;
}
