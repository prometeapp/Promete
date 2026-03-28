using FluentAssertions;
using Promete.Input;
using Promete.UI;

namespace Promete.Test.UI;

public class InputMapTests
{
	[Fact]
	public void AddAction_ShouldCreateAction()
	{
		var map = new InputMap();
		var action = map.AddAction("jump");

		action.Name.Should().Be("jump");
		action.IsPressed.Should().BeFalse();
		action.IsJustPressed.Should().BeFalse();
		action.IsJustReleased.Should().BeFalse();
	}

	[Fact]
	public void GetAction_ShouldReturnSameInstance()
	{
		var map = new InputMap();
		var created = map.AddAction("attack");
		var retrieved = map.GetAction("attack");

		retrieved.Should().BeSameAs(created);
	}

	[Fact]
	public void HasAction_ShouldReturnCorrectly()
	{
		var map = new InputMap();
		map.AddAction("jump");

		map.HasAction("jump").Should().BeTrue();
		map.HasAction("attack").Should().BeFalse();
	}

	[Fact]
	public void Fire_ShouldActivateForOneFrame()
	{
		var map = new InputMap();
		var action = map.AddAction("submit");

		// Fire を呼んでから Update すると1フレーム pressed
		map.Fire("submit");
		map.UpdateWithoutInput();

		action.IsPressed.Should().BeTrue();
		action.IsJustPressed.Should().BeTrue();

		// 次の Update で自動リセット
		map.UpdateWithoutInput();

		action.IsPressed.Should().BeFalse();
		action.IsJustReleased.Should().BeTrue();

		// さらに次の Update
		map.UpdateWithoutInput();
		action.IsJustReleased.Should().BeFalse();
	}

	[Fact]
	public void SetPressed_ShouldHoldState()
	{
		var map = new InputMap();
		var action = map.AddAction("move");

		map.SetPressed("move", true);
		map.UpdateWithoutInput();

		action.IsPressed.Should().BeTrue();
		action.IsJustPressed.Should().BeTrue();

		// 2フレーム目もまだ押し続け
		map.UpdateWithoutInput();
		action.IsPressed.Should().BeTrue();
		action.IsJustPressed.Should().BeFalse();

		// 解除
		map.SetPressed("move", false);
		map.UpdateWithoutInput();

		action.IsPressed.Should().BeFalse();
		action.IsJustReleased.Should().BeTrue();
	}

	[Fact]
	public void ClearProgrammaticState_ShouldRemoveOverride()
	{
		var map = new InputMap();
		var action = map.AddAction("dash");

		map.SetPressed("dash", true);
		map.UpdateWithoutInput();
		action.IsPressed.Should().BeTrue();

		map.ClearProgrammaticState("dash");
		map.UpdateWithoutInput();
		action.IsPressed.Should().BeFalse();
	}

	[Fact]
	public void UnbindAll_ShouldRemoveAllBindings()
	{
		var map = new InputMap();
		map.AddAction("jump");
		map.BindKey("jump", KeyCode.Space);
		map.BindKey("jump", KeyCode.Up);

		map.UnbindAll("jump");

		// バインドが解除されているので、Fire しなければ pressed にならない
		map.UpdateWithoutInput();
		map.GetAction("jump").IsPressed.Should().BeFalse();
	}

	[Fact]
	public void MethodChaining_ShouldWork()
	{
		var map = new InputMap();
		map.AddAction("jump");

		var result = map
			.BindKey("jump", KeyCode.Space)
			.BindMouseButton("jump", MouseButtonType.Left)
			.BindGamepadButton("jump", GamepadButtonType.A);

		result.Should().BeSameAs(map);
	}
}

/// <summary>
/// テスト用のヘルパー拡張。物理入力なしで Update を呼べるようにする。
/// </summary>
internal static class InputMapTestExtensions
{
	/// <summary>
	/// 物理入力なしで Update サイクルを実行します。
	/// プログラマティック入力（Fire / SetPressed）のみが評価されます。
	/// </summary>
	public static void UpdateWithoutInput(this InputMap map)
	{
		// Keyboard/Mouse が null だと EvaluateBinding でクラッシュするが、
		// バインドがなければ問題ない。バインドありのテストは統合テストで行う。
		map.Update(null!, null!);
	}
}
