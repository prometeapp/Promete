import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "Promete",
  description: ".NET向け、2Dに特化した、次世代のゲームエンジン。",
  lang: 'ja-JP',

  head: [
    ['link', { rel: 'stylesheet', href: 'https://koruri.chillout.chat/koruri.css' }]
  ],

  markdown: {
    container: {
      tipLabel: '💡 Tips',
      warningLabel: '注意！',
      dangerLabel: '警告！',
      infoLabel: 'Note: ',
      detailsLabel: 'もっと見る',
    }
  },

  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'ガイドブック', link: '/guide/basic/about' }
    ],

    sidebar: [
      {
        text: '入門編',
        collapsed: false,
        items: [
          { text: 'Promete とは？', link: '/guide/basic/about' },
          { text: 'クイックスタート', link: '/guide/basic/getting-started' },
          { text: '"Hello, world!"', link: '/guide/basic/hello-world' },
          { text: 'ウィンドウをカスタマイズする', link: '/guide/basic/customize-window' },
          { text: '画像ファイルを画面に表示する', link: '/guide/basic/use-texture' },
          { text: 'Element とは？', link: '/guide/basic/use-elements' },
          { text: '好きなテキストを表示する', link: '/guide/basic/use-text' },
          { text: '図形を画面に表示する', link: '/guide/basic/use-shape' },
          { text: 'タイルマップ', link: '/guide/basic/use-tilemap' },
          { text: '9スライススプライト', link: '/guide/basic/use-9slice-sprite' },
          { text: 'キーボード入力を読み取る', link: '/guide/basic/input-keyboard' },
          { text: 'マウス入力を読み取る', link: '/guide/basic/input-mouse' },
          { text: 'ゲームパッド入力を読み取る', link: '/guide/basic/input-gamepad' },
          { text: '音声を扱う', link: '/guide/basic/use-audio' },
          { text: 'シーンを管理する', link: '/guide/basic/manage-scenes' },
        ],
      },  
      {
        text: '応用編',
        collapsed: false,
        items: [
          { text: 'コンポーネント', link: '/guide/advanced/use-components' },
          { text: 'ゲームデータの管理', link: '/guide/advanced/manage-data' },
          { text: 'Element を補完移動させる', link: '/guide/advanced/smooth-movement' },
        ],
      },
      {
        text: '機能別のマニュアル',
        items: [
          { text: 'シーンのライフサイクル', link: '/guide/features/scene-lifecycle' },
          { text: 'ウィンドウ', link: '/guide/features/window' },
          { text: 'マウス入力', link: '/guide/features/mouse' },
          { text: 'キーボード入力', link: '/guide/features/keyboard' },
          { text: 'ゲームパッド入力', link: '/guide/features/gamepad' },
          { text: 'ベクトル', link: '/guide/features/vector' },
          { text: 'フォント', link: '/guide/features/font' },
          { text: 'オーディオプレイヤーとソース', link: '/guide/features/audio-player' },
          { text: 'プラグインシステム', link: '/guide/features/plugin-system' },
          { text: 'テクスチャファクトリ', link: '/guide/features/texture-factory' },
          { text: 'コンソールレイヤー', link: '/guide/features/console-layer' },
          { text: 'グリフレンダラー', link: '/guide/features/glyph-renderer' },
        ],
      },
      {
        text: '高度な拡張',
        collapsed: true,
        items: [
          { text: 'カスタムレンダラー', link: '/guide/extends/custom-renderer' },
          { text: 'カスタムのオーディオソース', link: '/guide/extends/custom-audio-source' },
        ],
      },
      {
        text: 'その他',
        collapsed: false,
        items: [
          { text: 'DotFeather v3 からの移行', link: '/guide/migrate/from-dotfeather-v3' },
          { text: 'GitHub リポジトリ', link: 'https://github.com/prometeapp/promete' },
          { text: 'リリースノート', link: 'https://github.com/prometeapp/promete/releases' },
          { text: 'ライセンス', link: 'https://github.com/prometeapp/promete/tree/master/LICENSE' },
        ],
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/prometeapp/promete' }
    ],

    footer: {
      copyright: '(C)2024 Promete Developers',
    }
  }
})
