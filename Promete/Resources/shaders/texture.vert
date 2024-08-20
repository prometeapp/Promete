#version 330 core
layout (location = 0) in vec2 vPos;
layout (location = 1) in vec2 vUv;

out vec2 fUv;

uniform mat4 uModel;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uModel * vec4(vPos.x, vPos.y, 0.0, 1.0);
    fUv = vUv;
}
