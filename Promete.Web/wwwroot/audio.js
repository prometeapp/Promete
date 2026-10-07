// Web Audio への出力。.NET 側が生成した PCM を AudioBuffer にして、途切れないよう順に予約再生する。

let ctx = null;
let analyser = null;
let output = null;
let channels = 2;
let rate = 44100;
let nextTime = 0;
let sources = [];

// AudioContext を必要になった時点で作る。ストリーム出力とワンショット再生で共有する
function ensureContext(sampleRate) {
    if (ctx) return;
    ctx = sampleRate ? new AudioContext({ sampleRate }) : new AudioContext();
    analyser = ctx.createAnalyser();
    analyser.fftSize = 2048;
    output = ctx.createGain();
    output.connect(analyser);
    analyser.connect(ctx.destination);
    nextTime = 0;

    // ブラウザの自動再生ポリシー: ユーザー操作があるまで AudioContext は suspended のまま
    const resume = () => { if (ctx && ctx.state !== 'running') ctx.resume(); };
    for (const type of ['pointerdown', 'keydown', 'mousedown', 'touchstart'])
        window.addEventListener(type, resume);
}

export function start(sampleRate, channelCount) {
    rate = sampleRate;
    channels = channelCount;
    ensureContext(sampleRate);
}

// 再生中のワンショットの終了を待つ Promise。id で引く
const oneShots = new Map();
let nextOneShotId = 1;

// interleaved float32 PCM を 1 回だけ再生し、id を返す。終了は oneShotEnded(id) で待つ
// (MemoryView は Promise を返す関数に渡せないため、開始と待機を分けている)
export function playOneShot(bytes, frames, channelCount, sampleRate, gain, pitch, pan) {
    ensureContext(0);
    const copy = bytes.slice();
    const samples = new Float32Array(copy.buffer, copy.byteOffset, copy.byteLength / 4);
    const buffer = ctx.createBuffer(channelCount, frames, sampleRate);
    for (let c = 0; c < channelCount; c++) {
        const data = buffer.getChannelData(c);
        for (let i = 0; i < frames; i++) data[i] = samples[i * channelCount + c];
    }

    const source = ctx.createBufferSource();
    source.buffer = buffer;
    source.playbackRate.value = pitch;
    const gainNode = ctx.createGain();
    gainNode.gain.value = gain;
    const panner = ctx.createStereoPanner();
    panner.pan.value = pan;
    source.connect(gainNode).connect(panner).connect(output);
    const id = nextOneShotId++;
    oneShots.set(id, new Promise(resolve => { source.onended = () => resolve(); }));
    source.start();
    return id;
}

export function oneShotEnded(id) {
    const promise = oneShots.get(id) ?? Promise.resolve();
    oneShots.delete(id);
    return promise;
}

// bytes: float32 の interleaved PCM を、メモリビュー (MemoryView) で受け取る
export function enqueue(bytes, frames, pitch) {
    // MemoryView は wasm メモリを直接指すラッパーで、呼び出しの間だけ有効。slice() でコピーを取る
    const copy = bytes.slice();
    const samples = new Float32Array(copy.buffer, copy.byteOffset, copy.byteLength / 4);
    const buffer = ctx.createBuffer(channels, frames, rate);
    for (let c = 0; c < channels; c++) {
        const data = buffer.getChannelData(c);
        for (let i = 0; i < frames; i++) data[i] = samples[i * channels + c];
    }

    const source = ctx.createBufferSource();
    source.buffer = buffer;
    source.playbackRate.value = pitch;
    source.connect(output);
    const startTime = Math.max(nextTime, ctx.currentTime + 0.02);
    source.start(startTime);
    nextTime = startTime + buffer.duration / pitch;
    sources.push(source);
    source.onended = () => { sources = sources.filter(s => s !== source); };
}

export const queuedSeconds = () => (ctx ? Math.max(0, nextTime - ctx.currentTime) : 0);

export function flush() {
    for (const s of sources) { try { s.stop(); } catch { /* 既に停止済み */ } }
    sources = [];
    nextTime = 0;
}

export function stop() {
    flush();
    if (ctx) { ctx.close(); ctx = null; }
}

export const state = () => (ctx ? ctx.state : 'none');
export const currentTime = () => (ctx ? ctx.currentTime : 0);

// 直近の出力の RMS。無音でないことの確認用
export function rms() {
    if (!analyser) return 0;
    const data = new Float32Array(analyser.fftSize);
    analyser.getFloatTimeDomainData(data);
    let sum = 0;
    for (const v of data) sum += v * v;
    return Math.sqrt(sum / data.length);
}
