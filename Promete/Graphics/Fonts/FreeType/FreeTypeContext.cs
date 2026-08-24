using System;
using System.Threading;
using FreeTypeSharp;

namespace Promete.Graphics.Fonts.FreeType;

/// <summary>
/// プロセス全体で共有される FreeType ライブラリのインスタンスを管理します。
/// </summary>
internal static unsafe class FreeTypeContext
{
    private static readonly Lock InitializeLock = new();
    private static FT_LibraryRec_* _library;

    /// <summary>
    /// FreeType ライブラリのハンドルを取得します。初回アクセス時に初期化されます。
    /// </summary>
    internal static FT_LibraryRec_* Library
    {
        get
        {
            if (_library != null)
                return _library;

            lock (InitializeLock)
            {
                if (_library != null)
                    return _library;

                FT_LibraryRec_* library;
                var error = FT.FT_Init_FreeType(&library);
                if (error != FT_Error.FT_Err_Ok)
                    throw new FontException($"FreeType の初期化に失敗しました。({error})");

                _library = library;
            }

            return _library;
        }
    }

    /// <summary>
    /// FreeType のエラーコードを検査し、エラーであれば例外をスローします。
    /// </summary>
    internal static void ThrowIfError(FT_Error error, string message)
    {
        if (error != FT_Error.FT_Err_Ok)
            throw new FontException($"{message} ({error})");
    }
}
