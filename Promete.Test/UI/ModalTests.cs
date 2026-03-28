using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class ModalTests : IDisposable
{
	private readonly PrometeApp _app;

	public ModalTests()
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
		var modal = new Modal();

		modal.CloseOnOverlayClick.Should().BeTrue();
		modal.IsFocusable.Should().BeFalse();
		modal.Body.Should().NotBeNull();
		modal.Body.Size.Should().Be(new VectorInt(300, 200));
	}

	[Fact]
	public void Constructor_WithCustomBodySize_ShouldSetBodySize()
	{
		var modal = new Modal(bodySize: (400, 300));

		modal.Body.Size.Should().Be(new VectorInt(400, 300));
	}

	[Fact]
	public void Body_ShouldBeAccessiblePanel()
	{
		var modal = new Modal();

		modal.Body.Should().BeOfType<Panel>();
	}

	[Fact]
	public void CloseOnOverlayClick_ShouldBeSettable()
	{
		var modal = new Modal();

		modal.CloseOnOverlayClick = false;
		modal.CloseOnOverlayClick.Should().BeFalse();
	}

	[Fact]
	public void Style_ShouldBeChangeable()
	{
		var modal = new Modal();
		var newStyle = new DefaultModalStyle();

		modal.Style = newStyle;
		modal.Style.Should().BeSameAs(newStyle);
	}
}
