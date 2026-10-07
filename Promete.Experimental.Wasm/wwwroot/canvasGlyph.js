// ブラウザの Canvas2D でグリフをラスタライズする。IGlyphSource (CanvasGlyphSource) から呼ばれる。

const canvas = document.createElement('canvas');
const ctx = canvas.getContext('2d', { willReadFrequently: true });
let last = { width: 0, height: 0, bearingX: 0, bearingY: 0, pixels: new Uint8Array(0) };

const fontSpec = (family, size, bold, italic) =>
    `${italic ? 'italic ' : ''}${bold ? 'bold ' : ''}${size}px "${family}"`;

export function metrics(family, size, bold, italic) {
    ctx.font = fontSpec(family, size, bold, italic);
    const m = ctx.measureText('M');
    return [m.fontBoundingBoxAscent, m.fontBoundingBoxDescent];
}

export function advance(family, size, bold, italic, codepoint) {
    ctx.font = fontSpec(family, size, bold, italic);
    return ctx.measureText(String.fromCodePoint(codepoint)).width;
}

// ラスタライズして結果を last に保持する。空のグリフなら false。
export function rasterize(family, size, bold, italic, codepoint) {
    const font = fontSpec(family, size, bold, italic);
    ctx.font = font;
    const text = String.fromCodePoint(codepoint);
    const m = ctx.measureText(text);
    const left = Math.max(0, Math.ceil(m.actualBoundingBoxLeft)) + 1;
    const right = Math.max(0, Math.ceil(m.actualBoundingBoxRight)) + 1;
    const ascent = Math.max(0, Math.ceil(m.actualBoundingBoxAscent)) + 1;
    const descent = Math.max(0, Math.ceil(m.actualBoundingBoxDescent)) + 1;
    const width = left + right;
    const height = ascent + descent;
    if (width <= 2 || height <= 2) return false;

    canvas.width = width;
    canvas.height = height;
    ctx.font = font;
    ctx.textBaseline = 'alphabetic';
    ctx.fillStyle = '#fff';
    ctx.fillText(text, left, ascent);

    const data = ctx.getImageData(0, 0, width, height).data;
    const pixels = new Uint8Array(data.length);
    for (let i = 0; i < data.length; i += 4) {
        pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
        pixels[i + 3] = data[i + 3];
    }

    last = { width, height, bearingX: -left, bearingY: -ascent, pixels };
    return true;
}

export const lastWidth = () => last.width;
export const lastHeight = () => last.height;
export const lastBearingX = () => last.bearingX;
export const lastBearingY = () => last.bearingY;
export const lastPixels = () => last.pixels;
