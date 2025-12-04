#version 330 core

#define NSAMPLES_2 NB_SAMPLES       // number of samples divided by 2
#define NSAMPLES (NSAMPLES_2 * 2)   // real number of samples
#define STEP (1.0f / NSAMPLES)      // radial glow step

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uGlowMaskTexture;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec4 fragTexCoords[NSAMPLES_2];

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec3 col = vec3(0);
    float lum = 0.8f;

    //sample radial glow buffer with different mappings and decreasing luminance
    for(int i = 0; i<NSAMPLES_2; i++)
    {
        col += texture(uGlowMaskTexture, fragTexCoords[i].xy).rgb * lum;
        lum -= STEP;
        col += texture(uGlowMaskTexture, fragTexCoords[i].zw).rgb * lum;
        lum -= STEP;
    }
    outColor = col * 0.25f;
}
