// DOM の入力イベントを溜めておき、C# が毎フレームの先頭で drain() でまとめて取り出す。
// イベントは "種類:引数" の文字列で表す。
//   kd:<code> / ku:<code>   キーの押下 / 解放 (KeyboardEvent.code)
//   ch:<text>               文字の入力
//   mm:<x>:<y>              マウスの移動 (canvas のピクセル座標)
//   md:<button> / mu:<button> マウスボタンの押下 / 解放 (MouseEvent.button)
//   wh:<dx>:<dy>            ホイール (1 ノッチを 1 とし、上方向が正)

const queue = [];
const preventedKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Space', 'Tab']);

/** canvas と window にイベントを登録する。C# の WebBackend の初期化時に呼ばれる。 */
export function attach(selector) {
    const canvas = document.querySelector(selector);
    if (!canvas) throw new Error(`canvas が見つかりません: ${selector}`);

    window.addEventListener('keydown', e => {
        if (!e.repeat) queue.push(`kd:${e.code}`);
        if (e.key.length === 1) queue.push(`ch:${e.key}`);
        if (preventedKeys.has(e.code)) e.preventDefault();
    });
    window.addEventListener('keyup', e => queue.push(`ku:${e.code}`));

    // canvas の表示サイズと描画バッファのサイズが違っても、描画バッファのピクセル座標で渡す
    const position = e => {
        const rect = canvas.getBoundingClientRect();
        const x = (e.clientX - rect.left) * canvas.width / rect.width;
        const y = (e.clientY - rect.top) * canvas.height / rect.height;
        return `${x}:${y}`;
    };
    canvas.addEventListener('pointermove', e => queue.push(`mm:${position(e)}`));
    canvas.addEventListener('pointerdown', e => {
        canvas.setPointerCapture(e.pointerId);
        queue.push(`mm:${position(e)}`, `md:${e.button}`);
    });
    canvas.addEventListener('pointerup', e => queue.push(`mm:${position(e)}`, `mu:${e.button}`));
    canvas.addEventListener('contextmenu', e => e.preventDefault());
    canvas.addEventListener('wheel', e => {
        // deltaMode: 0 = ピクセル (1 ノッチを約 100 とみなす)、1 = 行 (1 ノッチを 3 行とみなす)、2 = ページ
        const unit = e.deltaMode === 0 ? 100 : e.deltaMode === 1 ? 3 : 1;
        queue.push(`wh:${-e.deltaX / unit}:${-e.deltaY / unit}`);
        e.preventDefault();
    }, { passive: false });
}

/** 溜まったイベントを取り出す。 */
export function drain() {
    return queue.splice(0, queue.length);
}
