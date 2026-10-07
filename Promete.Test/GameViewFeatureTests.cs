#pragma warning disable CS0067
using FluentAssertions;
using Promete.Backends;
using Promete.Graphics;
using Promete.Headless;
using Promete.Windowing;

namespace Promete.Test;

/// <summary>
/// <see cref="IGameView.IsSupported(GameViewFeature)"/> の既定の実装と、各バックエンドの申告を確かめるテスト。
/// </summary>
public class GameViewFeatureTests
{
    public static TheoryData<GameViewFeature> AllFeatures => [.. Enum.GetValues<GameViewFeature>()];

    [Theory]
    [MemberData(nameof(AllFeatures))]
    public void IsSupported_ShouldReturnTrueByDefault(GameViewFeature feature)
    {
        IGameView view = new MinimalGameView();

        view.IsSupported(feature).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(AllFeatures))]
    public void IsSupported_ShouldReturnFalseOnHeadless(GameViewFeature feature)
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        app.View.IsSupported(feature).Should().BeFalse();
    }

    /// <summary>
    /// <see cref="IGameView.IsSupported(GameViewFeature)"/> を実装しない、外部のバックエンドを模したビュー。
    /// </summary>
    private sealed class MinimalGameView : IGameView
    {
        public event Action<FileDroppedEventArgs>? FileDropped;

        public event Action? Resize;

        public VectorInt Location { get; set; }

        public VectorInt Size { get; set; }

        public VectorInt ActualSize => Size;

        public int Scale { get; set; } = 1;

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int ActualWidth => Width;

        public int ActualHeight => Height;

        public bool IsVisible { get; set; }

        public bool IsFocused => true;

        public bool IsFullScreen { get; set; }

        public bool TopMost { get; set; }

        public float PixelRatio => 1f;

        public string Title { get; set; } = string.Empty;

        public WindowMode Mode { get; set; }

        public Texture2D TakeScreenshot() => default;

        public Task SaveScreenshotAsync(string path, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
