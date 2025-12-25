---
applyTo: "**/*.{ts,tsx,js,jsx}"
description: Next.js 16 and React 19 development guidelines for building performant, type-safe applications
---

# Next.js 16 and React 19 Development Guidelines

Propose clean, organized solutions following App Router conventions. Cover Server/Client Components, data fetching, caching, streaming, and Server Actions. Apply modern React patterns with TypeScript. Optimize for Core Web Vitals and SEO.

## Core Expertise

- **App Router**: File-based routing, layouts, templates, route groups, parallel routes, intercepting routes
- **Cache Components (v16)**: `use cache` directive and Partial Pre-Rendering (PPR)
- **Turbopack (Stable)**: Default bundler with file system caching
- **React Compiler (Stable)**: Automatic memoization without manual `useMemo`/`useCallback`
- **Server & Client Components**: When to use each, composition patterns
- **Data Fetching**: Server Components, fetch with caching, streaming, suspense
- **Advanced Caching**: `updateTag()`, `refresh()`, `revalidateTag()`
- **React 19.2**: View Transitions, `useEffectEvent()`, `<Activity/>`, `useOptimistic`, `useFormStatus`
- **Middleware & Auth**: Protected routes, session management

## Breaking Changes in Next.js 16

- **Async Params**: `params` and `searchParams` are `Promise` types—must await them
- **Turbopack Default**: No manual configuration needed
- **React Compiler**: Built-in automatic memoization
- **Image Defaults**: Updated optimization defaults

## Guidelines

- Always use App Router (`app/` directory)—never Pages Router for new code
- **Await `params` and `searchParams`** in all components (v16 breaking change)
- Use `use cache` directive for components that benefit from caching and PPR
- Mark Client Components with `'use client'` at file top
- Server Components by default—Client Components only for interactivity, hooks, or browser APIs
- TypeScript for all components with typed async `params`, `searchParams`, and metadata
- `next/image` for all images with `width`, `height`, and `alt`
- `loading.tsx` and `<Suspense>` for loading states; `error.tsx` for error boundaries
- Server Actions for mutations instead of API routes when possible
- Metadata API in `layout.tsx` and `page.tsx` for SEO
- `next/font/google` or `next/font/local` at layout level
- Parallel routes `@folder` for modals; intercepting routes `(.)folder` for overlays
- Middleware in `middleware.ts` for auth, redirects, request modification

## Code Design Rules

- Server Components by default; Client Components small and leaf-level
- Named exports for utilities; `export default` for pages
- kebab-case for files, PascalCase for components
- Comments explain **why**, not what
- Reuse existing components; don't add unused code

## Component Patterns

### Server Component

```typescript
// app/posts/page.tsx
async function getPosts() {
  const res = await fetch('https://api.example.com/posts', {
    next: { revalidate: 3600 }
  });
  if (!res.ok) throw new Error('Failed to fetch');
  return res.json();
}

export default async function PostsPage() {
  const posts = await getPosts();
  return <PostList posts={posts} />;
}
```

### Client Component

```typescript
// app/components/counter.tsx
'use client';

import { useState } from 'react';

export function Counter() {
  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(c => c + 1)}>Count: {count}</button>;
}
```

### Dynamic Route (v16 Async Params)

```typescript
// app/posts/[id]/page.tsx
interface PageProps {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

export default async function PostPage({ params }: PageProps) {
  const { id } = await params; // Must await in v16
  const post = await getPost(id);
  return <article>{post.title}</article>;
}

export async function generateMetadata({ params }: PageProps) {
  const { id } = await params;
  const post = await getPost(id);
  return { title: post.title, description: post.excerpt };
}
```

## Data Fetching & Caching

```typescript
// Caching options
await fetch(url, { cache: 'force-cache' });     // Default caching
await fetch(url, { cache: 'no-store' });         // No cache
await fetch(url, { next: { revalidate: 3600 }}); // Time-based
await fetch(url, { next: { tags: ['posts'] }});  // Tag-based

// Cache Component (v16)
'use cache';
export async function ProductList() {
  const products = await getProducts();
  return <div>{/* render */}</div>;
}

// Cache APIs
import { revalidateTag, updateTag, refresh } from 'next/cache';
await revalidateTag('products');
await updateTag(`product-${id}`);
await refresh();
```

## Server Actions

```typescript
// app/actions/create-post.ts
'use server';

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

export async function createPost(formData: FormData) {
  const title = formData.get('title') as string;
  if (!title) return { error: 'Title required' };
  
  await db.post.create({ data: { title } });
  revalidatePath('/posts');
  redirect('/posts');
}
```

## Middleware

```typescript
// middleware.ts
import { NextResponse, NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('auth-token');
  if (request.nextUrl.pathname.startsWith('/dashboard') && !token) {
    return NextResponse.redirect(new URL('/login', request.url));
  }
  return NextResponse.next();
}

export const config = { matcher: ['/dashboard/:path*'] };
```

## Metadata & SEO

```typescript
// app/layout.tsx
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: { default: 'My App', template: '%s | My App' },
  description: 'App description',
  openGraph: { title: 'My App', url: 'https://example.com', type: 'website' },
  twitter: { card: 'summary_large_image' },
};
```

## React 19.2 Features

```typescript
// useFormStatus
'use client';
import { useFormStatus } from 'react-dom';
export function SubmitButton() {
  const { pending } = useFormStatus();
  return <button disabled={pending}>{pending ? 'Submitting...' : 'Submit'}</button>;
}

// useOptimistic
import { useOptimistic } from 'react';
const [optimistic, addOptimistic] = useOptimistic(data, (state, newItem) => [...state, newItem]);

// View Transitions
if (document.startViewTransition) {
  document.startViewTransition(() => startTransition(() => router.push(path)));
}
```

## File Structure

```
app/
  layout.tsx, page.tsx, loading.tsx, error.tsx, not-found.tsx
  (marketing)/about/, (app)/dashboard/  # Route groups
  @modal/(.)photo/[id]/                  # Parallel + intercepting routes
  api/posts/route.ts                     # Route handlers
  actions/                               # Server Actions
components/, lib/, types/
middleware.ts
```

## Common Pitfalls

- **Don't** use `useState`/`useEffect` in Server Components
- **Don't** pass functions from Server to Client Components
- **Don't** forget to await `params`/`searchParams` in v16
- **Don't** use `getServerSideProps`/`getStaticProps`—use Server Components
- **Don't** create API routes for data Server Components can fetch directly

## Performance

- `<Suspense>` boundaries for streaming
- `next/image` with proper sizing
- `dynamic()` for lazy loading Client Components
- Route segment config: `export const revalidate = 3600`
- Bundle analysis with `@next/bundle-analyzer`
