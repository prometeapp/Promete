#version 450
layout(location = 0) in vec2 vPos;

void main()
{
    gl_Position = vec4(vPos.x, vPos.y, 0.0, 1.0);
    gl_PointSize = 1.0;
}
