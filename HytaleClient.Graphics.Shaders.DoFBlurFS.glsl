#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uSceneColorTexture;
uniform sampler2D uSceneColorTexture2;

uniform float uHorizontalPass;
uniform vec2 uPixelSize;
uniform float uNearBlurScale;
uniform float uFarBlurScale;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
layout(location = 0) out vec4 outNearField;
layout(location = 1) out vec4 outFarField;

//-------------------------------------------------------------------------------------------------------------------------

// This is a 9x9 gaussian blur, using separate horizontal & vertical passes.
// Using separate passes allow for 9+9 taps instead of 9x9, with exact same result.
// Then, we can reduce the number of taps for each pass from 9 to 5, 
// by leveraging the hardware texture filtering, and selecting smartly taps (and weight) right between 2 texels, 
// and the hardware will do part of the blur for us.
const float offset[3] = float[](0.0, 1.3846153846, 3.2307692308);
const float weight[3] = float[](0.2270270270, 0.3162162162, 0.0702702703);

void main(void)
{
    vec2 uv = fragTexCoords;

    outNearField = texture(uSceneColorTexture, uv) * weight[0];
    for (int i=1; i<3; i++)
    {
        vec2 voffset = vec2(uPixelSize.x *offset[i] * uHorizontalPass,
                            uPixelSize.y * offset[i] * (1 - uHorizontalPass));
        voffset *=  uNearBlurScale;

        outNearField +=
                texture( uSceneColorTexture, (uv + voffset))
                * weight[i];
        outNearField +=
                texture( uSceneColorTexture, (uv - voffset))
                * weight[i];
    }

    outFarField = texture(uSceneColorTexture2, uv) * weight[0];
    for (int i=1; i<3; i++)
    {
        vec2 voffset = vec2(uPixelSize.x *offset[i] * uHorizontalPass,
                            uPixelSize.y * offset[i] * (1 - uHorizontalPass));
        voffset *=  uFarBlurScale;

        outFarField +=
                texture( uSceneColorTexture2, (uv + voffset))
                * weight[i];
        outFarField +=
                texture( uSceneColorTexture2, (uv - voffset))
                * weight[i];
    }
}

