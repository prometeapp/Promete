#version 330 core
in vec2 fUv;
in vec4 fTintColor;

uniform sampler2D uTexture0;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture0, fUv) * fTintColor;
}
