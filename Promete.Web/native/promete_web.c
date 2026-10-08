#include <emscripten/html5.h>

extern void *emscripten_GetProcAddress(const char *name);

// canvas セレクタ (例: "#canvas") に WebGL2 コンテキストを作って current にする。失敗時は 0 以下
int promete_web_create_context(const char *selector)
{
    EmscriptenWebGLContextAttributes attrs;
    emscripten_webgl_init_context_attributes(&attrs);
    attrs.majorVersion = 2;
    attrs.minorVersion = 0;
    attrs.stencil = 1;
    attrs.alpha = 0;

    EMSCRIPTEN_WEBGL_CONTEXT_HANDLE ctx = emscripten_webgl_create_context(selector, &attrs);
    if (ctx <= 0)
        return (int)ctx;

    emscripten_webgl_make_context_current(ctx);
    return (int)ctx;
}

// Silk.NET の INativeContext.GetProcAddress から呼ぶ
void *promete_web_get_proc(const char *name)
{
    return emscripten_GetProcAddress(name);
}
