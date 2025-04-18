using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/frameBuffer.demo", "FrameBufferのテスト")]
public class frameBuffer(Mouse mouse, Keyboard keyboard, ConsoleLayer console) : Scene
{
    private FrameBuffer? _frameBuffer;
    private bool _isSupported;

    private readonly Sprite _editorSprite = new();

    private readonly Sprite _previewSprite = new Sprite()
        .Location(200, 200);

    private VectorInt _previousPosition = VectorInt.Zero;

    public override void OnStart()
    {
        console.Clear();
        _isSupported = App.IsFrameBufferSupported;
        if (!_isSupported)
        {
            console.Print("FrameBufferはこのバックエンドではサポートされていません");
            console.Print("Press [ESC] to exit");
            return;
        }

        _frameBuffer = new FrameBuffer(128, 128);
        _frameBuffer.BackgroundColor = Color.White;
        _editorSprite.Texture = _frameBuffer.Texture;
        _previewSprite.Texture = _frameBuffer.Texture;
        _previewSprite.Scale *= 4;

        _frameBuffer.Add(new Text("Hello", null, Color.Black));

        Root.Add(_editorSprite);
        Root.Add(_previewSprite);
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
            return;
        }

        if (!_isSupported) return;
        _previewSprite.Angle = (_previewSprite.Angle + 45 * Window.DeltaTime) % 360;

        // 左ボタンを推している間、_frameBufferに線を描画する
        if (mouse[MouseButtonType.Left])
        {
            var currentPosition = mouse.Position;
            if (_previousPosition == VectorInt.Zero && _previousPosition != currentPosition)
            {
                _previousPosition = currentPosition;
                return;
            }

            _frameBuffer?.Add(Shape.CreateLine(_previousPosition, currentPosition, Color.Black));
            _previousPosition = currentPosition;
            return;
        }

        _previousPosition = VectorInt.Zero;
    }

    public override void OnDestroy()
    {
        _frameBuffer?.Dispose();
    }
}
