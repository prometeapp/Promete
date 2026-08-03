using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// レビュー指摘 #05 の再現シーン。
/// VulkanMaskedContainerHelper.RenderToTexture は MaskedContainer の Parent / Location /
/// Angle / Scale を一時的に退避してから子を描画するが、try/finally で保護していない。
/// 子の Collect が例外を投げると復元ブロックへ到達せず、コンテナは親から切り離され
/// 原点・単位スケールのまま残る。
///
/// このシーンは 1 フレームだけ例外を投げる子ノードを仕込み、
/// 例外の前後で MaskedContainer の変換が保たれるかを確認する。
/// </summary>
[Demo("/debug/vulkan_mask_restore", "指摘#05: マスク描画中の例外で変換が復元されない")]
public class VulkanMaskRestoreDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private static readonly Vector InitialLocation = (200, 120);

    private Texture2D _backgroundTexture;
    private Texture2D _maskTexture;
    private MaskedContainer _masked = null!;
    private ThrowingNode _thrower = null!;

    public override void OnStart()
    {
        _backgroundTexture = App.TextureFactory.Load("assets/ichigo2.png");
        _maskTexture = App.TextureFactory.Load("assets/circle_mask.png");

        _masked = new MaskedContainer(_maskTexture, useAlphaMask: true)
            .Location(InitialLocation)
            .Scale(2, 2)
            .Size(32, 32);

        _masked.Add(new Sprite(_backgroundTexture));

        _thrower = new ThrowingNode();
        _masked.Add(_thrower);

        Root.Add(_masked);

        console.Print("指摘#05 再現シーン");
        console.Print("T: 次のフレームで子ノードに例外を投げさせる");
        console.Print("例外後、MaskedContainer の Location / Scale / Parent を検査する");
        PrintState("初期状態");
    }

    public override void OnUpdate()
    {
        if (keyboard.T.IsKeyDown)
        {
            _thrower.ShouldThrow = true;
            console.Print("次フレームで例外を発生させる");
        }

        if (_thrower.HasThrown)
        {
            _thrower.HasThrown = false;
            PrintState("例外発生後");
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    /// <summary>
    /// 退避された変換が復元されているかを表示する。
    /// 例外後に (0,0) / scale=1 / Parent=null になっていれば指摘#05 の再現。
    /// </summary>
    private void PrintState(string label)
    {
        var hasParent = _masked.Parent is not null;
        var restored =
            _masked.Location == InitialLocation && _masked.Scale.X == 2 && hasParent;

        console.Print(
            $"{label}: loc={_masked.Location} scale={_masked.Scale} parent={(hasParent ? "有" : "null")} "
                + (restored ? "-> 復元OK" : "-> 復元されていない (指摘#05)")
        );
    }

    public override void OnDestroy()
    {
        _masked.Destroy();
        _backgroundTexture.Dispose();
        _maskTexture.Dispose();
    }

    /// <summary>
    /// Collect 時に一度だけ例外を投げるノード。
    /// </summary>
    private sealed class ThrowingNode : Node
    {
        public bool ShouldThrow { get; set; }
        public bool HasThrown { get; set; }

        public override void Collect(RenderCommandQueue queue, RenderContext ctx)
        {
            if (!ShouldThrow)
                return;

            ShouldThrow = false;
            HasThrown = true;
            throw new InvalidOperationException("指摘#05 再現用の意図的な例外");
        }
    }
}
