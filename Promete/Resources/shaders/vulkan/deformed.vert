#version 450
layout(location = 0) in vec2 vPos;
layout(location = 1) in vec3 vUvq; // (u * q, v * q, q)

layout(location = 0) out vec3 fUvq;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
};

void main()
{
    gl_Position = uMvp * vec4(vPos, 0.0, 1.0);
    fUvq = vUvq;
}
