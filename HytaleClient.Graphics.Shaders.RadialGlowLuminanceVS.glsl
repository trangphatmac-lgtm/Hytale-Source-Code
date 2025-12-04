#version 330 core

#define NSAMPLES_2 NB_SAMPLES       // number of samples divided by 2
#define NSAMPLES (NSAMPLES_2 * 2)   // real number of samples

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uMVPMatrix;
uniform mat4 uSunMVPMatrix;
uniform float uScaleFactor;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 fragTexCoords[NSAMPLES_2];

//-------------------------------------------------------------------------------------------------------------------------

// scale texcoords
vec2 NTexcoord(vec2 texcoords, int iIdx, vec3 c)
{
    float scale = sqrt(iIdx) * c.z + 1.0;
    return (texcoords.xy - c.xy) * scale + c.xy;
}

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    const vec4 sunVertPosition = vec4(0.5,0.5,0,1);

    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);

    // transform sun position to screen space [-1;1]
    vec4 sunPos = uSunMVPMatrix * sunVertPosition;
    sunPos = sunPos * 2.0f - 1.0f;

    // determine sun position in screen space and texcoord scale factor
    vec3 ts = vec3((sunPos.x / sunPos.w) * 0.5f + 0.5f,
                   (sunPos.y / sunPos.w) * 0.5f + 0.5f,
                   uScaleFactor / NSAMPLES );

    // calculate n = N_SAMPLES different mappings for sun radial glow
    int j = 0;
    for( int i = 0; i<NSAMPLES_2; i++)
    {
        fragTexCoords[i].xy = NTexcoord(vertTexCoords.xy, j, ts);
        j++;
        fragTexCoords[i].zw = NTexcoord(vertTexCoords.xy, j, ts);
        j++;
    }
}
