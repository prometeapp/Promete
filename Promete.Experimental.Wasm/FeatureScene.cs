using System.Drawing;
using System.Numerics;
using Promete;
using Promete.Graphics;
using Promete.Nodes;

namespace Promete.Experimental.Wasm;

/// <summary>
/// 描画系ノードを 1 画面に並べた検証用シーン。ノードの生成時に起きた例外は <see cref="Failures"/> に記録する。
/// </summary>
public class FeatureScene : Scene
{
    private const int CellWidth = 160;
    private const int CellHeight = 120;

    private const string InstancedVertexShader = """
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

    private const string SepiaFragment = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uTexture0, fUv) * fTintColor;
            float r = dot(c.rgb, vec3(0.393, 0.769, 0.189));
            float g = dot(c.rgb, vec3(0.349, 0.686, 0.168));
            float b = dot(c.rgb, vec3(0.272, 0.534, 0.131));
            FragColor = vec4(r, g, b, c.a);
        }
        """;

    private const string RasterScrollFragment = """
        #version 330 core
        in vec2 fUv;
        in vec4 fTintColor;
        uniform sampler2D uTexture0;
        uniform float uTime;
        out vec4 FragColor;

        void main()
        {
            float offset = sin(fUv.y * 18.0 + uTime * 4.0) * 0.06;
            vec2 uv = vec2(fUv.x + offset, fUv.y);
            FragColor = texture(uTexture0, uv) * fTintColor;
        }
        """;

    private const string PostProcessFragment = """
        #version 330 core
        in vec2 fUv;
        uniform sampler2D uScreenTexture;
        out vec4 FragColor;

        void main()
        {
            vec4 c = texture(uScreenTexture, fUv);
            FragColor = vec4(c.rgb * vec3(1.0, 0.85, 0.7), c.a);
        }
        """;

    private const string PostProcessVertex = """
        #version 330 core
        layout (location = 0) in vec2 vPos;
        layout (location = 1) in vec2 vUv;
        out vec2 fUv;
        void main()
        {
            gl_Position = vec4(vPos.x, vPos.y, 0.0, 1.0);
            fUv = vUv;
        }
        """;

    private Sprite? _rotating;
    private PieSprite? _pie;
    private Material? _rasterMaterial;
    private Graphics.FrameBuffer? _frameBuffer;
    private Sprite? _frameBufferContent;

    /// <summary>実行する機能名。空なら全部。</summary>
    public static HashSet<string> Enabled { get; } = [];

    /// <summary>ノードの生成時に起きた例外を、機能名ごとに記録する。</summary>
    public static Dictionary<string, string> Failures { get; } = [];

    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        var features = new (string Name, Action<Container> Build)[]
        {
            ("shapes", BuildShapes),
            ("sprite", BuildSprite),
            ("trim", BuildTrim),
            ("stencil", c => BuildMask(c, useAlphaMask: false)),
            ("alphamask", c => BuildMask(c, useAlphaMask: true)),
            ("pie", BuildPie),
            ("nine", BuildNineSlice),
            ("tilemap", BuildTilemap),
            ("material", BuildMaterial),
            ("framebuffer", BuildFrameBuffer),
        };

        for (var i = 0; i < features.Length; i++)
        {
            var (name, build) = features[i];
            if (Enabled.Count > 0 && !Enabled.Contains(name))
                continue;

            var cell = new Container().Location((i % 4) * CellWidth, (i / 4) * CellHeight);
            try
            {
                build(cell);
                Root.Add(cell);
            }
            catch (Exception e)
            {
                Failures[name] = e.GetType().Name + ": " + e.Message;
            }
        }

