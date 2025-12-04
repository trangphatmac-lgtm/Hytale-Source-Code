#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uMVPMatrix;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec2 vertPosition;
in vec2 vertTexCoords;
in vec4 vertScissor;
in vec4 vertMaskTextureArea;
in vec4 vertMaskBounds;
in vec4 vertFillColor;
in vec4 vertOutlineColor;
in vec4 vertSDFSettings;
in uint vertFontId;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec2 fragTexCoords;
flat out vec4 fragScissor;
flat out vec4 fragMaskTextureArea;
flat out vec4 fragMaskBounds;
out vec4 fragFillColor;
out vec4 fragOutlineColor;
out vec4 fragSDFSettings;
flat out uint fragFontId;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    gl_Position = uMVPMatrix * vec4(vertPosition, 0.0, 1.0);

    fragTexCoords = vertTexCoords;
    fragScissor = vertScissor;
    fragMaskTextureArea = vertMaskTextureArea;
    fragMaskBounds = vertMaskBounds;
    fragFillColor = vertFillColor;
    fragOutlineColor = vertOutlineColor;
	fragSDFSettings = vertSDFSettings;
    fragFontId = vertFontId;
}
