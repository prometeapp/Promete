using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FreeTypeSharp;

namespace Promete.Graphics.Fonts.FreeType;

/// <summary>
/// 実行環境にインストールされているフォントを走査します。
/// </summary>
internal static unsafe class SystemFontScanner
{
    private const int StyleFlagItalic = 1 << 0;
    private const int StyleFlagBold = 1 << 1;

    private static readonly string[] FontExtensions = [".ttf", ".ttc", ".otf", ".otc", ".pfb"];

    /// <summary>
    /// 実行環境のフォントディレクトリを走査し、見つかったフォントの情報を列挙します。
    /// </summary>
    public static List<SystemFontInfo> Scan()
    {
        var result = new List<SystemFontInfo>();

        foreach (var directory in EnumerateFontDirectories())
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var path in EnumerateFontFiles(directory))
                ScanFile(path, result);
        }

        return result;
    }

    /// <summary>
    /// 実行環境におけるフォントディレクトリを列挙します。
    /// </summary>
    private static IEnumerable<string> EnumerateFontDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Fonts"
            );
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "Windows",
                "Fonts"
            );
            yield break;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            yield return "/System/Library/Fonts";
            yield return "/Library/Fonts";
            yield return Path.Combine(home, "Library", "Fonts");
            yield break;
        }

        yield return "/usr/share/fonts";
        yield return "/usr/local/share/fonts";
        yield return Path.Combine(home, ".local", "share", "fonts");
        yield return Path.Combine(home, ".fonts");
    }

    private static IEnumerable<string> EnumerateFontFiles(string directory)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
        }
        catch (Exception)
        {
            // アクセス権のないディレクトリは読み飛ばす
            yield break;
        }

        foreach (var file in files)
        {
            if (Array.Exists(FontExtensions, ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                yield return file;
        }
    }

    /// <summary>
    /// フォントファイルに含まれるすべてのフェイスを走査します。
    /// </summary>
    private static void ScanFile(string path, List<SystemFontInfo> result)
    {
        var pathBytes = Encoding.UTF8.GetBytes(path + "\0");

        if (!TryOpenFace(pathBytes, 0, out var face))
            return;

        var faceCount = (int)face->num_faces;
        AppendFace(face, path, 0, result);
        FT.FT_Done_Face(face);

        for (var i = 1; i < faceCount; i++)
        {
            if (!TryOpenFace(pathBytes, i, out face))
                continue;

            AppendFace(face, path, i, result);
            FT.FT_Done_Face(face);
        }
    }

    private static bool TryOpenFace(byte[] pathBytes, int faceIndex, out FT_FaceRec_* face)
    {
        fixed (byte* p = pathBytes)
        {
            FT_FaceRec_* opened;
            var error = FT.FT_New_Face(
                FreeTypeContext.Library,
                p,
                (IntPtr)faceIndex,
                &opened
            );

            face = opened;
            return error == FT_Error.FT_Err_Ok;
        }
    }

    private static void AppendFace(
        FT_FaceRec_* face,
        string path,
        int faceIndex,
        List<SystemFontInfo> result
    )
    {
        var familyName = Marshal.PtrToStringUTF8((IntPtr)face->family_name);
        if (string.IsNullOrEmpty(familyName))
            return;

        var styleName = Marshal.PtrToStringUTF8((IntPtr)face->style_name) ?? string.Empty;
        var flags = face->style_flags.ToInt64();
        var style = ((flags & StyleFlagBold) != 0, (flags & StyleFlagItalic) != 0) switch
        {
            (true, true) => FontStyle.BoldItalic,
            (true, false) => FontStyle.Bold,
            (false, true) => FontStyle.Italic,
            _ => FontStyle.Normal,
        };

        result.Add(new SystemFontInfo(familyName, styleName, style, path, faceIndex));
    }
}
