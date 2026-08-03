using System.Text;
using FluentAssertions;
using Promete.Graphics.Rendering.Vulkan;

namespace Promete.Test;

/// <summary>
/// <see cref="SpirvReflector"/> のパース堅牢性テスト。
///
/// レビュー指摘 #09 に対応する。パースループは opcode に応じて words[index + 1] 〜
/// words[index + 4] を読むが、ガードしているのは wordCount == 0 だけで、
/// index + wordCount が配列長を超えないことも、命令長が読もうとするオペランド分
/// あることも検査していない。そのため切り詰められた・不正な SPIR-V は
/// InvalidOperationException("不正な SPIR-V バイナリです。") ではなく
/// IndexOutOfRangeException になる。
///
/// 修正後は「不正な入力に対して InvalidOperationException を投げる」か
/// 「その命令を安全に読み飛ばす」のいずれかであるべき。
/// IndexOutOfRangeException が出ている間はテストが失敗する。
/// </summary>
public class SpirvReflectorTests
{
    private const uint MagicNumber = 0x07230203;

    private const uint OpName = 5;
    private const uint OpMemberName = 6;
    private const uint OpTypeImage = 25;
    private const uint OpTypeStruct = 30;
    private const uint OpTypePointer = 32;
    private const uint OpVariable = 59;
    private const uint OpDecorate = 71;
    private const uint OpMemberDecorate = 72;

    private const uint DecorationBinding = 33;
    private const uint DecorationDescriptorSet = 34;
    private const uint DecorationOffset = 35;

    private const uint StorageClassUniformConstant = 0;
    private const uint StorageClassUniform = 2;

    [Fact]
    public void 正常なSPIRVからUniformブロックを抽出できる()
    {
        // 構造体型 10 に uColor(offset 0) / uTime(offset 16) を持ち、
        // set=1, binding=0 の Uniform 変数 30 として宣言する
        var spirv = new SpirvBuilder()
            .Add(OpName, 30, PackString("Params"))
            .Add(OpMemberName, 10, 0, PackString("uColor"))
            .Add(OpMemberName, 10, 1, PackString("uTime"))
            .Add(OpMemberDecorate, 10, 0, DecorationOffset, 0)
            .Add(OpMemberDecorate, 10, 1, DecorationOffset, 16)
            .Add(OpDecorate, 30, DecorationDescriptorSet, 1)
            .Add(OpDecorate, 30, DecorationBinding, 0)
            .Add(OpTypeStruct, 10)
            .Add(OpTypePointer, 20, StorageClassUniform, 10)
            .Add(OpVariable, 20, 30, StorageClassUniform)
            .Build();

        var (blocks, _) = SpirvReflector.Reflect(spirv);

        blocks.Should().ContainSingle();
        blocks[0].Set.Should().Be(1);
        blocks[0].Binding.Should().Be(0);
        blocks[0].MemberOffsets.Should().Contain("uColor", 0u);
        blocks[0].MemberOffsets.Should().Contain("uTime", 16u);
    }

    [Fact]
    public void 正常なSPIRVからサンプラーを抽出できる()
    {
        var spirv = new SpirvBuilder()
            .Add(OpName, 31, PackString("uTexture0"))
            .Add(OpDecorate, 31, DecorationDescriptorSet, 0)
            .Add(OpDecorate, 31, DecorationBinding, 0)
            .Add(OpTypeImage, 11)
            .Add(OpTypePointer, 21, StorageClassUniformConstant, 11)
            .Add(OpVariable, 21, 31, StorageClassUniformConstant)
            .Build();

        var (_, samplers) = SpirvReflector.Reflect(spirv);

        samplers.Should().ContainSingle();
        samplers[0].Name.Should().Be("uTexture0");
        samplers[0].Set.Should().Be(0);
    }

