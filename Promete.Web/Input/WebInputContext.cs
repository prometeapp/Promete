using System.Globalization;
using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// ブラウザの入力を Silk.NET の <see cref="IInputContext"/> として見せる実装です。
/// キーボードとマウスのみで、ゲームパッドは未対応です。
/// </summary>
/// <remarks>
/// JavaScript (<c>input.js</c>) が DOM のイベントを溜め、毎フレームの先頭で <see cref="Apply"/> がまとめて反映します。
/// フレームの途中で入力の状態が変わることはありません。
/// </remarks>
internal sealed class WebInputContext : IInputContext
{
    private readonly List<string> _deferred = [];

    public event Action<IInputDevice, bool>? ConnectionChanged;

    public static WebInputContext Instance { get; } = new();

    public IntPtr Handle => IntPtr.Zero;

    public WebKeyboard Keyboard { get; } = new();

    public WebMouse Mouse { get; } = new();

    public IReadOnlyList<IGamepad> Gamepads { get; } = [];

    public IReadOnlyList<IJoystick> Joysticks { get; } = [];

    public IReadOnlyList<IKeyboard> Keyboards => [Keyboard];

    public IReadOnlyList<IMouse> Mice => [Mouse];

    public IReadOnlyList<IInputDevice> OtherDevices { get; } = [];

    /// <summary>
    /// 溜まった入力イベントを、発生した順に反映します。毎フレームの更新の前に 1 回呼びます。
    /// </summary>
    /// <remarks>
    /// 1 フレームで反映する状態の変化は、キーとボタンごとに 1 回までです。
    /// 同じフレームに押下と解放の両方があれば、解放 (とそれ以降の同じキーのイベント) を次のフレームに持ち越します。
    /// これにより、毎フレームのポーリングでも、押した瞬間を取りこぼしません。
    /// </remarks>
    /// <param name="events">JavaScript が溜めたイベント。形式は <c>input.js</c> を参照。</param>
    public void Apply(IReadOnlyList<string> events)
    {
        Mouse.ResetWheel();

        var pending = new List<string>(_deferred.Count + events.Count);
        pending.AddRange(_deferred);
        pending.AddRange(events);
        _deferred.Clear();

        var changed = new HashSet<string>();
        var deferredTargets = new HashSet<string>();
        foreach (var e in pending)
        {
            var parts = e.Split(':', 2);
            var (type, arg) = (parts[0], parts.Length > 1 ? parts[1] : string.Empty);

            if (type is "kd" or "ku" or "md" or "mu")
            {
                var target = (type[0] == 'k' ? "k:" : "m:") + arg;
                if (deferredTargets.Contains(target) || !changed.Add(target))
                {
                    deferredTargets.Add(target);
                    _deferred.Add(e);
                    continue;
                }
            }

            ApplyEvent(type, arg);
        }
    }

    public void Dispose()
    {
        _ = ConnectionChanged;
    }

    private static float ParseFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private void ApplyEvent(string type, string arg)
    {
        switch (type)
        {
            case "kd":
                Keyboard.Press(WebKeyMap.ToKey(arg));
                break;
            case "ku":
                Keyboard.Release(WebKeyMap.ToKey(arg));
                break;
            case "ch":
                foreach (var c in arg)
                    Keyboard.Type(c);
                break;
            case "md":
            case "mu":
                Mouse.Button(
                    WebKeyMap.ToMouseButton(int.Parse(arg, CultureInfo.InvariantCulture)),
                    type == "md"
                );
                break;
            case "mm":
            {
                var xy = arg.Split(':');
                Mouse.Move(ParseFloat(xy[0]), ParseFloat(xy[1]));
                break;
            }

            case "wh":
            {
                var xy = arg.Split(':');
                Mouse.Wheel(ParseFloat(xy[0]), ParseFloat(xy[1]));
                break;
            }
        }
    }
}
