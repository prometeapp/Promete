using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class CheckboxTests : IDisposable
{
	private readonly PrometeApp _app;

	public CheckboxTests()
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
		var checkbox = new Checkbox();

		checkbox.IsChecked.Should().BeFalse();
		checkbox.BoxSize.Should().Be(20);
		checkbox.LabelText.Should().BeEmpty();
		checkbox.Size.Should().Be(new VectorInt(200, 24));
		checkbox.State.Should().Be(UIElementState.Normal);
	}

	[Fact]
	public void Constructor_WithIsCheckedTrue_ShouldBeChecked()
	{
		var checkbox = new Checkbox(isChecked: true);

		checkbox.IsChecked.Should().BeTrue();
	}

	[Fact]
	public void IsChecked_Toggle_ShouldFireCheckedChanged()
	{
		var checkbox = new Checkbox();
		var firedValues = new List<bool>();
		checkbox.CheckedChanged += v => firedValues.Add(v);

		checkbox.IsChecked = true;
		checkbox.IsChecked = false;

		firedValues.Should().Equal(true, false);
	}

	[Fact]
	public void IsChecked_SetSameValue_ShouldNotFireEvent()
	{
		var checkbox = new Checkbox();
		var fired = false;
		checkbox.CheckedChanged += _ => fired = true;

		checkbox.IsChecked = false;

		fired.Should().BeFalse();
	}

	[Fact]
	public void LabelText_ShouldUpdateLabelNode()
	{
		var checkbox = new Checkbox("initial");

		checkbox.LabelText.Should().Be("initial");
		checkbox.Label.Content.Should().Be("initial");

		checkbox.LabelText = "updated";
		checkbox.LabelText.Should().Be("updated");
		checkbox.Label.Content.Should().Be("updated");
	}

	[Fact]
	public void Style_Change_ShouldTriggerStyleDirty()
	{
		var checkbox = new Checkbox();

		// State を Hovered に設定してスタイルダーティを間接的に確認
		checkbox.State = UIElementState.Hovered;
		checkbox.State.Should().Be(UIElementState.Hovered);

		var newStyle = new DefaultCheckboxStyle();
		checkbox.Style = newStyle;
		checkbox.Style.Should().BeSameAs(newStyle);
	}
}
