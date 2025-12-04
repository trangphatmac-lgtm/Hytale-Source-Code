#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uCoCLowResTexture;
uniform sampler2D uNearCoCBlurredLowResTexture;
uniform sampler2D uNearFieldLowResTexture;
uniform sampler2D uFarFieldLowResTexture;

uniform vec2 uPixelSize;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec4 outNearFill;
layout(location = 1) out vec4 outFarFill;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    float cocNearBlurred = textureLod(uNearCoCBlurredLowResTexture, fragTexCoords, 0).x;
    float cocFar = textureLod(uCoCLowResTexture, fragTexCoords, 0).y;

    if(cocNearBlurred > 0.0f)
    {
        vec4 maxFill = vec4(0.0f);
        for(int i = -1; i<= 1 ; i++)
        {
            for(int j = -1; j<= 1 ;j ++)
            {
                vec2 sampleTexCoord = fragTexCoords + vec2(i,j)*uPixelSize;
                vec4 vsample = textureLod(uNearFieldLowResTexture, sampleTexCoord, 0);
                maxFill = max(maxFill, vsample);
            }
        }
        outNearFill = maxFill;
    }
    else
    {
        outNearFill = vec4(0.0f);
    }

    if(cocFar > 0.0f)
    {
        vec4 maxFill = vec4(0.0f);
        for(int i = -1; i<= 1 ; i++)
        {
            for(int j = 0; j<= 1 ;j ++)
            {
                vec2 sampleTexCoord = fragTexCoords + vec2(i,j)*uPixelSize;
                vec4 vsample = textureLod(uFarFieldLowResTexture, sampleTexCoord, 0);
                maxFill = max(maxFill, vsample);
            }
        }
        outFarFill = maxFill;
    }
    else
    {
        outFarFill = vec4(0.0);
    }
}
