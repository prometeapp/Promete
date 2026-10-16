#version 450
layout(location = 0) in vec3 fUvq;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
};

layout(set = 0, binding = 0) uniform sampler2D uTexture0;

layout(location = 0) out vec4 FragColor;

void main()
{
    // 射影補間: 線形補間された (u * q, v * q, q) を q で割って UV に戻す
    FragColor = texture(uTexture0, fUvq.xy / fUvq.z) * uTintColor;
}
