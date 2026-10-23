using System;
using System.Drawing;
using Promete.Assets;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("graphics/deformedSprite.demo", "DeformedSpriteの例（頂点のランダム移動）")]
public class DeformedSpriteDemo(ConsoleLayer console, Keyboard keyboard, AssetManager assets)
    : Scene
{
    private const float QuadSize = 128;

    private readonly Container _rotatingContainer = new();

    private Texture2D _texture;
    private Texture2D[] _cells = [];
    private Graphics.FrameBuffer? _frameBuffer;

    private Wobble _rootWobble = null!;
    private Wobble _containerWobble = null!;
    private Wobble? _frameBufferWobble;
    private Text _info = null!;

    public override void OnStart()
    {
        console.Print("Press [ESC] to return");

        var font = Graphics.Fonts.Font.GetDefault(16);
        _texture = assets.LoadTexture("assets/ichigo.png", AssetScope.Scene);
        _cells = assets.LoadSpriteSheet("assets/icons.png", 3, 1, (32, 32), AssetScope.Scene);

        var top = (View.Height / 2f) - (QuadSize / 2);
        var columnWidth = View.Width / 3f;

        // 1. ルート直下。頂点の原点が左上にあるので、ピボットは (0, 0) 付近になる
        var rootSprite = new DeformedSprite(_texture).Location(
            (columnWidth / 2) - (QuadSize / 2),
            top
        );
        _rootWobble = new Wobble(rootSprite, (0, 0));
        _info = new Text("", font, Color.White).Location(8, View.Height - 64);
        Root.AddRange(
            new Text("Root", font, Color.Lime).Location(rootSprite.Location.X, top - 56),
            rootSprite,
            _info
        );

        // 2. 回転・拡大するコンテナの子。頂点の原点を中心に置くので、ピボットは (0.5, 0.5) 付近になる
        var containerSprite = new DeformedSprite(_texture);
        _containerWobble = new Wobble(containerSprite, (-QuadSize / 2, -QuadSize / 2));
        _rotatingContainer.Location(columnWidth * 1.5f, top + (QuadSize / 2)).Scale(0.8f, 0.8f);
        _rotatingContainer.Add(containerSprite);
        Root.AddRange(
            new Text("Container", font, Color.Lime).Location(
                (columnWidth * 1.5f) - (QuadSize / 2),
                top - 56
            ),
            _rotatingContainer
        );

        // 3. FrameBuffer の子。描画結果を Sprite で表示する
        var frameBufferLabel = new Text("FrameBuffer", font, Color.Lime).Location(
            (columnWidth * 2.5f) - (QuadSize / 2),
            top - 56
        );
        Root.Add(frameBufferLabel);
        if (!App.IsFrameBufferSupported)
        {
            frameBufferLabel.Content = "FrameBuffer 非対応";
            return;
        }

        _frameBuffer = new Graphics.FrameBuffer(160, 160) { BackgroundColor = Color.DarkSlateGray };
        var frameBufferSprite = new DeformedSprite(_cells[1]).Location(16, 16);
        _frameBufferWobble = new Wobble(frameBufferSprite, (0, 0));
        _frameBuffer.Add(frameBufferSprite);
        Root.Add(
            new Sprite(_frameBuffer.Texture).Location(
                (columnWidth * 2.5f) - 80,
                top + (QuadSize / 2) - 80
            )
        );
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp)
        {
            App.LoadScene<MainScene>();
            return;
        }

        var dt = Time.DeltaTime;
        _rootWobble.Update(dt);
        _containerWobble.Update(dt);
        _frameBufferWobble?.Update(dt);
        _rotatingContainer.Angle += (30 * dt).Degrees;

        var root = _rootWobble.Sprite;
        _info.Content =
            $"Root: Size=({root.Size.X}, {root.Size.Y}) Pivot=({root.Pivot.X:0.00}, {root.Pivot.Y:0.00})\n"
            + $"Container: Size=({_containerWobble.Sprite.Size.X}, {_containerWobble.Sprite.Size.Y}) "
            + $"Pivot=({_containerWobble.Sprite.Pivot.X:0.00}, {_containerWobble.Sprite.Pivot.Y:0.00})";
    }

    public override void OnDestroy()
    {
        _frameBuffer?.Dispose();
    }

    /// <summary>
    /// 4つの頂点を、正方形の角からランダムな目標位置へ滑らかに動かし続けます。
    /// </summary>
    private sealed class Wobble
    {
        private const float Range = 40;
        private const float Interval = 1.2f;

        private readonly Vector[] _corners;
        private readonly Vector[] _from = new Vector[4];
        private readonly Vector[] _to = new Vector[4];
        private float _time;

        public Wobble(DeformedSprite sprite, Vector origin)
        {
            Sprite = sprite;
            _corners =
            [
                origin,
                origin + (QuadSize, 0),
                origin + (0, QuadSize),
                origin + (QuadSize, QuadSize),
            ];
            for (var i = 0; i < 4; i++)
                _to[i] = _corners[i];
            NextTarget();
            Apply(0);
        }

        public DeformedSprite Sprite { get; }

        private static Vector Lerp(Vector a, Vector b, float t) => a + ((b - a) * t);

        public void Update(float dt)
        {
            _time += dt;
            if (_time >= Interval)
            {
                _time -= Interval;
                NextTarget();
            }

            Apply(_time / Interval);
        }

        private void NextTarget()
        {
            for (var i = 0; i < 4; i++)
            {
                _from[i] = _to[i];
                _to[i] =
                    _corners[i]
                    + (
                        (Random.Shared.NextSingle() - 0.5f) * Range * 2,
                        (Random.Shared.NextSingle() - 0.5f) * Range * 2
                    );
            }
        }

        private void Apply(float t)
        {
            // smoothstep で目標位置へ近づける
            var e = t * t * (3 - (2 * t));
            Sprite.SetVertices(
                Lerp(_from[0], _to[0], e),
                Lerp(_from[1], _to[1], e),
                Lerp(_from[2], _to[2], e),
                Lerp(_from[3], _to[3], e)
            );
        }
    }
}
