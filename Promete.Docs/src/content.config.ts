import {docsLoader, i18nLoader} from '@astrojs/starlight/loaders';
import {docsSchema, i18nSchema} from '@astrojs/starlight/schema';
import {defineCollection} from 'astro:content';
import {glob} from 'astro/loaders';
import {docsVersionsLoader} from 'starlight-versions/loader';
import {z} from "astro/zod";

export const collections = {
  docs: defineCollection({loader: docsLoader(), schema: docsSchema()}),
  // ブログ記事。ファイルの src/content/blog からの相対パスが URL (/blog/<パス>/) になる
  blog: defineCollection({
    loader: glob({pattern: '**/*.{md,mdx}', base: './src/content/blog'}),
    schema: z.object({
      title: z.string(),
      description: z.string().optional(),
      // 公開日
      date: z.coerce.date(),
      authors: z.array(z.string()).optional(),
      // true の記事は開発サーバーでのみ表示する
      draft: z.boolean().default(false),
    }),
  }),
  versions: defineCollection({loader: docsVersionsLoader()}),
  i18n: defineCollection({
    loader: i18nLoader(),
    schema: i18nSchema({
      extend: z.object({
        "starlightVersions.link.latest": z.string().optional(),
        "starlightVersions.outdated.label": z.string().optional(),
        "starlightVersions.outdated.slug": z.string().optional(),
        "starlightVersions.search.link.latest": z.string().optional(),
        "starlightVersions.search.outdated.label": z.string().optional(),
        "starlightVersions.search.outdated.slug": z.string().optional(),
        "starlightVersions.select.accessibleLabel": z.string().optional(),
      }),
    }),
  }),
};
