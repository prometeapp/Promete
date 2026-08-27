#version 330 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aUv;

uniform mat4 uModel;
uniform mat4 uProjection;
uniform vec2 uUvStart;
uniform vec2 uUvEnd;

out vec2 vUv;

void main()
{
    gl_Position = uProjection * uModel * vec4(aPosition, 0.0, 1.0);
    vUv = mix(uUvStart, uUvEnd, aUv);
}
