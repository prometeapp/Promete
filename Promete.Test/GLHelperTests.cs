using FluentAssertions;
using Promete.Graphics.Rendering.GL;

namespace Promete.Test;

public class GLHelperTests
{
    [Fact]
    public void ToGlslEs_WithGlsl330Core_ShouldReplaceHeader()
    {
        const string source =
            "#version 330 core\nout vec4 color;\nvoid main() { color = vec4(1.0); }\n";

        var result = GLHelper.ToGlslEs(source);

        result
            .Should()
            .Be(
                "#version 300 es\nprecision highp float;\nprecision highp int;\nprecision highp sampler2D;"
                    + "\nout vec4 color;\nvoid main() { color = vec4(1.0); }\n"
            );
    }

    [Fact]
    public void ToGlslEs_WithLeadingWhitespace_ShouldReplaceHeader()
    {
        var result = GLHelper.ToGlslEs("\n  #version 330 core\nvoid main() {}\n");

        result.Should().StartWith("#version 300 es\n").And.EndWith("\nvoid main() {}\n");
    }

    [Theory]
    [InlineData("#version 300 es\nvoid main() {}\n")]
    [InlineData("#version 450\nvoid main() {}\n")]
    [InlineData("void main() {}\n")]
    public void ToGlslEs_WithOtherSource_ShouldReturnAsIs(string source)
    {
        GLHelper.ToGlslEs(source).Should().BeSameAs(source);
    }
}
