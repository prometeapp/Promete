#version 330 core
in vec2 fUv;

uniform sampler2D uTexture0;
uniform vec4 uTintColor;
uniform float uStartAngle;  // ラジアン
uniform float uEndAngle;    // ラジアン

out vec4 FragColor;

const float PI = 3.14159265359;
const float TWO_PI = 6.28318530718;

void main()
{
    // UV座標を中心原点の座標系に変換（-0.5 ~ 0.5）
    vec2 centered = fUv - vec2(0.5, 0.5);

    // 極座標変換（atan2で角度を計算）
    float angle = atan(centered.y, centered.x);

    // 角度の正規化（-π ~ π を 0 ~ 2π に）
    if (angle < 0.0) {
        angle += TWO_PI;
    }

    // 角度範囲の正規化（0 ~ 2π）
    float startNorm = uStartAngle;
    float endNorm = uEndAngle;
    if (startNorm < 0.0) startNorm += TWO_PI;
    if (endNorm < 0.0) endNorm += TWO_PI;

    // クリッピング判定
    bool inRange = false;
    if (endNorm >= startNorm) {
        // 通常ケース
        inRange = (angle >= startNorm && angle <= endNorm);
    } else {
        // 0度をまたぐケース（例: 270° ~ 90°）
        inRange = (angle >= startNorm || angle <= endNorm);
    }

    // 範囲外は破棄
    if (!inRange) {
        discard;
    }

    // テクスチャサンプリング
    FragColor = texture(uTexture0, fUv) * uTintColor;
}
