#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uSceneColorTexture;
uniform sampler2D uGlowMaskTexture;
uniform sampler2D uDepthTexture;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec4 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout (location = 0) out vec3 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    float depth = texture(uDepthTexture, fragTexCoords.xy).r;
    vec3 sceneColor = texture(uSceneColorTexture, fragTexCoords.xy).rgb;
    vec3 mask = texture(uGlowMaskTexture, fragTexCoords.zw).rgb;

    float linearDepth = depth * 1024; 

    // compute pixel luminance
    float luminance = dot(sceneColor.xyz, vec3(1.0f/3.0f));

    // adjust lighting constrast
    sceneColor *= luminance;

    // we test the depth to avoid having colored sun shafts when we hold a torch in first person for example -> the torch needs to be black
    vec3 result = (linearDepth > 3) ? sceneColor * mask : vec3(0);    

    outColor = result;
}
