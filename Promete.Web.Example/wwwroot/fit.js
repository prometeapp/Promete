// canvas がビューポートより大きいときに、縦横比を保ったまま縮めて収める。
// スマートフォンの縦画面などで、640x480 が見切れないようにする。拡大はしない。
//
// canvas の表示サイズは、ゲーム側 (Promete.Web) が style に書き込む。ここでは !important で上書きする。
// 表示サイズ = 描画バッファのサイズ (Scale 1) の前提。
const canvas = document.getElementById('canvas');

const fit = () => {
    const { width, height } = canvas;
    if (!width || !height) return;

    const ratio = Math.min(1, window.innerWidth / width, window.innerHeight / height);
    canvas.style.setProperty('width', `${Math.floor(width * ratio)}px`, 'important');
    canvas.style.setProperty('height', `${Math.floor(height * ratio)}px`, 'important');
};

window.addEventListener('resize', fit);
// ゲームが canvas の大きさを変えたとき (width / height 属性) にも追従する
new MutationObserver(fit).observe(canvas, { attributes: true, attributeFilter: ['width', 'height'] });
fit();
