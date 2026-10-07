// Promete.Web が配る既定の起動スクリプト。プロジェクトに wwwroot/main.js を置くと、そちらが使われる。
import { startPromete } from './_content/Promete.Web/promete.js';

const status = document.getElementById('promete-status');
let failed = false;

await startPromete({
    onProgress: (loaded, total) => {
        if (status) status.textContent = `読み込み中... ${loaded}/${total}`;
    },
    onError: error => {
        failed = true;
        console.error(error);
        if (status) status.textContent = error;
    },
});

if (status && !failed) status.textContent = '';
