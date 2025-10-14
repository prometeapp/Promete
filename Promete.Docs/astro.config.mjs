import {defineConfig} from 'astro/config';
import starlight from '@astrojs/starlight';
import rehypeMermaid from 'rehype-mermaid';

// https://astro.build/config
export default defineConfig({
  integrations: [starlight({
    title: 'Promete',
    description: 'Prometeは、2Dゲームの開発に特化した、.NET向けのゲームエンジンです。',
    defaultLocale: 'root',
    locales: {
      root: {
        lang: 'ja',
        label: '日本語'
      }
    },
    social: {
      github: 'https://github.com/PrometeApp/Promete',
    },
    logo: {
      src: './assets/logo.png',
      alt: 'Promete',
    },
    customCss: [
      '@/styles/global.scss',
    ],
    head: [
      {tag: 'link', attrs: {rel: 'stylesheet', href: 'https://koruri.chillout.chat/koruri.css'}},
    ],
    components: {
      Head: '@/components/Head.astro',
    },
    sidebar: [
      {
        label: '入門編',
        autogenerate: {directory: 'guide/intro'},
      },
      {
        label: '機能編',
        items: [
          {
            label: 'コア',
            autogenerate: {directory: 'guide/manual'},
          },
          {
            label: 'グラフィック',
            autogenerate: {directory: 'guide/graphics'},
          },
          {
            label: 'テキスト',
            autogenerate: {directory: 'guide/text'},
          },
          {
            label: '入力',
            autogenerate: {directory: 'guide/input'},
          },
          {
            label: 'オーディオ',
            autogenerate: {directory: 'guide/audio'},
          },
          {
            label: '数学',
            autogenerate: {directory: 'guide/math'},
          },
          {
            label: 'その他',
            autogenerate: {directory: 'guide/other'},
          },
        ],
      },
      {
        label: 'プラグイン',
        autogenerate: {directory: 'guide/plugins'},
      },
      {
        label: 'エンジンを拡張する',
        autogenerate: {directory: 'guide/extends'},
      },
    ],
  })],
  markdown: {
    rehypePlugins: [
      [rehypeMermaid, {strategy: 'pre-mermaid' }],
    ],
  },
  vite: {
    resolve: {
      alias: {
        '@': '/src',
      },
    },
  },
});
