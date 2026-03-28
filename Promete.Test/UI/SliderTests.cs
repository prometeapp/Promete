using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class SliderTests : IDisposable
{
	private readonly PrometeApp _app;

	public SliderTests()
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
		var slider = new Slider();

		slider.Value.Should().Be(0f);
		slider.Minimum.Should().Be(0f);
		slider.Maximum.Should().Be(1f);
		slider.IntegerOnly.Should().BeTrue();
		slider.Step.Should().Be(1f);
		slider.IsDragging.Should().BeFalse();
		slider.Size.Should().Be(new VectorInt(200, 24));
	}

	[Fact]
	public void Constructor_WithCustomRange_ShouldSetValues()
	{
		var slider = new Slider(minimum: 10f, maximum: 100f, value: 50f);

		slider.Minimum.Should().Be(10f);
		slider.Maximum.Should().Be(100f);
		slider.Value.Should().Be(50f);
	}

	[Fact]
	public void Constructor_ValueClampedToRange()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f, value: 20f);

		slider.Value.Should().Be(10f);
	}

	[Fact]
	public void Value_ShouldBeClampedToMinMax()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f);

		slider.Value = 15f;
		slider.Value.Should().Be(10f);

		slider.Value = -5f;
		slider.Value.Should().Be(0f);
	}

	[Fact]
	public void Value_Setter_ShouldFireValueChanged()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f);
		var firedValues = new List<float>();
		slider.ValueChanged += v => firedValues.Add(v);

		slider.Value = 5f;
		slider.Value = 8f;

		firedValues.Should().Equal(5f, 8f);
	}

	[Fact]
	public void Value_SetSameValue_ShouldNotFireEvent()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f, value: 5f);
		var fired = false;
		slider.ValueChanged += _ => fired = true;

		slider.Value = 5f;

		fired.Should().BeFalse();
	}

	[Fact]
	public void IntegerOnly_False_AllowsFloatValues()
	{
		var slider = new Slider(minimum: 0f, maximum: 1f);
		slider.IntegerOnly = false;

		slider.Value = 0.5f;
		slider.Value.Should().BeApproximately(0.5f, 0.001f);
	}

	[Fact]
	public void NormalizedValue_ShouldCalculateCorrectly()
	{
		var slider = new Slider(minimum: 0f, maximum: 100f, value: 50f);

		slider.NormalizedValue.Should().BeApproximately(0.5f, 0.001f);
	}

	[Fact]
	public void NormalizedValue_AtMin_ShouldBeZero()
	{
		var slider = new Slider(minimum: 10f, maximum: 20f, value: 10f);

		slider.NormalizedValue.Should().Be(0f);
	}

	[Fact]
	public void NormalizedValue_AtMax_ShouldBeOne()
	{
		var slider = new Slider(minimum: 10f, maximum: 20f, value: 20f);

		slider.NormalizedValue.Should().BeApproximately(1f, 0.001f);
	}

	[Fact]
	public void NormalizedValue_EqualMinMax_ShouldBeZero()
	{
		var slider = new Slider(minimum: 5f, maximum: 5f, value: 5f);

		slider.NormalizedValue.Should().Be(0f);
	}

	[Fact]
	public void Minimum_Change_ShouldClampValue()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f, value: 3f);

		slider.Minimum = 5f;
		slider.Value.Should().Be(5f);
	}

	[Fact]
	public void Maximum_Change_ShouldClampValue()
	{
		var slider = new Slider(minimum: 0f, maximum: 10f, value: 8f);

		slider.Maximum = 5f;
		slider.Value.Should().Be(5f);
	}
}
