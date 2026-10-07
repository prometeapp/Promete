import { dotnet } from './_framework/dotnet.js';

const log = document.getElementById('log');
const query = new URLSearchParams(location.search);
const { getAssemblyExports, getConfig } = await dotnet.create();
await dotnet.run;
const exports = await getAssemblyExports(getConfig().mainAssemblyName);

// 宣言されたアセット (assets/manifest.txt) は、同期 API (Load(path)) から読めるよう、
// シーン開始前にすべて fetch して仮想ファイルシステムの /assets に置く。
// フォント (.ttf) はあわせて FontFace に登録し、パス名 (/assets/xxx.ttf) をファミリー名にする。
async function preloadAssets() {
    const manifest = (await (await fetch('assets/manifest.txt')).text()).split(/\r?\n/).filter(Boolean);
    let loaded = 0;
    await Promise.all(manifest.map(async name => {
        const response = await fetch(`assets/${name}`);
        if (!response.ok) throw new Error(`fetch failed: ${name} ${response.status}`);
        const buffer = await response.arrayBuffer();
        exports.Program.WriteAsset(`/assets/${name}`, new Uint8Array(buffer));
        if (name.toLowerCase().endsWith('.ttf')) {
            document.fonts.add(await new FontFace(`/assets/${name}`, buffer).load());
            if (name === 'MisakiGothic.ttf') document.fonts.add(await new FontFace('Misaki', buffer).load());
        }
        log.textContent = `loading assets... ${++loaded}/${manifest.length}`;
    }));
}

const t0 = performance.now();
await preloadAssets();
console.log(`assets loaded in ${(performance.now() - t0).toFixed(0)} ms`);

// テキストは FreeType のネイティブが無いので、ブラウザの Canvas2D で描く。そのための JS モジュールを用意する
await exports.Program.ImportGlyphModule(new URL('./canvasGlyph.js', location.href).href);
await exports.Program.ImportAudioModule(new URL('./webAudio.js', location.href).href);
window.promete = exports.Program;

// DOM の入力イベントを Promete に渡す
const canvas = document.getElementById('canvas');
window.addEventListener('keydown', e => { exports.Program.OnKey(e.code, true); if (e.key.length === 1) exports.Program.OnChar(e.key); if (e.code.startsWith('Arrow') || e.code === 'Space') e.preventDefault(); });
window.addEventListener('keyup', e => exports.Program.OnKey(e.code, false));
canvas.addEventListener('mousemove', e => exports.Program.OnMouseMove(e.offsetX, e.offsetY));
canvas.addEventListener('mousedown', e => exports.Program.OnMouseButton(e.button, true));
canvas.addEventListener('mouseup', e => exports.Program.OnMouseButton(e.button, false));
canvas.addEventListener('contextmenu', e => e.preventDefault());
canvas.addEventListener('wheel', e => { exports.Program.OnWheel(e.deltaX, e.deltaY); e.preventDefault(); }, { passive: false });

const result = exports.Program.Start('#canvas', query.get('scene') ?? 'example', query.get('f') ?? '', location.href);
log.textContent = result;

// ?demo=<パス> で、Promete.Example のデモを直接開く
if (result.startsWith('ok') && query.get('demo')) exports.Program.OpenDemo(query.get('demo'));
window.started = true;

// フレームで起きた例外は window.frameErrors に記録する。同じ例外が続くとき (回復できないとき) だけ止める
window.frameErrors = [];
if (result.startsWith('ok')) {
    let repeated = 0;
    const loop = t => {
        const error = exports.Program.Frame(t);
        if (error) {
            const last = window.frameErrors.at(-1);
            repeated = last && last.error === error ? repeated + 1 : 0;
            if (repeated === 0) window.frameErrors.push({ t, error });
            log.textContent = error;
            if (repeated > 30) return;
        } else {
            repeated = 0;
        }

        requestAnimationFrame(loop);
    };
    requestAnimationFrame(loop);
}
