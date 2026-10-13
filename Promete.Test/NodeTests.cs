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
        act.Should().Throw<ArgumentException>().WithMessage("*自分自身*");
    }

    [Fact]
    public void InsertSelfAsChild_ShouldThrowArgumentException()
    {
        // Arrange
        var container = new Container();

        // Act & Assert
        var act = () => container.Insert(0, container);
        act.Should().Throw<ArgumentException>().WithMessage("*自分自身*");
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

    [Fact]
    public void PixelSnap_WithFractionalPivotOffset_ShouldRoundTranslation()
    {
        // Arrange: 奇数サイズ + 中央ピボットで 0.5px の端数が出る
        var node = new Container();
        node.Size = (15, 15);
        node.Pivot = (0.5f, 0.5f);
        node.Location = (100, 100);

        // Act
        node.UpdateModelMatrix();

        // Assert
        node.ModelMatrix.M41.Should().Be(MathF.Round(node.ModelMatrix.M41));
        node.ModelMatrix.M42.Should().Be(MathF.Round(node.ModelMatrix.M42));
    }

    [Fact]
    public void PixelSnap_WhenDisabled_ShouldKeepFractionalTranslation()
    {
        // Arrange
        var node = new Container();
        node.Size = (15, 15);
        node.Pivot = (0.5f, 0.5f);
        node.Location = (100, 100);
        node.IsPixelSnapEnabled = false;

        // Act
        node.UpdateModelMatrix();

        // Assert
        node.ModelMatrix.M41.Should().Be(92.5f);
        node.ModelMatrix.M42.Should().Be(92.5f);
    }

    [Fact]
    public void PixelSnap_OnParent_ShouldPropagateSnappedMatrixToChild()
    {
        // Arrange: 親の端数位置がスナップされ、子はその行列を基に計算される
        var parent = new Container();
        parent.Location = (0.25f, 0.75f);

        var child = new Container();
        child.Location = (10, 10);
        child.IsPixelSnapEnabled = false;
        parent.Add(child);

        // Act
        parent.UpdateModelMatrix();

        // Assert
        child.ModelMatrix.M41.Should().Be(10);
        child.ModelMatrix.M42.Should().Be(11);
    }

    [Fact]
    public void Absolute_WithoutRotationOrPivot_ShouldComposeLocationAndScale()
    {
        var parent = new Container { Location = (100, 100), Scale = (2, 2) };
        var child = new Container { Location = (50, 0) };
        parent.Add(child);

        child.AbsoluteLocation.Should().Be((Vector)(200, 100));
        child.AbsoluteScale.Should().Be((Vector)(2, 2));
        child.AbsoluteAngle.ToDegrees().Should().BeApproximately(0, 1e-4f);
    }

    [Fact]
    public void AbsoluteLocation_WithRotatedParent_ShouldRotateChildOffset()
    {
        var parent = new Container { Location = (100, 100), Angle = Angle.FromDegrees(90) };
        var child = new Container { Location = (10, 0) };
        parent.Add(child);

        // 時計回り 90°: ローカル +X は画面の +Y へ向く
        child.AbsoluteLocation.X.Should().BeApproximately(100, 1e-3f);
        child.AbsoluteLocation.Y.Should().BeApproximately(110, 1e-3f);
        child.AbsoluteAngle.ToDegrees().Should().BeApproximately(90, 1e-3f);
    }

    [Fact]
    public void AbsoluteLocation_WithParentPivot_ShouldShiftChild()
    {
        var parent = new Container
        {
            Size = (20, 20),
            Pivot = (0.5f, 0.5f),
            Location = (100, 100),
        };
        var child = new Container { Location = (10, 10) };
        parent.Add(child);

        // 親のピボット (10, 10) が Location に来るので、子のローカル (10, 10) はちょうど親の Location に重なる
        child.AbsoluteLocation.Should().Be((Vector)(100, 100));
    }

    [Fact]
    public void Absolute_WithFlippedParent_ShouldReflectNegativeScale()
    {
        // FrameBuffer の子コンテナと同じ構成
        var parent = new Container { Location = (0, 240), Scale = (1, -1) };
        var child = new Container { Location = (0, 10), Angle = Angle.FromDegrees(30) };
        parent.Add(child);

        child.AbsoluteLocation.Should().Be((Vector)(0, 230));
        child.AbsoluteScale.Should().Be((Vector)(1, -1));
        child.AbsoluteAngle.ToDegrees().Should().BeApproximately(-30, 1e-3f);
    }

    [Fact]
    public void Absolute_AfterLocationChange_ShouldNotWaitForRender()
    {
        var parent = new Container();
        var child = new Container();
        parent.Add(child);
        parent.UpdateModelMatrix();

        parent.Location = (30, 40);

        child.AbsoluteLocation.Should().Be((Vector)(30, 40));
    }

    [Fact]
    public void AbsoluteLocation_ShouldNotApplyPixelSnap()
    {
        var node = new Container { Location = (0.25f, 0.75f) };

        node.AbsoluteLocation.Should().Be((Vector)(0.25f, 0.75f));
    }
}
