#version 330 core
layout (location = 0) in vec2 vPos;
layout (location = 1) in vec3 vUvq; // (u * q, v * q, q)

out vec3 fUvq;

uniform mat4 uModel;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uModel * vec4(vPos.x, vPos.y, 0.0, 1.0);
    fUvq = vUvq;
}
