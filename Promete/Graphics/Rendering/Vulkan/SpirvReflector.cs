using System;
using System.Collections.Generic;
using System.Text;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// SPIR-V バイトコードから Uniform ブロックのレイアウト情報を抽出する最小リフレクタです。
/// カスタムシェーダーの名前ベース Uniform (Material) をオフセットに解決するために使用します。
/// </summary>
internal static class SpirvReflector
{
    private const uint OpMemberName = 6;
    private const uint OpTypeStruct = 30;
    private const uint OpTypePointer = 32;
    private const uint OpVariable = 59;
    private const uint OpDecorate = 71;
    private const uint OpMemberDecorate = 72;

    private const uint DecorationBinding = 33;
    private const uint DecorationDescriptorSet = 34;
    private const uint DecorationOffset = 35;

    private const uint StorageClassUniform = 2;

    /// <summary>
    /// SPIR-V から Uniform ブロック (storage class Uniform) の一覧を抽出します。
    /// </summary>
    public static List<UniformBlock> ReflectUniformBlocks(byte[] spirv)
    {
        var words = new uint[spirv.Length / 4];
        System.Buffer.BlockCopy(spirv, 0, words, 0, words.Length * 4);

        if (words.Length < 5 || words[0] != 0x07230203)
            throw new InvalidOperationException("不正な SPIR-V バイナリです。");

        // 収集用テーブル
        var memberNames = new Dictionary<(uint TypeId, uint Member), string>();
        var memberOffsets = new Dictionary<(uint TypeId, uint Member), uint>();
        var decorations = new Dictionary<(uint Id, uint Decoration), uint>();
        var structTypes = new HashSet<uint>();
        var pointerTargets = new Dictionary<uint, (uint StorageClass, uint TypeId)>();
        var uniformVariables = new List<(uint Id, uint PointerTypeId)>();

        var index = 5;
        while (index < words.Length)
        {
            var opcode = words[index] & 0xFFFF;
            var wordCount = (int)(words[index] >> 16);
            if (wordCount == 0)
                break;

            switch (opcode)
            {
                case OpMemberName:
                    memberNames[(words[index + 1], words[index + 2])] = ReadString(words, index + 3, index + wordCount);
                    break;
                case OpMemberDecorate when words[index + 3] == DecorationOffset:
                    memberOffsets[(words[index + 1], words[index + 2])] = words[index + 4];
                    break;
                case OpDecorate when wordCount >= 4:
                    decorations[(words[index + 1], words[index + 2])] = words[index + 3];
                    break;
                case OpTypeStruct:
                    structTypes.Add(words[index + 1]);
                    break;
                case OpTypePointer:
                    pointerTargets[words[index + 1]] = (words[index + 2], words[index + 3]);
                    break;
                case OpVariable when words[index + 3] == StorageClassUniform:
                    uniformVariables.Add((words[index + 2], words[index + 1]));
                    break;
            }

            index += wordCount;
        }

        // Uniform 変数 → 構造体レイアウトを解決
        var blocks = new List<UniformBlock>();
        foreach (var (variableId, pointerTypeId) in uniformVariables)
        {
            if (!pointerTargets.TryGetValue(pointerTypeId, out var pointer))
                continue;
            if (!structTypes.Contains(pointer.TypeId))
                continue;

            var offsets = new Dictionary<string, uint>();
            uint maxOffset = 0;
            foreach (var ((typeId, member), name) in memberNames)
            {
                if (typeId != pointer.TypeId)
                    continue;
                if (!memberOffsets.TryGetValue((typeId, member), out var offset))
                    continue;
                offsets[name] = offset;
                maxOffset = Math.Max(maxOffset, offset);
            }

            blocks.Add(
                new UniformBlock
                {
                    Set = decorations.GetValueOrDefault((variableId, DecorationDescriptorSet)),
                    Binding = decorations.GetValueOrDefault((variableId, DecorationBinding)),
                    MemberOffsets = offsets,
                    Size = maxOffset + 64,
                }
            );
        }

        return blocks;
    }

    private static string ReadString(uint[] words, int start, int end)
    {
        var bytes = new List<byte>((end - start) * 4);
        for (var i = start; i < end; i++)
        {
            var word = words[i];
            for (var b = 0; b < 4; b++)
            {
                var value = (byte)(word >> (b * 8));
                if (value == 0)
                    return Encoding.UTF8.GetString(bytes.ToArray());
                bytes.Add(value);
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// リフレクションで得られた Uniform ブロックの情報です。
    /// </summary>
    public sealed class UniformBlock
    {
        /// <summary>ディスクリプタセット番号。</summary>
        public uint Set { get; init; }

        /// <summary>バインディング番号。</summary>
        public uint Binding { get; init; }

        /// <summary>メンバー名 → バイトオフセット。</summary>
        public required Dictionary<string, uint> MemberOffsets { get; init; }

        /// <summary>ブロックの最低サイズ（最大オフセット + 64 バイトの余裕）。</summary>
        public uint Size { get; init; }
    }
}
