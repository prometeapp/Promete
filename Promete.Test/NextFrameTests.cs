using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

public class NextFrameTests
{
    /// <summary>
    /// PrometeAppのOnUpdateメソッドを呼び出すヘルパーメソッド
    /// </summary>
    private static void InvokeOnUpdate(PrometeApp app)
    {
        var onUpdateMethod = typeof(PrometeApp).GetMethod("OnUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        onUpdateMethod.Invoke(app, null);
    }

    [Fact]
    public void NextFrame_ShouldExecuteInNextFrame_NotCurrentFrame()
    {
        // このテストでは、NextFrameが次のフレームで実行されることを確認します
        // NextFrame内のアクションは、次のOnUpdate呼び出しの開始時に実行されるべきです
        
        var executionOrder = new List<string>();
        
        var app = PrometeApp.Create()
            .BuildWithHeadless();
        
        // フレーム1: NextFrameを呼び出す
        executionOrder.Add("Frame1_Start");
        app.NextFrame(() => executionOrder.Add("NextFrame_Action"));
        executionOrder.Add("Frame1_End");
        
        // この時点でNextFrameActionはまだ実行されていないはず
        executionOrder.Should().NotContain("NextFrame_Action", "NextFrame should not execute in the same frame");
        
        // フレーム2: OnUpdateを呼び出す（NextFrameのアクションが実行される）
        executionOrder.Add("Frame2_BeforeUpdate");
        InvokeOnUpdate(app);
        executionOrder.Add("Frame2_AfterUpdate");
        
        // フレーム2でNextFrameアクションが実行されたはず
        executionOrder.Should().Contain("NextFrame_Action", "NextFrame should execute in the next frame");
        
        // 実行順序を確認
        string[] expectedOrder = [
            "Frame1_Start",
            "Frame1_End",
            "Frame2_BeforeUpdate",
            "NextFrame_Action",    // OnUpdateの最初に実行される
            "Frame2_AfterUpdate"
        ];
        
        executionOrder.Should().Equal(expectedOrder);
    }
    
    [Fact]
    public void NextFrame_MultipleActions_ShouldExecuteInOrder()
    {
        // 複数のNextFrameアクションが正しい順序で実行されることを確認
        
        var executionOrder = new List<string>();
        
        var app = PrometeApp.Create()
            .BuildWithHeadless();
        
        // 複数のNextFrameアクションをエンキュー
        app.NextFrame(() => executionOrder.Add("Action1"));
        app.NextFrame(() => executionOrder.Add("Action2"));
        app.NextFrame(() => executionOrder.Add("Action3"));
        
        // まだ実行されていない
        executionOrder.Should().BeEmpty();
        
        // 次のフレームで実行
        InvokeOnUpdate(app);
        
        // すべてのアクションが順序通りに実行される
        executionOrder.Should().Equal(["Action1", "Action2", "Action3"]);
    }
}
