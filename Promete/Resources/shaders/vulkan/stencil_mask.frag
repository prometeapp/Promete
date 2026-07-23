#version 450
layout(location = 0) in vec2 fUv;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
};

layout(set = 0, binding = 0) uniform sampler2D uTexture0;

layout(location = 0) out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture0, fUv) * uTintColor;

    // RGB値の平均（濃淡）が0.5以下の場合は破棄（ステンシルバッファに書き込まない）
    float gray = (texColor.r + texColor.g + texColor.b) / 3.0;
    if (gray <= 0.5)
        discard;

    // カラーバッファには書き込まない（パイプラインの colorWriteMask で無効化）
    FragColor = vec4(1.0);
}
