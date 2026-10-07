// アセンブリ内の calli (ネイティブ関数ポインタ呼び出し) のシグネチャを集め、Mono WASM の
// interp-to-native 表に載せるための、登録専用の P/Invoke 宣言と C 関数を生成する。
//
//   dotnet run tools/gen-calli-signatures.cs -- --out <dir> [--library gl_shim] [--namespace Generated] <assembly.dll>...
//
// Mono WASM のインタプリタは、ネイティブ関数ポインタを呼ぶためのスタブを、引数の型の並び
// (シグネチャ) ごとにビルド時に生成する。生成対象は P/Invoke や UnmanagedCallersOnly から
// 集められるが、Silk.NET のような calli は集められず、実行時に CANNOT HANDLE INTERP ICALL SIG で
// 落ちる。そこで calli のシグネチャを IL から拾い、同じ形の P/Invoke を宣言して表に載せる。
//
// シグネチャは Mono の cookie と同じ表記にする。int / uint / enum / ポインタ / bool などは I、
// long は L、float は F、double は D、戻り値なしは V。

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

var outDir = string.Empty;
var library = "gl_shim";
var ns = "Generated";
var inputs = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--out":
            outDir = args[++i];
            break;
        case "--library":
            library = args[++i];
            break;
        case "--namespace":
            ns = args[++i];
            break;
        default:
            inputs.Add(args[i]);
            break;
    }
}

if (outDir.Length == 0 || inputs.Count == 0)
{
    Console.Error.WriteLine(
        "usage: gen-calli-signatures --out <dir> [--library name] [--namespace ns] <assembly.dll>..."
    );
    return 2;
}

var operandSizes = BuildOperandSizeTable();
var signatures = new SortedSet<string>(StringComparer.Ordinal);

foreach (var path in inputs)
    CollectSignatures(path, operandSizes, signatures);

Directory.CreateDirectory(outDir);
WriteIfDifferent(Path.Combine(outDir, "CalliSignatures.g.cs"), EmitCSharp(signatures, library, ns));
WriteIfDifferent(Path.Combine(outDir, "calli_signatures.g.c"), EmitC(signatures));

Console.WriteLine($"calli signatures: {signatures.Count}");
return 0;

/// <summary>指定アセンブリ内の全 calli から、cookie 形式のシグネチャを集める。</summary>
static void CollectSignatures(
    string path,
    Dictionary<ushort, OpCode> opcodes,
    SortedSet<string> result
)
{
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();
    var provider = new CookieProvider();

    foreach (var methodHandle in reader.MethodDefinitions)
    {
        var method = reader.GetMethodDefinition(methodHandle);
        if (method.RelativeVirtualAddress == 0)
            continue;

        var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
        if (il is null)
            continue;

        foreach (var token in FindCalliTokens(il, opcodes))
        {
            var handle = MetadataTokens.EntityHandle(token);
            if (handle.Kind != HandleKind.StandaloneSignature)
                continue;

            var blob = reader.GetBlobReader(
                reader.GetStandaloneSignature((StandaloneSignatureHandle)handle).Signature
            );
            var signature = new SignatureDecoder<char, object?>(
                provider,
                reader,
                null
            ).DecodeMethodSignature(ref blob);

            var cookie = new StringBuilder();
            cookie.Append(signature.ReturnType);
            foreach (var p in signature.ParameterTypes)
                cookie.Append(p);

            var text = cookie.ToString();
            if (text.Contains('S'))
            {
                Console.Error.WriteLine(
                    $"warning: skipped unsupported calli signature in {reader.GetString(method.Name)}"
                );
                continue;
            }

            result.Add(text);
        }
    }
}

/// <summary>IL を走査して calli のオペランド (シグネチャのトークン) を列挙する。</summary>
static IEnumerable<int> FindCalliTokens(byte[] il, Dictionary<ushort, OpCode> opcodes)
{
    var pos = 0;
    while (pos < il.Length)
    {
        ushort code = il[pos++];
        if (code == 0xFE && pos < il.Length)
            code = (ushort)(0xFE00 | il[pos++]);

        if (!opcodes.TryGetValue(code, out var op))
            yield break;

        if (op == OpCodes.Calli)
        {
            yield return BitConverter.ToInt32(il, pos);
            pos += 4;
            continue;
        }

        switch (op.OperandType)
        {
            case OperandType.InlineNone:
                break;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                pos += 1;
                break;
            case OperandType.InlineVar:
                pos += 2;
                break;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                pos += 8;
                break;
            case OperandType.InlineSwitch:
                var count = BitConverter.ToInt32(il, pos);
                pos += 4 + (4 * count);
                break;
            default:
                pos += 4;
                break;
        }
    }
}

static Dictionary<ushort, OpCode> BuildOperandSizeTable()
{
    var table = new Dictionary<ushort, OpCode>();
    foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
    {
        if (field.GetValue(null) is OpCode op)
            table[(ushort)op.Value] = op;
    }

    return table;
}

