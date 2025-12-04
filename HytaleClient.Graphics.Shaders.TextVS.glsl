#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uMVPMatrix;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;
in uint vertFillColor;
in uint vertOutlineColor;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec2 fragTexCoords;
out vec4 fillColor;
out vec4 outlineColor;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);

    fragTexCoords = vertTexCoords;

    fillColor = vec4(
        float(vertFillColor & uint(0xff)) / 255.0,
        float((vertFillColor >> 8) & uint(0xff)) / 255.0,
        float((vertFillColor >> 16) & uint(0xff)) / 255.0,
        float((vertFillColor >> 24) & uint(0xff)) / 255.0);

    outlineColor = vec4(
        float(vertOutlineColor & uint(0xff)) / 255.0,
        float((vertOutlineColor >> 8) & uint(0xff)) / 255.0,
        float((vertOutlineColor >> 16) & uint(0xff)) / 255.0,
        float((vertOutlineColor >> 24) & uint(0xff)) / 255.0);
}
