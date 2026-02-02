#version 330 core
in vec2 fUv;

uniform sampler2D uContent;  // 子要素のテクスチャ
uniform sampler2D uMask;     // マスクテクスチャ
uniform vec4 uTintColor;

out vec4 FragColor;

void main()
{
    vec4 content = texture(uContent, fUv);
    vec4 mask = texture(uMask, fUv);

    // マスクのアルファ値でコンテンツのアルファを調整
    FragColor = content * uTintColor;
    FragColor.a *= mask.a;
}
