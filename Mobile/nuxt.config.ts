import tailwindcss from '@tailwindcss/vite'

export default defineNuxtConfig({
  ssr: false,
  vite: { plugins: [tailwindcss()] },
  devtools: { enabled: false },
  css: ['~/assets/main.css'],
  app: { head: {
    title: 'TopLogger Plus',
    meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1, viewport-fit=cover' }, { name: 'theme-color', content: '#0b1014' }],
    link: [{ rel: 'icon', type: 'image/svg+xml', href: '/icon.svg' }],
  } },
  compatibilityDate: '2026-09-28',
})
