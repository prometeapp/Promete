using Xunit;

// PrometeApp.Current がプロセスグローバルであるため、テストクラスの並列実行を無効化する。
// 並列実行すると、あるテストの PrometeApp が他のテストの AudioPlayer 等に「カレント」として観測され、
// イベント配送先が化けるフレーキーの原因になる。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
