using Promete;
using Promete.Coroutines;
using Promete.Headless;
using Promete.HeadlessTest;

var app = PrometeApp.Create()
    .Use<CoroutineManager>()
    .BuildWithHeadless();

return app.Run<MainScene>();
