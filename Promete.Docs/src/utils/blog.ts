import {getCollection, type CollectionEntry} from 'astro:content';

export type BlogPost = CollectionEntry<'blog'>;

const dateFormat = new Intl.DateTimeFormat('ja-JP', {dateStyle: 'long', timeZone: 'UTC'});

/**
 * 記事を新しい順に取得する。下書きは開発サーバーでのみ含める。
 */
export async function getBlogPosts(): Promise<BlogPost[]> {
  const posts = await getCollection('blog', ({data}) => import.meta.env.DEV || !data.draft);
  return posts.sort((a, b) => b.data.date.valueOf() - a.data.date.valueOf());
}

/**
 * 記事の URL を返す。
 */
export function getBlogPostUrl(post: BlogPost): string {
  return `/blog/${post.id}/`;
}

/**
 * 公開日を「2026年10月6日」の形式にする。
 */
export function formatBlogDate(date: Date): string {
  // frontmatter の日付は UTC の 0 時として読み込まれるため、UTC で表示して日付のずれを防ぐ
  return dateFormat.format(date);
}