    [Fact]
    public void マジックナンバーが不正なら例外を投げる()
    {
        var spirv = new byte[5 * 4];
        BitConverter.GetBytes(0xDEADBEEFu).CopyTo(spirv, 0);

        var act = () => SpirvReflector.Reflect(spirv);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ヘッダーより短い入力なら例外を投げる()
    {
        var spirv = new byte[3 * 4];
        BitConverter.GetBytes(MagicNumber).CopyTo(spirv, 0);

        var act = () => SpirvReflector.Reflect(spirv);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// 指摘#09: 命令の宣言長が配列の残りを超えている場合。
    /// index += wordCount で範囲外へ飛ぶか、オペランド読み出しで境界を越える。
    /// </summary>
    [Fact]
    public void 命令長が配列末尾を超えていてもIndexOutOfRangeにならない()
    {
        // wordCount = 8 と宣言しつつ、実際には 2 ワードしか続かない OpTypeStruct
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((8u << 16) | OpTypeStruct);
        words.Add(10);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>("切り詰められた SPIR-V は境界チェックで弾くべき");
    }

    /// <summary>
    /// 指摘#09: OpMemberDecorate は words[index + 4] を読むが、命令長の検査がない。
    /// </summary>
    [Fact]
    public void OpMemberDecorateが短くてもIndexOutOfRangeにならない()
    {
        // 本来 wordCount 5 必要なところを 4 で宣言し、末尾に配置する
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((4u << 16) | OpMemberDecorate);
        words.Add(10);
        words.Add(0);
        words.Add(DecorationOffset);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>(
                "OpMemberDecorate は words[index + 4] を読む前に命令長を検査すべき"
            );
    }

    /// <summary>
    /// 指摘#09: OpVariable は words[index + 3] を読むが、命令長の検査がない。
    /// </summary>
    [Fact]
    public void OpVariableが短くてもIndexOutOfRangeにならない()
    {
        // 本来 wordCount 4 必要なところを 3 で宣言し、末尾に配置する
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((3u << 16) | OpVariable);
        words.Add(20);
        words.Add(30);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>(
                "OpVariable は words[index + 3] を読む前に命令長を検査すべき"
            );
    }

    /// <summary>
    /// 指摘#09: OpTypePointer は words[index + 3] を読むが、命令長の検査がない。
    /// </summary>
    [Fact]
    public void OpTypePointerが短くてもIndexOutOfRangeにならない()
    {
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((3u << 16) | OpTypePointer);
        words.Add(20);
        words.Add(StorageClassUniform);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>(
                "OpTypePointer は words[index + 3] を読む前に命令長を検査すべき"
            );
    }

    /// <summary>
    /// 指摘#09: OpName の文字列読み出し範囲 (index + wordCount) が配列長を超えるケース。
    /// ReadString は範囲を信用して words[i] を読む。
    /// </summary>
    [Fact]
    public void OpNameの文字列が配列末尾を超えていてもIndexOutOfRangeにならない()
    {
        // wordCount = 16 と偽り、実際には 2 ワードしか存在しない
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((16u << 16) | OpName);
        words.Add(30);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>(
                "ReadString の終端は配列長でクランプすべき"
            );
    }

    /// <summary>
    /// 4 バイト境界に満たない入力。words 化の時点で情報が落ちる。
    /// </summary>
    [Fact]
    public void 語境界に満たない入力でもIndexOutOfRangeにならない()
    {
        var valid = new SpirvBuilder().Add(OpTypeStruct, 10).Build();
        var truncated = valid[..^3];

        var act = () => SpirvReflector.Reflect(truncated);

        act.Should().NotThrow<IndexOutOfRangeException>();
    }

    /// <summary>
    /// 全 opcode を総当たりで末尾に配置し、境界越えが起きないことを確認する。
    /// どの命令にガード漏れがあっても検出できる。
    /// </summary>
    [Theory]
    [InlineData(OpName)]
    [InlineData(OpMemberName)]
    [InlineData(OpTypeImage)]
    [InlineData(OpTypeStruct)]
    [InlineData(OpTypePointer)]
    [InlineData(OpVariable)]
    [InlineData(OpDecorate)]
    [InlineData(OpMemberDecorate)]
    public void 各命令が単独で末尾にあってもIndexOutOfRangeにならない(uint opcode)
    {
        // オペランドを 1 つも伴わない wordCount = 1 の命令
        var words = new List<uint> { MagicNumber, 0x00010600, 8, 1, 0 };
        words.Add((1u << 16) | opcode);

        var act = () => SpirvReflector.Reflect(ToBytes(words));

        act.Should()
            .NotThrow<IndexOutOfRangeException>(
                $"opcode {opcode} はオペランド読み出し前に命令長を検査すべき"
            );
    }

    private static byte[] ToBytes(List<uint> words)
    {
        var bytes = new byte[words.Count * 4];
        System.Buffer.BlockCopy(words.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// 文字列を SPIR-V のリテラル表現 (UTF-8 + NUL 終端 + 4 バイトパディング) に変換する。
    /// </summary>
    private static uint[] PackString(string value)
    {
        var utf8 = Encoding.UTF8.GetBytes(value);
        var wordCount = (utf8.Length / 4) + 1;
        var padded = new byte[wordCount * 4];
        utf8.CopyTo(padded, 0);

        var words = new uint[wordCount];
        System.Buffer.BlockCopy(padded, 0, words, 0, padded.Length);
        return words;
    }

    /// <summary>
    /// 正しい wordCount を自動計算しながら SPIR-V モジュールを組み立てる。
    /// </summary>
    private sealed class SpirvBuilder
    {
        private readonly List<uint> _words =
        [
            MagicNumber,
            0x00010600, // version 1.6
            8, // generator
            100, // bound
            0, // schema
        ];

        public SpirvBuilder Add(uint opcode, params object[] operands)
        {
            var flat = new List<uint>();
            foreach (var operand in operands)
            {
                switch (operand)
                {
                    case uint u:
                        flat.Add(u);
                        break;
                    case int i:
                        flat.Add((uint)i);
                        break;
                    case uint[] many:
                        flat.AddRange(many);
                        break;
                    default:
                        throw new ArgumentException($"未対応のオペランド型: {operand.GetType()}");
                }
            }

            var wordCount = (uint)(flat.Count + 1);
            _words.Add((wordCount << 16) | opcode);
            _words.AddRange(flat);
            return this;
        }

        public byte[] Build() => ToBytes(_words);
    }
}
