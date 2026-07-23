#version 450
layout(location = 0) in vec2 fUv;
layout(location = 1) in vec4 fTintColor;

layout(set = 0, binding = 0) uniform sampler2D uTexture0;

layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture0, fUv) * fTintColor;
}
