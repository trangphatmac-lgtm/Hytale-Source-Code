#version 330 core

#if USE_VEC3
#define MAX_TYPE vec3
#define MAX_CHANNELS rgb
#else
#define MAX_TYPE float
#define MAX_CHANNELS r
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;

uniform float uHorizontalPass;
uniform vec2 uPixelSize;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out MAX_TYPE outMaxcolor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    const int loopMin = -KERNEL_SIZE;
    const int loopMax = KERNEL_SIZE;

    MAX_TYPE maxColor = MAX_TYPE(0.0f);
    vec2 offset = vec2(0);
    for(int i = loopMin; i<=loopMax; i++)
    {
        offset = vec2( uPixelSize.x * i * uHorizontalPass,
                       uPixelSize.y * i * (1 - uHorizontalPass));

        MAX_TYPE color = texture(uColorTexture, fragTexCoords + offset).MAX_CHANNELS;
        maxColor = max(maxColor,color);
    }
    outMaxcolor = (maxColor);
}
