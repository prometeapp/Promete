using System.Reflection;
using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class ButtonTests : IDisposable
{
	private readonly PrometeApp _app;

	public ButtonTests()
	{
		_app = PrometeApp.Create()
			.Use<Keyboard>()
			.Use<Mouse>()
			.Use<UIManager>()
			.BuildWithHeadless();
		_app.OnStart();
	}

	public void Dispose()
	{
		_app.OnDestroy();
	}

	[Fact]
	public void Constructor_ShouldSetDefaults()
	{
		var button = new Button("テスト");

		button.TextContent.Should().Be("テスト");
		button.Size.Should().Be(new VectorInt(120, 40));
		button.IsEnabled.Should().BeTrue();
		button.IsFocusable.Should().BeTrue();
		button.State.Should().Be(UIElementState.Normal);
	}

	[Fact]
	public void TextContent_ShouldUpdateLabel()
	{
		var button = new Button("元のテキスト");
		button.TextContent = "新しいテキスト";

		button.TextContent.Should().Be("新しいテキスト");
		button.Label.Content.Should().Be("新しいテキスト");
	}

	[Fact]
	public void Style_ShouldBeChangeable()
	{
		var button = new Button("テスト");
		var customStyle = new DefaultButtonStyle
		{
			NormalColor = System.Drawing.Color.Red,
		};

		button.Style = customStyle;
		button.Style.Should().BeSameAs(customStyle);
	}

	[Fact]
	public void SetBackgroundNode_ShouldSwapBackground()
	{
		var button = new Button("テスト");

		var bg1 = Shape.CreateRect((0, 0), (120, 40), System.Drawing.Color.Red);
		button.SetBackgroundNode(bg1);
		button.Contains(bg1).Should().BeTrue();

		var bg2 = Shape.CreateRect((0, 0), (120, 40), System.Drawing.Color.Blue);
		button.SetBackgroundNode(bg2);
		button.Contains(bg1).Should().BeFalse();
		button.Contains(bg2).Should().BeTrue();
	}

	[Fact]
	public void SetBackgroundNode_Null_ShouldRemoveBackground()
	{
		var button = new Button("テスト");
		var bg = Shape.CreateRect((0, 0), (120, 40), System.Drawing.Color.Red);

		button.SetBackgroundNode(bg);
		button.SetBackgroundNode(null);

		button.Contains(bg).Should().BeFalse();
	}

	[Fact]
	public void FluentApi_ShouldWork()
	{
		var clicked = false;
		var button = new Button("テスト")
			.OnClick(() => clicked = true)
			.Enabled(true)
			.Focusable(true)
			.NavigationOrder(5);

		button.NavigationOrder.Should().Be(5);

		// Click イベントの発火テスト
		button.RaiseClicked();
		clicked.Should().BeTrue();
	}
}
