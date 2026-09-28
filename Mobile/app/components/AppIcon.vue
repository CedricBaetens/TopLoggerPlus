<script setup lang="ts">
const props = defineProps<{ name: string }>()
const paths: Record<string, string> = {
  routes: 'M4 4h6v6H4z M14 4h6v6h-6z M4 14h6v6H4z M14 14h6v6h-6z',
  leaving: 'M12 8v5l3 2 M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18',
  top: 'M4 20h16 M6 16V11 M12 16V4 M18 16V8',
  account: 'M12 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8 M5 21v-3a7 7 0 0 1 14 0v3',
  search: 'M10.5 3a7.5 7.5 0 1 0 0 15 7.5 7.5 0 0 0 0-15 M16 16l5 5',
  refresh: 'M20 7v5h-5 M4 17v-5h5 M5 8a8 8 0 0 1 13-3l2 2 M4 17l2 2a8 8 0 0 0 13-3',
  arrow: 'm9 5 7 7-7 7', close: 'm6 6 12 12 M6 18 18 6', back: 'm14 5-7 7 7 7',
  check: 'm5 12 4 4L19 6', mountain: 'm3 20 7-15 5 9 3-5 4 11 M7 20l5-10 5 10',
  filter: 'M4 6h16 M7 12h10 M10 18h4', connect: 'M9 15l6-6 M8 17l-1 1a4 4 0 0 1-6-6l4-4a4 4 0 0 1 6 0 M16 7l1-1a4 4 0 1 1 6 6l-4 4a4 4 0 0 1-6 0',
}
</script>
<template><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path :d="paths[props.name] || paths.routes" /></svg></template>
