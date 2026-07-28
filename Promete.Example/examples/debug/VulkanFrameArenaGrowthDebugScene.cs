using System.Drawing;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// レビュー指摘 #02 の再現シーン。
/// VulkanFrameArena は全バッファを永続マップするが、容量超過で退役したバッファを
/// Reset()/Dispose() で破棄する際に UnmapMemory を呼ばない。
/// マップ済みメモリの解放は未定義動作であり、バリデーションレイヤ有効時に検出される。
///
/// 1 フレームあたりのプッシュ量が初期容量 256 KB を超えるとアリーナが再確保され、
/// 旧バッファが退役リストへ入る。矩形 1 個あたり頂点 32 B + インデックス 24 B、
/// 16 B アライン込みで概ね 64 B 消費するため、4,096 個前後が境界となる。
/// </summary>
[Demo("/debug/vulkan_frame_arena_growth", "指摘#02: アリーナ再確保でマップ済みメモリを解放")]
public class VulkanFrameArenaGrowthDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    /// <summary>初期容量 256 KB を超えない個数。</summary>
    private const int BelowThreshold = 2000;

    /// <summary>初期容量を明確に超え、毎フレーム退役バッファを生む個数。</summary>
    private const int AboveThreshold = 8000;

    private readonly Container _container = new();

    private int _shapeCount = BelowThreshold;

    public override void OnStart()
    {
        Root.Add(_container);
        Rebuild();

        console.Print("指摘#02 再現シーン");
        console.Print("バリデーションレイヤを有効にして実行すること");
        console.Print("SPACE: 図形数を 2000 <-> 8000 で切り替え");
        console.Print("8000 側でアリーナが 256KB を超えて再確保され、退役バッファが発生する");
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyDown)
        {
            _shapeCount = _shapeCount == BelowThreshold ? AboveThreshold : BelowThreshold;
            Rebuild();
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    /// <summary>
    /// 指定個数の矩形を敷き詰め、1 フレームのプッシュ量を変化させる。
    /// </summary>
    private void Rebuild()
    {
        _container.Clear();

        const int columns = 100;
        for (var i = 0; i < _shapeCount; i++)
        {
            var x = i % columns * 6;
            var y = i / columns * 6;
            var color = i % 2 == 0 ? Color.SteelBlue : Color.LightSalmon;
            _container.Add(Shape.CreateRect(x, y, x + 4, y + 4, color));
        }

        var estimatedBytes = _shapeCount * 64;
        console.Print(
            $"図形数={_shapeCount} 推定プッシュ量={estimatedBytes / 1024}KB "
                + (estimatedBytes > 256 * 1024 ? "(初期容量超過 → 再確保あり)" : "(容量内)")
        );
    }

    public override void OnDestroy()
    {
        _container.Destroy();
    }
}
