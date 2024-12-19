using FluentAssertions;

namespace Promete.Test;

public class StringExtension
{
    [Fact]
    public void ReplaceAt()
    {
        var str = "I have 300yen.";
        var replaced = str.ReplaceAt(7, "100");

        replaced.Should().Be("I have 100yen.");
    }

    [Fact]
    public void ReplaceAt_WithOverflow()
    {
        var str = "I have 300yen.";
        var replaced = str.ReplaceAt(7, "10000yen.");

        replaced.Should().Be("I have 10000yen.");
    }

    [Fact]
    public void ReplaceAt_WithOverflow2()
    {
        var str = "I have 300yen.";
        var replaced = str.ReplaceAt(15, "However the chocolate costs 10000yen...");

        replaced.Should().Be("I have 300yen. However the chocolate costs 10000yen...");
    }
}
