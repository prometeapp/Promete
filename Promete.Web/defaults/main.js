// Promete.Web が配る既定の起動スクリプト。プロジェクトに wwwroot/main.js を置くと、そちらが使われる。
import { startPromete } from './_content/Promete.Web/promete.js';

const loading = document.getElementById('promete-loading');
const bar = document.getElementById('promete-progress-bar');
const label = document.getElementById('promete-label');
const errorView = document.getElementById('promete-error');

// 段階ごとに、バー全体のうちの範囲を割り当てる。ランタイムがいちばん大きい
const ranges = {
    runtime: [0, 0.7],
    assets: [0.7, 0.95],
    starting: [0.95, 1],
};
const labels = {
    runtime: 'ランタイムを読み込み中',
    assets: 'アセットを読み込み中',
    starting: '起動中',
};

let failed = false;

await startPromete({
    onProgress: ({ phase, loaded, total }) => {
        if (failed) return;
        const [start, end] = ranges[phase];
        const ratio = total > 0 ? loaded / total : 0;
        bar.style.width = `${(start + (end - start) * ratio) * 100}%`;
        label.textContent = total > 0 ? `${labels[phase]}... ${loaded}/${total}` : `${labels[phase]}...`;
    },
    onReady: () => {
        bar.style.width = '100%';
        loading.classList.add('done');
    },
    onError: error => {
        console.error(error);
        errorView.textContent = error;
        if (!loading.classList.contains('done')) {
            failed = true;
            loading.classList.add('failed');
            label.textContent = '起動に失敗しました';
        }
    },
});
