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
    private Texture2D _texture;

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
        _editorSprite.Texture = _frameBuffer.Texture;
        _previewSprite.Texture = _frameBuffer.Texture;
        _previewSprite.Scale *= 2;
        _frameBuffer.BackgroundColor = Color.White;

        _texture = Window.TextureFactory.Load("assets/ichigo.png");

        _frameBuffer.Add(new Text("Hello", null, Color.Black));
        _frameBuffer.Add(new Sprite(_texture).Location(0, 32));

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
        if (_frameBuffer == null) return;
        _previewSprite.Angle = (_previewSprite.Angle + 45 * Window.DeltaTime) % 360;

        if (keyboard.Enter.IsKeyDown)
        {
            _frameBuffer.Size = new VectorInt(300, 300);
            _editorSprite.Texture = _previewSprite.Texture = _frameBuffer.Texture;
        }

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
        _texture.Dispose();
    }
}
