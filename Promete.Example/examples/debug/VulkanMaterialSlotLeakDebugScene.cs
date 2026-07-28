using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// レビュー指摘 #07 の再現シーン。
/// VulkanMaterialSystem の _slots は (Material, フレームスロット) をキーに
/// UBO とディスクリプタセットを確保するが、Material がスコープを抜けても
/// エントリが除去されない。毎フレーム新しい Material を生成すると
/// プール上限 MaxSets = 1024 を消費し尽くし、AllocateDescriptorSets が
/// ErrorOutOfPoolMemory を投げる。
///
/// 1 フレームあたり 2 スロット (FramesInFlight = 2) 消費するため、
/// 60fps では概ね 8.5 秒でクラッシュに到達する。
/// </summary>
[Demo("/debug/vulkan_material_slot_leak", "指摘#07: Materialスロットが解放されずプール枯渇")]
public class VulkanMaterialSlotLeakDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private const string VertSrc = """
        #version 330 core
        layout(location = 0) in vec2 vPos;
        layout(location = 1) in vec2 vUv;
        layout(location = 2) in vec4 iModel0;
        layout(location = 3) in vec4 iModel1;
        layout(location = 4) in vec4 iModel2;
        layout(location = 5) in vec4 iModel3;
        layout(location = 6) in vec4 iTintColor;
        layout(location = 7) in vec4 iUvRect;

        out vec2 fUv;
        out vec4 fTintColor;

        uniform mat4 uProjection;

        void main()
        {
            mat4 model = mat4(iModel0, iModel1, iModel2, iModel3);
            gl_Position = uProjection * model * vec4(vPos, 0.0, 1.0);
            fUv = mix(iUvRect.xy, iUvRect.zw, vUv);
            fTintColor = iTintColor;
        }
        """;

    // uTime を持つだけの最小フラグメントシェーダー
    private const string FragSrc = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        uniform float uTime;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            FragColor = vec4(c.rgb * (0.5 + 0.5 * sin(uTime)), c.a);
        }
        """;

    private Texture2D _texture;
    private ShaderProgram _shader = null!;
    private Sprite _sprite = null!;

    /// <summary>再利用する Material。リーク再現時は使用しない。</summary>
    private Material _sharedMaterial = null!;

    private bool _allocatePerFrame = true;
    private int _frames;

    public override void OnStart()
    {
        _texture = App.TextureFactory.Load("assets/ichigo2.png");
        _shader = ShaderProgram.Create().Vertex(VertSrc).Fragment(FragSrc).Compile();
        _sharedMaterial = new Material(_shader) { ["uTime"] = 0f };

        _sprite = new Sprite(_texture).Location(280, 180).Scale(4, 4);
        Root.Add(_sprite);

        console.Print("指摘#07 再現シーン");
        console.Print("毎フレーム新しい Material を生成し、スロットを枯渇させる");
        console.Print("SPACE: 毎フレーム生成 / 使い回し を切り替え");
        console.Print("毎フレーム生成のまま放置すると数秒で ErrorOutOfPoolMemory で落ちる");
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyDown)
        {
            _allocatePerFrame = !_allocatePerFrame;
            _frames = 0;
            console.Print(
                _allocatePerFrame
                    ? "毎フレーム Material を生成 (リーク再現)"
                    : "Material を使い回し (正常動作)"
            );
        }

        var time = App.Time.TotalTime;

        if (_allocatePerFrame)
        {
            // 毎フレーム新しいインスタンスを作るため、_slots のキーが毎回変わる
            _sprite.Material = new Material(_shader) { ["uTime"] = time };

            _frames++;
            if (_frames % 60 == 0)
                console.Print($"経過フレーム={_frames} 推定消費スロット={_frames * 2} / 1024");
        }
        else
        {
            _sharedMaterial["uTime"] = time;
            _sprite.Material = _sharedMaterial;
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _sprite.Destroy();
        _texture.Dispose();
    }
}
