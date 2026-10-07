import { startPromete } from './_content/Promete.Web/promete.js';

const log = document.getElementById('log');

// フレームで起きた例外は window.frameErrors に記録する (スモークテスト用)
window.frameErrors = [];

await startPromete({
    onProgress: ({ phase, loaded, total }) => {
        log.textContent = `${phase}... ${loaded}/${total}`;
        console.debug(`[progress] ${phase} ${loaded}/${total}`);
    },
    onReady: () => {
        log.textContent = 'ok';
        window.started = true;
    },
    onError: error => {
        window.frameErrors.push(error);
        log.textContent = error;
        console.error(error);
    },
});
