using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// レビュー指摘 #03 の再現シーン。
///
/// 発生源は RenderCommandQueue.PushTrim (RenderCommandQueue.cs:97-100)。
/// トリム矩形の原点が負のとき 0 にクランプするが、クランプした分を size から
/// 減算しないため、可視領域が「はみ出した量」だけ広いまま残る。
/// GL / Vulkan 双方のランナーが同じ矩形を受け取る、両バックエンド共通の不具合。
///
/// 重要: トリムコンテナ単体では、この不具合は目に見えない。
/// クリップ矩形はコンテナ自身の位置とサイズから作られるため、過大になった帯は
/// 必ずそのコンテナの中身より右側、つまり何も描かれていない領域に落ちるためである。
///
/// 可視化にはトリムコンテナの入れ子が要る。PushTrim は親トリムとの積集合を取る
/// (RenderCommandQueue.cs:115-123) ので、外側の過大な幅が内側の積集合に漏れる。
/// 外側を左へはみ出させて幅を過大にし、内側は画面内に置いて中身を持たせると、
/// 内側が本来の右端を越えて描画される。
///
/// 外側 loc=(-200,140) size=400x150 / 内側 loc=画面(50,140) size=300x150 のとき:
///   正しい内側クリップ = x 50..200 (幅150)
///   実装の内側クリップ = x 50..350 (幅300)  -> 150px ぶん余分に見える
/// </summary>
[Demo("/debug/trim_clamp", "指摘#03: 入れ子トリムで内側のクリップ幅が過大になる")]
public class TrimClampDebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private const int TrimTop = 140;
    private const int TrimHeight = 150;

    private const int OuterWidth = 400;
    private const int InnerWidth = 300;

    /// <summary>内側コンテナの画面上の左端。外側の可視領域内に置く。</summary>
    private const int InnerScreenX = 50;

    /// <summary>
    /// 外側コンテナ。自身は中身を持たず、左へはみ出させてクリップ幅を過大にする役に徹する。
    /// </summary>
    private readonly Container _outer = new Container().Size(OuterWidth, TrimHeight);

    /// <summary>内側コンテナ。中身を全域に持ち、外側の過大な幅の影響を受ける。</summary>
    private readonly Container _inner = new Container().Size(InnerWidth, TrimHeight);

    private Container _guide = null!;
    private int _outerX = -200;

    public override void OnStart()
    {
        _outer.IsTrimmable = true;
        _inner.IsTrimmable = true;

        // 内側の中身。全域に敷き詰めることで、過大な帯に「切り取られるべき中身」を置く
        for (var x = 0; x < InnerWidth; x += 20)
        {
            var isEven = x / 20 % 2 == 0;
            _inner.Add(
                Shape.CreateRect(
                    x,
                    0,
                    x + 19,
                    TrimHeight - 1,
                    isEven ? Color.MediumSeaGreen : Color.SeaGreen
                )
            );
        }

        // 外側自身は中身を持たない。トリム枠としてのみ機能させる
        _outer.Add(_inner);
        Root.Add(_outer);

        _guide = new Container();
        Root.Add(_guide);

        SetOuterX(-200);

        console.Print("指摘#03 再現シーン (入れ子トリム)");
        console.Print("左右キー: 外側コンテナを移動 / R: -200 へ戻す / 0: 0 へ");
    }

    public override void OnUpdate()
    {
        if (keyboard.Left)
            SetOuterX(_outerX - 4);

        if (keyboard.Right)
            SetOuterX(_outerX + 4);

        if (keyboard.R.IsKeyDown)
            SetOuterX(-200);

        if (keyboard.Number0.IsKeyDown)
            SetOuterX(0);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    /// <summary>
    /// 外側の位置を更新する。内側は画面上で固定に見えるよう、外側ローカル座標を補正する。
    /// </summary>
    private void SetOuterX(int outerX)
    {
        _outerX = Math.Clamp(outerX, -OuterWidth, 0);

        _outer.Location = (_outerX, TrimTop);

        // 内側は常に画面 x = InnerScreenX に来るようにする
        _inner.Location = (InnerScreenX - _outerX, 0);

        UpdateGuide();
        PrintState();
    }

    /// <summary>
    /// 内側の期待クリップ右端 (赤) と、実装が使う右端 (黄) を引く。
    /// </summary>
    private void UpdateGuide()
    {
        var (expectedRight, actualRight) = ComputeInnerRight();

        var top = TrimTop - 20;
        var bottom = TrimTop + TrimHeight + 20;

        _guide.Clear();
        _guide.Add(Shape.CreateLine(expectedRight, top, expectedRight, bottom, Color.Red));

        if (actualRight != expectedRight)
            _guide.Add(Shape.CreateLine(actualRight, top, actualRight, bottom, Color.Yellow));
    }

    /// <summary>
    /// 内側トリムの右端を、期待値と実装値の双方について求める。
    /// PushTrim と同じく、親トリムとの積集合を左右両端について取る。
    /// </summary>
    private (int Expected, int Actual) ComputeInnerRight()
    {
        // 外側: 原点を 0 にクランプする際、実装は幅を縮めない
        var outerClampedX = Math.Max(0, _outerX);
        var outerExpectedWidth = _outerX < 0 ? OuterWidth + _outerX : OuterWidth;

        var actual = IntersectRight(outerClampedX, outerClampedX + OuterWidth);
        var expected = IntersectRight(outerClampedX, outerClampedX + outerExpectedWidth);

        return (expected, actual);
    }

    /// <summary>
    /// 内側の矩形を親トリム [parentLeft, parentRight] と積集合し、その右端を返す。
    /// 幅が負にならないよう左端でクランプする。
    /// </summary>
    private static int IntersectRight(int parentLeft, int parentRight)
    {
        var left = Math.Max(InnerScreenX, parentLeft);
        var right = Math.Min(InnerScreenX + InnerWidth, parentRight);
        return Math.Max(left, right);
    }

    private void PrintState()
    {
        var (expectedRight, actualRight) = ComputeInnerRight();
        var over = actualRight - expectedRight;

        console.Clear();
        console.Print("指摘#03 再現シーン (入れ子トリム)");
        console.Print("左右キー: 外側を移動 / R: -200 / 0: 0");
        console.Print("赤線 = 内側の期待クリップ右端 / 黄線 = 実装が使う右端");
        console.Print("");
        console.Print($"外側 loc.X = {_outerX} (size {OuterWidth}x{TrimHeight})");
        console.Print($"内側 画面X = {InnerScreenX} (size {InnerWidth}x{TrimHeight})");
        console.Print($"内側クリップ右端  期待 = {expectedRight} / 実装 = {actualRight}");
        console.Print(
            over > 0
                ? $"緑の帯が赤線を {over}px 越えていれば再現"
                : "過大なし (外側がはみ出していないため一致)"
        );
    }

    public override void OnDestroy()
    {
        _guide.Destroy();
        _outer.Destroy();
    }
}
