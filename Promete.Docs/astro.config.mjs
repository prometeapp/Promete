import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
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
    logo: {
      src: './assets/logo.png',
      alt: 'シュリンピア',
    },
    customCss: [
      '@/styles/global.scss',
    ],
	  head: [
		  { tag: 'link', attrs: { rel: 'stylesheet', href: 'https://koruri.chillout.chat/koruri.css' } },
	  ],
	  sidebar: [
      {
        label: '入門編',
        autogenerate: { directory: 'guide/intro' },
      },
		  {
			  label: '基本編',
			  autogenerate: { directory: 'guide/basic' },
		  },
		  {
			  label: 'ノード',
			  autogenerate: { directory: 'guide/nodes' },
		  },
      {
        label: 'ユーティリティ',
        autogenerate: { directory: 'guide/utilities' },
      },
		  {
			  label: '機能詳細',
			  autogenerate: { directory: 'guide/features' },
		  },
		  {
			  label: 'エンジンを拡張する',
			  autogenerate: { directory: 'guide/extends' },
		  },
		  {
			  label: '旧バージョンからの移行',
			  autogenerate: { directory: 'guide/migrate' },
		  },
	  ],
  })],
  vite: {
    resolve: {
      alias: {
        '@': '/src',
      },
    },
	},
});
