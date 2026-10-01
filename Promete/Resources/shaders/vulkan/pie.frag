#version 450
layout(location = 0) in vec2 fUv;

layout(push_constant) uniform PushConstants
{
    mat4 uMvp;
    vec4 uTintColor;
    vec2 uAngles; // x = 開始角, y = 終了角 (ラジアン)
};

layout(set = 0, binding = 0) uniform sampler2D uTexture0;

layout(location = 0) out vec4 FragColor;

const float TWO_PI = 6.28318530718;

void main()
{
    // UV座標を中心原点の座標系に変換（-0.5 ~ 0.5）
    vec2 centered = fUv - vec2(0.5, 0.5);

    // 極座標変換（atan2で角度を計算）し、0 ~ 2π に正規化
    float angle = atan(centered.y, centered.x);
    if (angle < 0.0) {
        angle += TWO_PI;
    }

    float startNorm = uAngles.x;
    float endNorm = uAngles.y;
    if (startNorm < 0.0) startNorm += TWO_PI;
    if (endNorm < 0.0) endNorm += TWO_PI;

    // クリッピング判定（0度をまたぐケースに対応）
    bool inRange = endNorm >= startNorm
        ? (angle >= startNorm && angle <= endNorm)
        : (angle >= startNorm || angle <= endNorm);

    if (!inRange) {
        discard;
    }

    FragColor = texture(uTexture0, fUv) * uTintColor;
}
