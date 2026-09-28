export default defineNuxtConfig({
  ssr: false,
  devtools: { enabled: false },
  css: ['~/assets/main.css'],
  app: { head: {
    title: 'TopLogger Plus',
    meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1, viewport-fit=cover' }, { name: 'theme-color', content: '#152f2b' }],
    link: [{ rel: 'icon', type: 'image/svg+xml', href: '/icon.svg' }],
  } },
  compatibilityDate: '2026-09-28',
})
