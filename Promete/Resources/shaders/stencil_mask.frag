#version 330 core
in vec2 fUv;

uniform sampler2D uTexture0;
uniform vec4 uTintColor;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture0, fUv) * uTintColor;

    // RGB値の平均（濃淡）を計算
    float gray = (texColor.r + texColor.g + texColor.b) / 3.0;

    // 濃淡が0.5以下の場合は破棄（ステンシルバッファに書き込まない）
    if (gray <= 0.5)
        discard;

    // カラーバッファには書き込まないが、ステンシルバッファには書き込む
    FragColor = vec4(1.0, 1.0, 1.0, 1.0);
}
