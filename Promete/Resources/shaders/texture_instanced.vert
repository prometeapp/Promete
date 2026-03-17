#version 330 core
layout(location = 0) in vec2 vPos;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec4 iModel0;
layout(location = 3) in vec4 iModel1;
layout(location = 4) in vec4 iModel2;
layout(location = 5) in vec4 iModel3;
layout(location = 6) in vec4 iTintColor;

out vec2 fUv;
out vec4 fTintColor;

uniform mat4 uProjection;

void main()
{
    mat4 model = mat4(iModel0, iModel1, iModel2, iModel3);
    gl_Position = uProjection * model * vec4(vPos, 0.0, 1.0);
    fUv = vUv;
    fTintColor = iTintColor;
}
