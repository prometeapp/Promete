using System;
using System.Runtime.InteropServices;

namespace Promete.Platforms;

/// <summary>
/// ユーザーが呼び出すことを想定していません。このクラスおよびメンバーの内容は予告なく変更される可能性があります。
/// </summary>
public static class MacNativeHelper
{
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr GetClass(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr GetSelector(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void ObjcMsgSendVoid(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendStr(IntPtr receiver, IntPtr selector, string str);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendInt(IntPtr receiver, IntPtr selector, int value);

    /// <summary>
    /// メニューバーのタイトルを変更します。
    /// </summary>
    /// <param name="name">タイトルに設定する文字列</param>
    public static void SetMenuBarTitle(string name)
    {
        if (!OperatingSystem.IsMacOS()) return;
        var nsStringClass = GetClass("NSString");
        var nsStr = ObjcMsgSendStr(
            ObjcMsgSend(nsStringClass, GetSelector("alloc")),
            GetSelector("initWithUTF8String:"),
            name
        );

        var app = ObjcMsgSend(GetClass("NSApplication"), GetSelector("sharedApplication"));
        var mainMenu = ObjcMsgSend(app, GetSelector("mainMenu"));
        var firstItem = ObjcMsgSendInt(mainMenu, GetSelector("itemAtIndex:"), 0 );
        ObjcMsgSendVoid(firstItem, GetSelector("setTitle:"), nsStr);
    }
}
