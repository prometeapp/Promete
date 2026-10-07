// Promete.Web の起動と描画ループ。
//
// ページは startPromete() を呼ぶだけでよい。ランタイムの作成、アセットの配置、Main の実行を順に行う。
// ゲームを組み立てるのは C# の Main で、Main が Run を呼ぶと startLoop() で描画ループが始まる。

const assetManifest = 'promete-assets.json';
const maxRepeatedErrors = 30;

let frame = null;
let onError = null;

/**
 * Promete のアプリを起動する。
 * @param {object} [options]
 * @param {(loaded: number, total: number) => void} [options.onProgress] アセットの読み込みの進捗。
 * @param {(error: string) => void} [options.onError] 起動時、またはフレームで起きた例外。
 */
export async function startPromete(options = {}) {
    onError = options.onError ?? (error => console.error(error));
    try {
        const { dotnet } = await import(new URL('_framework/dotnet.js', document.baseURI).href);
        const runtime = await dotnet.create();
        await preloadAssets(runtime.Module.FS, options.onProgress);

        const exports = await runtime.getAssemblyExports('Promete.Web.dll');
        frame = exports.Promete.Web.PrometeWeb.Frame;

        // run() は Main が戻るとランタイムを終了するので、常駐するゲームでは runMain() を使う
        await runtime.runMain();
    } catch (e) {
        onError(String(e?.stack ?? e));
        throw e;
    }
}

/** 描画ループを始める。C# の WebBackend.OnStart から呼ばれる。 */
export function startLoop() {
    let lastError = null;
    let repeated = 0;
    const loop = time => {
        const result = frame(time);
        if (result === '') return; // ゲームが終了した
        if (result) {
            repeated = result === lastError ? repeated + 1 : 0;
            if (repeated === 0) onError(result);
            lastError = result;
            // 同じ例外が続く (回復できない) ときは止める
            if (repeated >= maxRepeatedErrors) return;
        } else {
            lastError = null;
            repeated = 0;
        }
        requestAnimationFrame(loop);
    };
    requestAnimationFrame(loop);
}

/** canvas の描画バッファと表示サイズを設定する。拡大はピクセルを保ったまま行う。 */
export function setCanvasSize(selector, width, height) {
    const canvas = document.querySelector(selector);
    canvas.width = width;
    canvas.height = height;
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;
    canvas.style.imageRendering = 'pixelated';
}

/** ドキュメントのタイトルを設定する。 */
export function setTitle(title) {
    document.title = title;
}

// マニフェストに載ったアセットを並列に取得し、同期 API (Load(path)) から読めるよう仮想 FS に置く。
// フォントは FontFace にも登録する。ファミリー名は仮想 FS 上の絶対パス。
async function preloadAssets(fs, onProgress) {
    const response = await fetch(new URL(assetManifest, document.baseURI));
    if (!response.ok) return;

    const assets = await response.json();
    let loaded = 0;
    onProgress?.(0, assets.length);
    await Promise.all(assets.map(async asset => {
        const res = await fetch(new URL(asset.url, document.baseURI));
        if (!res.ok) throw new Error(`アセットを取得できませんでした: ${asset.url} (${res.status})`);
        const buffer = await res.arrayBuffer();

        fs.mkdirTree(asset.path.substring(0, asset.path.lastIndexOf('/')) || '/');
        fs.writeFile(asset.path, new Uint8Array(buffer));
        if (asset.kind === 'font') document.fonts.add(await new FontFace(asset.path, buffer).load());

        onProgress?.(++loaded, assets.length);
    }));
}
