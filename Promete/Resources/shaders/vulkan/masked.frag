#version 450
layout(location = 0) in vec2 fUv;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
};

layout(set = 0, binding = 0) uniform sampler2D uContent; // 子要素のテクスチャ
layout(set = 1, binding = 0) uniform sampler2D uMask;    // マスクテクスチャ

layout(location = 0) out vec4 FragColor;

void main()
{
    vec4 content = texture(uContent, fUv);
    vec4 mask = texture(uMask, fUv);

    // マスクのRGB値の平均（濃淡）でコンテンツのアルファを調整
    float gray = (mask.r + mask.g + mask.b) / 3.0;

    FragColor = content * uTintColor;
    FragColor.a *= gray;
}
