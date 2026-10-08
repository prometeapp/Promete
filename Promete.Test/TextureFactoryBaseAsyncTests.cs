using System.Drawing;
using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Imaging;
using Promete.Headless;
using Promete.Test.Fakes;

namespace Promete.Test;

/// <summary>
/// <see cref="TextureFactoryBase"/> の非同期読み込みを検証します。
/// </summary>
public class TextureFactoryBaseAsyncTests
{
    [Fact]
    public async Task LoadAsyncはデコードした画像をアップロードする()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 2);
        var options = new TextureOptions(TextureFilterMode.Linear, TextureAddressMode.Repeat);

        var texture = await factory.LoadAsync(stream, options);

        texture.Size.Should().Be(new VectorInt(4, 2));
        factory.CreatedCount.Should().Be(1);
        factory.GetOptions(texture.Handle).Should().Be(options);
    }

    [Fact]
    public async Task LoadSpriteSheetAsyncは全セルが1枚のテクスチャを共有する()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);

        var cells = await factory.LoadSpriteSheetAsync(stream, 2, 2, (2, 2));

        cells.Should().HaveCount(4);
        factory.CreatedCount.Should().Be(1);
        cells.Should().OnlyContain(c => c.IsSubTexture);

        foreach (var cell in cells)
            cell.Dispose();
        factory.DisposedHandles.Should().Equal(cells[0].Handle);
    }

    [Fact]
    public async Task Load9SlicedAsyncは9枚のテクスチャを生成する()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(6, 6);

        var sliced = await factory.Load9SlicedAsync(stream, 2, 2, 2, 2);

        sliced.Should().NotBeNull();
        factory.CreatedCount.Should().Be(9);
    }

    [Fact]
    public async Task スプライトシートが画像からはみ出す場合は例外になる()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(4, 4);

        var act = () => factory.LoadSpriteSheetAsync(stream, 3, 1, (2, 2));

        await act.Should().ThrowAsync<ArgumentException>();
        factory.CreatedCount.Should().Be(0);
    }

    [Fact]
    public async Task 転送の開始前にキャンセルするとアップロードされない()
    {
        var factory = new FakeTextureFactory();
        using var stream = CreatePng(2, 2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => factory.LoadAsync(stream, default, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.CreatedCount.Should().Be(0);
    }

    [Fact]
    public void メインスレッド限定のバックエンドではアップロードがメインスレッドで実行される()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        var factory = new MainThreadFactory(app);
        using var stream = CreatePng(2, 2);

        var task = factory.LoadAsync(stream);

        // メインスレッドがフレームを進めるまで、転送は実行されない
        Thread.Sleep(100);
        task.IsCompleted.Should().BeFalse();
        factory.UploadThreadIds.Should().BeEmpty();

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            app.OnUpdate();
            Thread.Sleep(1);
        }

        task.IsCompletedSuccessfully.Should().BeTrue();
        factory.UploadThreadIds.Should().Equal(Environment.CurrentManagedThreadId);
    }

    [Fact]
    public void 九スライスの転送は1回のフレームでまとめて実行される()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        var factory = new MainThreadFactory(app);
        using var stream = CreatePng(6, 6);

        var task = factory.Load9SlicedAsync(stream, 2, 2, 2, 2);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (factory.PendingUploadRequests < 9 && DateTime.UtcNow < deadline)
            Thread.Sleep(1);

        app.OnUpdate();
        task.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();

        factory.UploadThreadIds.Should().HaveCount(9);
        factory
            .UploadThreadIds.Should()
            .OnlyContain(id => id == Environment.CurrentManagedThreadId);
    }

    private static MemoryStream CreatePng(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = 255;

        var stream = new MemoryStream();
        PngEncoder.Encode(new RgbaImage(width, height, pixels), stream);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// GL バックエンドのように、転送をメインスレッドへ委譲するファクトリです。
    /// </summary>
    private sealed class MainThreadFactory(PrometeApp app) : FakeTextureFactory
    {
        private int _pending;

        public List<int> UploadThreadIds { get; } = [];

        public int PendingUploadRequests => Volatile.Read(ref _pending);

        protected override int UploadTexture(in TextureUploadRequest request)
        {
            UploadThreadIds.Add(Environment.CurrentManagedThreadId);
            return base.UploadTexture(in request);
        }

        protected override Task<int> UploadTextureAsync(
            byte[] rgba,
            VectorInt size,
            TextureOptions options
        )
        {
            var task = app.InvokeOnMainThreadAsync(() =>
                UploadTexture(new TextureUploadRequest(rgba, size, options))
            );
            Interlocked.Increment(ref _pending);
            return task;
        }
    }
}
