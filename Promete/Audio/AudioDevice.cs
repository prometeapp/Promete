using System;
using System.Threading;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// プロセス全体で共有される ALC デバイス・コンテキストを管理するクラスです。
/// OpenAL のカレントコンテキストはプロセスグローバルであるため、<see cref="AudioPlayer"/> ごとに
/// 個別のコンテキストを作成すると、後から作られたコンテキストが先のコンテキストを上書きしてしまいます。
/// このクラスは参照カウント方式でデバイス・コンテキストを共有し、この問題を回避します。
/// </summary>
public sealed class AudioDevice : IDisposable
{
    private static readonly Lock Lock = new();
    private static AudioDevice? _shared;
    private static int _refCount;

    private readonly nint _device;
    private readonly nint _context;
    private bool _isDisposed;

    private unsafe AudioDevice()
    {
        Al = AL.GetApi(true);
        var alc = ALContext.GetApi(true);

        Al.DistanceModel(DistanceModel.None);

        var d = alc.OpenDevice("");
        var c = alc.CreateContext(d, null);
        alc.MakeContextCurrent(c);
        _device = (nint)d;
        _context = (nint)c;

        SupportsFloat32 = Al.IsExtensionPresent("AL_EXT_FLOAT32");

        Alc = alc;
    }

    /// <summary>
    /// Silk.NET の AL API インスタンスを取得します。
    /// </summary>
    public AL Al { get; }

    /// <summary>
    /// AL_EXT_FLOAT32 拡張（float32 形式のバッファ）がサポートされているかどうかを取得します。
    /// </summary>
    public bool SupportsFloat32 { get; }

    private ALContext Alc { get; }

    /// <summary>
    /// 共有 <see cref="AudioDevice"/> インスタンスを取得します。参照カウントを1つ増やします。
    /// 初めての呼び出し、または全ての参照が解放された後の呼び出しでは、デバイス・コンテキストが新規に初期化されます。
    /// </summary>
    /// <returns>共有される <see cref="AudioDevice"/> インスタンス。</returns>
    public static AudioDevice Acquire()
    {
        lock (Lock)
        {
            _shared ??= new AudioDevice();
            _refCount++;
            return _shared;
        }
    }

    /// <summary>
    /// このインスタンスへの参照を解放します。参照カウントが0になった場合、ALC コンテキスト・デバイスを破棄します。
    /// </summary>
    public unsafe void Dispose()
    {
        lock (Lock)
        {
            if (_isDisposed)
                return;

            _refCount--;
            if (_refCount > 0)
                return;

            _isDisposed = true;
            Alc.DestroyContext((Context*)_context);
            Alc.CloseDevice((Device*)_device);
            Al.Dispose();
            Alc.Dispose();

            if (ReferenceEquals(_shared, this))
                _shared = null;

            GC.SuppressFinalize(this);
        }
    }
}
