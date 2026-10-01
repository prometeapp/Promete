#version 450
layout(push_constant) uniform PushConstants
{
    vec4 uTintColor;
};

layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = uTintColor;
}
