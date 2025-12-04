#version 330 core
#pragma optionNV(strict on)

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uBloomTexture;
uniform float uBloomIntensity;

#if USE_SUNSHAFT
uniform sampler2D uSunshaftTexture;
uniform float uSunshaftIntensity;
#endif //USE_SUNSHAFT

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    vec3 bloom = texture(uBloomTexture, fragTexCoords.xy).rgb * uBloomIntensity;

    #if USE_SUNSHAFT
    vec3 sunshaft = texture(uSunshaftTexture, fragTexCoords.xy).rgb * uSunshaftIntensity;
    #endif //USE_SUNSHAFT

    #if SUN_FB_POW
    outColor.rgb = bloom;
    #endif //SUN_FB_POW

    #if USE_SUNSHAFT
    // combine sun and sunshafts
    sunshaft = clamp(sunshaft - bloom, 0, 1);
    outColor += sunshaft;
    #endif //USE_SUNSHAFT
}

