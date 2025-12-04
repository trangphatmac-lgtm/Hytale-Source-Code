#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform sampler2D uDepthTexture;

uniform vec3 uCameraPosition;
uniform vec3 uCameraDirection;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out float outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    // We modify the sun occlusion value for pixels "on"/near the sky
    // depending on the camera altitude (and direction ?), by making it darker at low altitude.
    // The goal is to somehow cover huge holes in caves (low altitude) that show us the sky,
    // just because not enough chunks were loaded / rendered (e.g. huge caves).

    float depth = texture(uDepthTexture, fragTexCoords).r;
    float color00 = texture(uColorTexture, fragTexCoords).b;

    // Camera direction not used yet ! not necessary ?
    ////float directionFactor = uCameraDirection.y * 0.5 + 0.5;
    float maxWorldHeight = 320.0f;
    float altitudeFactor = uCameraPosition.y / maxWorldHeight;
    float cameraFactor = altitudeFactor;
    float skyOcclusionGradient = mix((1.0-depth), 1.0, cameraFactor) ;

    // This is disabled because it causes instabilities on mood fog especially visible since we added mood fog on the sky.
//    color00 *= skyOcclusionGradient;

    outColor = color00;
}
