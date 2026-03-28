using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class ScrollViewTests : IDisposable
{
	private readonly PrometeApp _app;

	public ScrollViewTests()
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
		var scrollView = new ScrollView();

		scrollView.IsTrimmable.Should().BeTrue();
		scrollView.IsFocusable.Should().BeFalse();
		scrollView.Size.Should().Be(new VectorInt(200, 200));
		scrollView.ContentSize.Should().Be(new VectorInt(200, 400));
		scrollView.ScrollOffset.Should().Be(new Vector(0, 0));
		scrollView.VerticalScrollEnabled.Should().BeTrue();
		scrollView.HorizontalScrollEnabled.Should().BeFalse();
		scrollView.ScrollSpeed.Should().Be(30f);
	}

	[Fact]
	public void Content_ShouldBeAccessible()
	{
		var scrollView = new ScrollView();

		scrollView.Content.Should().NotBeNull();
	}

	[Fact]
	public void ScrollTo_ShouldSetOffset()
	{
		var scrollView = new ScrollView();
		// ContentSize = (200, 400), Size = (200, 200) => maxY = 200
		Vector? firedOffset = null;
		scrollView.ScrollChanged += v => firedOffset = v;

		scrollView.ScrollTo(0, -100);

		scrollView.ScrollOffset.Y.Should().BeApproximately(-100f, 0.1f);
		firedOffset.Should().NotBeNull();
	}

	[Fact]
	public void ScrollTo_ShouldClampOffset()
	{
		var scrollView = new ScrollView();
		// maxY = ContentSize.Y - Size.Y = 400 - 200 = 200

		scrollView.ScrollTo(0, -500);
		scrollView.ScrollOffset.Y.Should().BeApproximately(-200f, 0.1f);

		scrollView.ScrollTo(0, 100);
		scrollView.ScrollOffset.Y.Should().BeApproximately(0f, 0.1f);
	}

	[Fact]
	public void NormalizedVerticalScroll_ShouldCalculateCorrectly()
	{
		var scrollView = new ScrollView();
		// maxY = 200

		scrollView.ScrollTo(0, -100);
		scrollView.NormalizedVerticalScroll.Should().BeApproximately(0.5f, 0.01f);
	}

	[Fact]
	public void NormalizedVerticalScroll_AtTop_ShouldBeZero()
	{
		var scrollView = new ScrollView();

		scrollView.NormalizedVerticalScroll.Should().Be(0f);
	}

	[Fact]
	public void NormalizedVerticalScroll_AtBottom_ShouldBeOne()
	{
		var scrollView = new ScrollView();

		scrollView.ScrollTo(0, -200);
		scrollView.NormalizedVerticalScroll.Should().BeApproximately(1f, 0.01f);
	}

	[Fact]
	public void NormalizedVerticalScroll_ContentFitsInView_ShouldBeZero()
	{
		var scrollView = new ScrollView();
		scrollView.ContentSize = (200, 100); // smaller than Size.Y

		scrollView.NormalizedVerticalScroll.Should().Be(0f);
	}
}