static string EmitCSharp(SortedSet<string> signatures, string library, string ns)
{
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine("// tools/gen-calli-signatures.cs によって生成された。直接編集しないこと。");
    sb.AppendLine("// </auto-generated>");
    sb.AppendLine();
    sb.AppendLine("using System.Runtime.InteropServices;");
    sb.AppendLine();
    sb.AppendLine($"namespace {ns};");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine(
        "/// calli のシグネチャを Mono WASM の interp-to-native 表に載せるための、登録専用の P/Invoke 宣言。"
    );
    sb.AppendLine("/// </summary>");
    sb.AppendLine("internal static class CalliSignatures");
    sb.AppendLine("{");
    sb.AppendLine(
        "    /// <summary>宣言への参照をトリミングで消えないように残す。実行時には何もしない。</summary>"
    );
    sb.AppendLine("    internal static void Register()");
    sb.AppendLine("    {");
    sb.AppendLine("        if (Environment.TickCount64 != long.MinValue) return;");
    sb.AppendLine();
    foreach (var s in signatures)
    {
        var zeros = string.Join(", ", Enumerable.Repeat("0", s.Length - 1));
        sb.AppendLine($"        Sig_{s}({zeros});");
    }

    sb.AppendLine("    }");
    foreach (var s in signatures)
    {
        var parameters = string.Join(", ", s.Skip(1).Select((c, i) => $"{ManagedType(c)} a{i}"));
        sb.AppendLine();
        sb.AppendLine($"    [DllImport(\"{library}\", EntryPoint = \"{EntryPoint(s)}\")]");
        sb.AppendLine($"    private static extern {ManagedType(s[0])} Sig_{s}({parameters});");
    }

    sb.AppendLine("}");
    return sb.ToString();
}

static string EmitC(SortedSet<string> signatures)
{
    var sb = new StringBuilder();
    sb.AppendLine("// tools/gen-calli-signatures.cs によって生成された。直接編集しないこと。");
    sb.AppendLine("// シグネチャ登録専用の関数で、呼ばれることはない。");
    sb.AppendLine();
    foreach (var s in signatures)
    {
        var parameters =
            s.Length == 1
                ? "void"
                : string.Join(", ", s.Skip(1).Select((c, i) => $"{NativeType(c)} a{i}"));
        var body = s[0] == 'V' ? string.Empty : " return 0;";
        sb.AppendLine($"{NativeType(s[0])} {EntryPoint(s)}({parameters}) {{{body} }}");
    }

    return sb.ToString();
}

static string EntryPoint(string signature) => "calli_sig_" + signature.ToLowerInvariant();

static string ManagedType(char c) =>
    c switch
    {
        'V' => "void",
        'I' => "int",
        'L' => "long",
        'F' => "float",
        'D' => "double",
        _ => throw new InvalidOperationException($"unsupported cookie char '{c}'"),
    };

static string NativeType(char c) =>
    c switch
    {
        'V' => "void",
        'I' => "int",
        'L' => "long long",
        'F' => "float",
        'D' => "double",
        _ => throw new InvalidOperationException($"unsupported cookie char '{c}'"),
    };

static void WriteIfDifferent(string path, string content)
{
    content = content.Replace("\r\n", "\n");
    if (File.Exists(path) && File.ReadAllText(path) == content)
        return;
    File.WriteAllText(path, content, new UTF8Encoding(false));
}

/// <summary>メタデータ上の型を、Mono の cookie 1 文字に対応づける。</summary>
internal sealed class CookieProvider : ISignatureTypeProvider<char, object?>
{
    public char GetPrimitiveType(PrimitiveTypeCode typeCode) =>
        typeCode switch
        {
            PrimitiveTypeCode.Void => 'V',
            PrimitiveTypeCode.Boolean
            or PrimitiveTypeCode.Char
            or PrimitiveTypeCode.SByte
            or PrimitiveTypeCode.Byte
            or PrimitiveTypeCode.Int16
            or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int32
            or PrimitiveTypeCode.UInt32
            or PrimitiveTypeCode.IntPtr
            or PrimitiveTypeCode.UIntPtr
            or PrimitiveTypeCode.Object
            or PrimitiveTypeCode.String => 'I',
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 => 'L',
            PrimitiveTypeCode.Single => 'F',
            PrimitiveTypeCode.Double => 'D',
            _ => 'S',
        };

    public char GetTypeFromDefinition(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        byte rawTypeKind
    )
    {
        if (rawTypeKind == (byte)SignatureTypeKind.Class)
            return 'I';

        var type = reader.GetTypeDefinition(handle);
        if (!IsEnum(reader, type))
            return 'S';

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if (reader.GetString(field.Name) == "value__")
                return field.DecodeSignature(this, null);
        }

        return 'S';
    }

    public char GetTypeFromReference(
        MetadataReader reader,
        TypeReferenceHandle handle,
        byte rawTypeKind
    ) => rawTypeKind == (byte)SignatureTypeKind.Class ? 'I' : 'S';

    public char GetTypeFromSpecification(
        MetadataReader reader,
        object? genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind
    ) => rawTypeKind == (byte)SignatureTypeKind.Class ? 'I' : 'S';

    public char GetPointerType(char elementType) => 'I';

    public char GetByReferenceType(char elementType) => 'I';

    public char GetFunctionPointerType(MethodSignature<char> signature) => 'I';

    public char GetSZArrayType(char elementType) => 'I';

    public char GetArrayType(char elementType, ArrayShape shape) => 'I';

    public char GetGenericInstantiation(char genericType, ImmutableArray<char> typeArguments) =>
        genericType;

    public char GetGenericMethodParameter(object? genericContext, int index) => 'S';

    public char GetGenericTypeParameter(object? genericContext, int index) => 'S';

    public char GetModifiedType(char modifier, char unmodifiedType, bool isRequired) =>
        unmodifiedType;

    public char GetPinnedType(char elementType) => elementType;

    private static bool IsEnum(MetadataReader reader, TypeDefinition type)
    {
        if (type.BaseType.Kind != HandleKind.TypeReference)
            return false;

        var baseRef = reader.GetTypeReference((TypeReferenceHandle)type.BaseType);
        return reader.GetString(baseRef.Namespace) == "System"
            && reader.GetString(baseRef.Name) == "Enum";
    }
}