        if (Enabled.Contains("postprocess"))
        {
            try
            {
                var shader = ShaderProgram
                    .Create()
                    .Vertex(PostProcessVertex)
                    .Fragment(PostProcessFragment)
                    .Compile();
                App.PostProcessMaterials.Add(new Material(shader));
            }
            catch (Exception e)
            {
                Failures["postprocess"] = e.GetType().Name + ": " + e.Message;
            }
        }
    }

    public override void OnUpdate()
    {
        var t = Time.TotalTime;
        if (_rotating != null)
            _rotating.Angle = Angle.FromDegrees(t * 60);
        if (_pie != null)
            _pie.Percent = (t * 25) % 100;
        if (_rasterMaterial != null)
            _rasterMaterial["uTime"] = t;
        if (_frameBufferContent != null)
            _frameBufferContent.Location = (10 + (MathF.Sin(t * 2) * 8), 10);
    }

    private static void BuildShapes(Container c)
    {
        c.Add(Shape.CreateLine(10, 10, 150, 30, Color.Red, 1));
        c.Add(Shape.CreateLine(10, 20, 150, 40, Color.Lime, 4));
        c.Add(Shape.CreatePixel(80, 60, Color.White));
        c.Add(Shape.CreateRect(10, 60, 60, 100, Color.FromArgb(80, Color.Cyan), 3, Color.Cyan));
        c.Add(Shape.CreateTriangle((90, 100), (120, 55), (150, 100), Color.Orange));
    }

    private void BuildSprite(Container c)
    {
        var texture = App.TextureFactory.Load("/assets/ichigo.png");
        c.Add(new Sprite(texture).Location(8, 8).Scale(3, 3));
        c.Add(new Sprite(texture, Color.FromArgb(255, 80, 200, 255)).Location(64, 8).Scale(3, 3));
        _rotating = new Sprite(texture) { Pivot = (0.5f, 0.5f), Scale = (3, 3) }.Location(120, 40);
        c.Add(_rotating);
    }

    private void BuildTrim(Container c)
    {
        var box = new Container(isTrimmable: true).Location(20, 20).Size(80, 60);
        box.Add(Shape.CreateRect(0, 0, 79, 59, Color.FromArgb(60, Color.White)));
        box.Add(Shape.CreateRect(-20, 10, 120, 30, Color.Crimson));
        c.Add(box);
    }

    private void BuildMask(Container c, bool useAlphaMask)
    {
        var mask = App.TextureFactory.Load("/assets/circle_mask.png");
        var texture = App.TextureFactory.CreateSolid(Color.Orange, (32, 32));
        var masked = new MaskedContainer(mask, useAlphaMask)
            .Location(40, 20)
            .Size(32, 32)
            .Scale(2, 2);
        masked.Add(new Sprite(texture));
        c.Add(masked);
    }

    private void BuildPie(Container c)
    {
        var texture = App.TextureFactory.CreateSolid(Color.Gold, (48, 48));
        _pie = new PieSprite(texture) { Percent = 50 }.Location(40, 20);
        c.Add(_pie);
    }

    private void BuildNineSlice(Container c)
    {
        var sliced = App.TextureFactory.Load9Sliced("/assets/rect.png", 16, 16, 16, 16);
        c.Add(new NineSliceSprite(sliced).Location(10, 10).Size(140, 90));
    }

    private void BuildTilemap(Container c)
    {
        var textures = App.TextureFactory.LoadSpriteSheet("/assets/tiles.png", 4, 1, (16, 16));
        var tiles = textures.Select(t => new Tile(t)).ToArray();
        var map = new Tilemap((16, 16));
        for (var x = 0; x < 8; x++)
        for (var y = 0; y < 5; y++)
            map.SetTile(x, y, tiles[(x + y) % tiles.Length]);
        map.Location = (16, 16);
        c.Add(map);
    }

    private void BuildMaterial(Container c)
    {
        var texture = App.TextureFactory.Load("/assets/ichigo.png");
        var sepia = new Material(
            ShaderProgram.Create().Vertex(InstancedVertexShader).Fragment(SepiaFragment).Compile()
        );
        _rasterMaterial = new Material(
            ShaderProgram
                .Create()
                .Vertex(InstancedVertexShader)
                .Fragment(RasterScrollFragment)
                .Compile()
        )
        {
            ["uTime"] = 0f,
        };

        c.Add(
            new Sprite(texture) { Material = sepia }
                .Location(10, 10)
                .Scale(4, 4)
        );
        c.Add(
            new Sprite(texture) { Material = _rasterMaterial }
                .Location(90, 10)
                .Scale(4, 4)
        );
    }

    private void BuildFrameBuffer(Container c)
    {
        if (!App.IsFrameBufferSupported)
            throw new NotSupportedException("IsFrameBufferSupported = false");

        var texture = App.TextureFactory.Load("/assets/ichigo.png");
        _frameBuffer = new Graphics.FrameBuffer(64, 64) { BackgroundColor = Color.White };
        _frameBufferContent = new Sprite(texture).Location(10, 10);
        _frameBuffer.Add(_frameBufferContent);
        _frameBuffer.Add(Shape.CreateRect(0, 0, 63, 63, Color.Transparent, 2, Color.Black));
        c.Add(new Sprite(_frameBuffer.Texture).Location(40, 10).Scale(1.5f, 1.5f));
    }
}
