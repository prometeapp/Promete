using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class TextInputTests : IDisposable
{
	private readonly PrometeApp _app;

	public TextInputTests()
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
		var input = new TextInput();

		input.Text.Should().BeEmpty();
		input.Placeholder.Should().BeEmpty();
		input.CursorPosition.Should().Be(0);
		input.Size.Should().Be(new VectorInt(200, 32));
		input.IsFocusable.Should().BeTrue();
	}

	[Fact]
	public void Constructor_WithPlaceholder_ShouldSetPlaceholder()
	{
		var input = new TextInput("Enter text...");

		input.Placeholder.Should().Be("Enter text...");
		input.PlaceholderNode.Content.Should().Be("Enter text...");
	}

	[Fact]
	public void Text_Setter_ShouldFireTextChanged()
	{
		var input = new TextInput();
		var firedValues = new List<string>();
		input.TextChanged += v => firedValues.Add(v);

		input.Text = "hello";
		input.Text = "world";

		firedValues.Should().Equal("hello", "world");
	}

	[Fact]
	public void Text_SetSameValue_ShouldNotFireEvent()
	{
		var input = new TextInput();
		input.Text = "test";

		var fired = false;
		input.TextChanged += _ => fired = true;

		input.Text = "test";

		fired.Should().BeFalse();
	}

	[Fact]
	public void Text_Setter_ShouldUpdateTextNode()
	{
		var input = new TextInput();

		input.Text = "hello";

		input.TextNode.Content.Should().Be("hello");
	}

	[Fact]
	public void CursorPosition_ShouldClampToTextLength()
	{
		var input = new TextInput();
		input.Text = "abc";

		input.CursorPosition = 10;
		input.CursorPosition.Should().Be(3);

		input.CursorPosition = -5;
		input.CursorPosition.Should().Be(0);
	}

	[Fact]
	public void Text_Change_ShouldClampCursorPosition()
	{
		var input = new TextInput();
		input.Text = "hello";
		input.CursorPosition = 5;

		input.Text = "hi";

		input.CursorPosition.Should().Be(2);
	}

	[Fact]
	public void Placeholder_Setter_ShouldUpdateNode()
	{
		var input = new TextInput("old");

		input.Placeholder = "new";

		input.Placeholder.Should().Be("new");
		input.PlaceholderNode.Content.Should().Be("new");
	}
}
