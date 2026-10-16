#version 330 core
in vec3 fUvq;

uniform sampler2D uTexture0;
uniform vec4 uTintColor;

out vec4 FragColor;

void main()
{
    // 射影補間: 線形補間された (u * q, v * q, q) を q で割って UV に戻す
    FragColor = texture(uTexture0, fUvq.xy / fUvq.z) * uTintColor;
}
