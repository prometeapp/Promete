#version 450
layout(location = 0) in vec2 fUv;

layout(set = 0, binding = 0) uniform sampler2D uScreenTexture;

layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = texture(uScreenTexture, fUv);
}
