import {docsLoader, i18nLoader} from '@astrojs/starlight/loaders';
import {docsSchema, i18nSchema} from '@astrojs/starlight/schema';
import {defineCollection} from 'astro:content';
import {docsVersionsLoader} from 'starlight-versions/loader';
import {z} from "astro/zod";

export const collections = {
  docs: defineCollection({loader: docsLoader(), schema: docsSchema()}),
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
