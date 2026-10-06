#version 330 core
in vec2 fUv;

uniform sampler2D uScreenTexture;

out vec4 FragColor;

void main()
{
    FragColor = texture(uScreenTexture, fUv);
}
