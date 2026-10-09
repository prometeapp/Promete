#version 450
layout(location = 0) in vec2 vPos;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec2 fUv;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
    vec2 uAngles; // x = 開始角, y = 終了角 (ラジアン)
    vec2 uUvStart; // テクスチャ内のUV開始位置 (スプライトシート対応)
    vec2 uUvEnd;   // テクスチャ内のUV終了位置
};

void main()
{
    gl_Position = uMvp * vec4(vPos, 0.0, 1.0);
    fUv = vUv;
}
