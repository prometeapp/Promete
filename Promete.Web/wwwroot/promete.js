// Promete.Web の起動と描画ループ。
//
// ページは startPromete() を呼ぶだけでよい。ランタイムの作成、アセットの配置、Main の実行を順に行う。
// ゲームを組み立てるのは C# の Main で、Main が Run を呼ぶと startLoop() で描画ループが始まる。

const assetManifest = 'promete-assets.json';
const maxRepeatedErrors = 30;

let frame = null;
let onError = null;
let onReady = null;

/**
 * 起動の進み具合。phase は次の順に進む。
 * - 'runtime': .NET のランタイムとアセンブリの取得。loaded / total はファイルの個数。
 * - 'assets': PrometeAsset で宣言したアセットの取得。loaded / total はファイルの個数。
 * - 'starting': C# の Main と、最初のシーンの OnStart の実行。loaded / total は 0。
 * @typedef {{ phase: 'runtime' | 'assets' | 'starting', loaded: number, total: number }} PrometeProgress
 */

/**
 * Promete のアプリを起動する。
 * @param {object} [options]
 * @param {(progress: PrometeProgress) => void} [options.onProgress] 起動の進み具合。
 * @param {() => void} [options.onReady] 最初のフレームを描画し終えたとき。読み込み表示を消すのに使う。
 * @param {(error: string) => void} [options.onError] 起動時、またはフレームで起きた例外。
 */
export async function startPromete(options = {}) {
    onError = options.onError ?? (error => console.error(error));
    onReady = options.onReady ?? null;
    const onProgress = options.onProgress;
    try {
        const { default: createDotnetRuntime } = await import(new URL('_framework/dotnet.js', document.baseURI).href);
        onProgress?.({ phase: 'runtime', loaded: 0, total: 0 });
        const runtime = await createDotnetRuntime({
            onDownloadResourceProgress: (loaded, total) => onProgress?.({ phase: 'runtime', loaded, total }),
        });
        // JSExport は runMain より前に呼べる。パッケージのフォントを取り出すのに使う
        const exports = await runtime.getAssemblyExports('Promete.Web.dll');
        await preloadAssets(runtime.Module.FS, exports.Promete.Web.PrometeWeb, onProgress);
        frame = exports.Promete.Web.PrometeWeb.Frame;

        onProgress?.({ phase: 'starting', loaded: 0, total: 0 });
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
            if (onReady) {
                const ready = onReady;
                onReady = null;
                ready();
            }
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

/**
 * promete-assets.json (version 1) に載ったアセットを起動前に用意し、C# の同期 API (Load(path)) から読めるよう仮想 FS に置く。
 * - file: 仮想 FS に置く。
 * - font: 仮想 FS に置き、FontFace にも登録する (ファミリー名は仮想 FS 上の絶対パス)。
 * - package: パッケージを仮想 FS のルートに置き、中のフォントを C# に取り出させて FontFace に登録する。
 * - embedded-package: 取得しない。アセンブリに埋め込まれたパッケージの中のフォントだけを取り出して登録する。
 * マニフェストそのものもルートに置く (C# のローダーが読む)。マニフェストが無ければ何もしない (古い Promete.Web)。
 * @param {object} fs 仮想ファイルシステム (runtime.Module.FS)。
 * @param {object} web Promete.Web の PrometeWeb の JSExport (PreparePackageFonts / PrepareEmbeddedPackageFonts)。
 * @param {(progress: PrometeProgress) => void} [onProgress]
 */
async function preloadAssets(fs, web, onProgress) {
    const response = await fetch(new URL(assetManifest, document.baseURI));
    if (!response.ok) return;

    const bytes = new Uint8Array(await response.arrayBuffer());
    const manifest = JSON.parse(new TextDecoder().decode(bytes));
    if (manifest.version !== 1) throw new Error(`未対応のアセットのマニフェストです (version: ${manifest.version})`);
    fs.writeFile('/' + assetManifest, bytes);

    // 進捗は取得するエントリの数で数える (embedded-package は取得しない)
    const fetched = manifest.entries.filter(entry => entry.kind !== 'embedded-package');
    let loaded = 0;
    onProgress?.({ phase: 'assets', loaded: 0, total: fetched.length });
    await Promise.all(manifest.entries.map(async entry => {
        if (entry.kind === 'embedded-package') {
            for (const path of web.PrepareEmbeddedPackageFonts(entry.assembly, entry.resource)) {
                await loadFontFile(fs, path);
            }
            return;
        }

        const res = await fetch(new URL(entry.url, document.baseURI));
        if (!res.ok) throw new Error(`アセットを取得できませんでした: ${entry.url} (${res.status})`);
        const buffer = await res.arrayBuffer();

        fs.mkdirTree(entry.path.substring(0, entry.path.lastIndexOf('/')) || '/');
        fs.writeFile(entry.path, new Uint8Array(buffer));
        if (entry.kind === 'font') {
            document.fonts.add(await new FontFace(entry.path, buffer).load());
        } else if (entry.kind === 'package') {
            // パッケージの中のフォントは C# が取り出す。ルートからの相対パスを渡す
            for (const path of web.PreparePackageFonts(entry.path.substring(1))) {
                await loadFontFile(fs, path);
            }
        }

        onProgress?.({ phase: 'assets', loaded: ++loaded, total: fetched.length });
    }));
}

// 仮想 FS 上のフォントを FontFace に登録し、読み込みの完了を待つ。ファミリー名はパス。
async function loadFontFile(fs, path) {
    document.fonts.add(await new FontFace(path, fs.readFile(path)).load());
}
