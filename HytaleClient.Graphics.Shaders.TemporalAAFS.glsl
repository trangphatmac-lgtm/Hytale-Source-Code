#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uColorTexture;
uniform sampler2D uPreviousColorTexture;

uniform vec2 uPixelSize;
uniform int uNeighborHoodCheck;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec2 reprojectedTexCoords = fragTexCoords;

    vec4 n11 = texture(uColorTexture, fragTexCoords + vec2(0, 0));
    vec4 previousColor = texture(uPreviousColorTexture, reprojectedTexCoords);

    float reprojectionBlend = 0.5;

    if (1 == uNeighborHoodCheck)
    {
        vec4 n00 = texture(uColorTexture, fragTexCoords + vec2(-uPixelSize.x, -uPixelSize.y));
        vec4 n01 = texture(uColorTexture, fragTexCoords + vec2(-uPixelSize.x, 0));
        vec4 n02 = texture(uColorTexture, fragTexCoords + vec2(-uPixelSize.x, uPixelSize.y));
        vec4 n10 = texture(uColorTexture, fragTexCoords + vec2(0, -uPixelSize.y));
        vec4 n12 = texture(uColorTexture, fragTexCoords + vec2(0, uPixelSize.y));
        vec4 n20 = texture(uColorTexture, fragTexCoords + vec2(uPixelSize.x, -uPixelSize.y));
        vec4 n21 = texture(uColorTexture, fragTexCoords + vec2(uPixelSize.x, 0));
        vec4 n22 = texture(uColorTexture, fragTexCoords + vec2(uPixelSize.x, uPixelSize.y));

        vec4 nMin = min(n00,min(n01,min(n02,min(n10,min(n11,min(n12,min(n20,min(n21, n22))))))));
        vec4 nMax = max(n00,max(n01,max(n02,max(n10,max(n11,max(n12,max(n20,max(n21, n22))))))));

        previousColor = clamp(previousColor, nMin, nMax);
    }

    // Blend
    outColor = mix(n11, previousColor, reprojectionBlend);
}
