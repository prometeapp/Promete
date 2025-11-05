using FluentAssertions;
using Promete.Nodes;

namespace Promete.Test;

public class NodeTests
{
    [Fact]
    public void AddSelfAsChild_ShouldThrowArgumentException()
    {
        // Arrange
        var container = new Container();

        // Act & Assert
        var act = () => container.Add(container);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*自分自身*");
    }

    [Fact]
    public void InsertSelfAsChild_ShouldThrowArgumentException()
    {
        // Arrange
        var container = new Container();

        // Act & Assert
        var act = () => container.Insert(0, container);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*自分自身*");
    }

    [Fact]
    public void AddDifferentNode_ShouldSucceed()
    {
        // Arrange
        var container = new Container();
        var child = new Container();

        // Act
        container.Add(child);

        // Assert
        container.Count.Should().Be(1);
        child.Parent.Should().Be(container);
    }

    [Fact]
    public void InsertDifferentNode_ShouldSucceed()
    {
        // Arrange
        var container = new Container();
        var child = new Container();

        // Act
        container.Insert(0, child);

        // Assert
        container.Count.Should().Be(1);
        child.Parent.Should().Be(container);
    }

    [Fact]
    public void InsertNodeWithExistingParent_ShouldMoveNode()
    {
        // Arrange
        var oldParent = new Container();
        var newParent = new Container();
        var child = new Container();
        
        oldParent.Add(child);

        // Act
        newParent.Insert(0, child);

        // Assert
        oldParent.Count.Should().Be(0);
        newParent.Count.Should().Be(1);
        child.Parent.Should().Be(newParent);
    }
}
