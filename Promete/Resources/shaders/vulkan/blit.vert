#version 450
layout(location = 0) out vec2 fUv;

// フルスクリーントライアングル (頂点バッファ不要)
void main()
{
    vec2 pos = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
    fUv = pos;
    gl_Position = vec4(pos * 2.0 - 1.0, 0.0, 1.0);
}
