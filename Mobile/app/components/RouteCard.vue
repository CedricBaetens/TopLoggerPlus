<script setup lang="ts">
import { computed } from 'vue'
import { ascentLabel, colorValue, dateLabel, frenchGrade, type RankedRoute, type Route } from '../utils/domain'
const props = defineProps<{ route: Route; rank?: number; ranked?: RankedRoute }>()
const tickType = computed(() => props.ranked?.ascent.tickType ?? props.route.climbUser?.tickType ?? 0)
const done = computed(() => tickType.value > 0)
defineEmits<{ open: [route: Route] }>()
</script>
<template>
  <button class="flex items-center gap-3 w-full min-h-16 text-left p-2.5 mt-1.5 border border-solid border-line rounded-[7px] bg-panel [&:hover]:border-accent max-[420px]:gap-2 [&.route-done]:border-success [&.route-done]:[border-left-width:5px] [&.route-done]:pl-1.5 [&.route-done]:bg-done-row" :class="['route-card', { 'route-done': done }]" @click="$emit('open', route)">
    <span v-if="rank" class="rank text-muted w-4.5 shrink-0 text-[.72rem] tabular-nums">{{ String(rank).padStart(2, '0') }}</span>
    <span class="grade-block flex items-center gap-2.25 w-16.5 shrink-0 [&_strong]:text-[1.06rem] [&_strong]:font-[650] [&_strong]:tabular-nums max-[420px]:w-15 max-[420px]:gap-1.75"><span class="hold w-3 h-3 border border-solid border-[#8888] rounded-full shrink-0" :style="{ background: colorValue(route.holdColor?.color) }" /><strong>{{ frenchGrade(route.grade) }}</strong></span>
    <span class="route-copy flex flex-col gap-0.5 flex-1 min-w-0">
      <span class="route-title text-[.85rem] font-semibold [overflow-wrap:anywhere]">{{ route.name || (route.label ? `Rope ${route.label}` : route.wall?.nameLoc || 'Route') }}</span>
      <span class="route-meta text-[.72rem] text-muted [overflow-wrap:anywhere]">{{ route.wall?.nameLoc || 'Wall unknown' }} <span aria-hidden="true">·</span> {{ route.holdColor?.nameLoc || 'Color unknown' }}</span>
      <span v-if="ranked" class="route-meta text-[.72rem] text-muted [overflow-wrap:anywhere]">{{ dateLabel(ranked.ascent.climbedAtDate) }} · {{ ranked.score }} points</span>
    </span>
    <span class="flex items-center justify-center w-9.5 h-9.5 shrink-0 p-1.5 border border-solid border-line rounded-[5px] bg-bg text-ink [&.done]:text-white [&.done]:bg-done-badge [&.done]:border-success [&.attempted]:text-gold [&_svg]:w-5.5 [&_svg]:h-5.5 [&_svg]:stroke-[3] [&.done_svg]:w-6.25 [&.done_svg]:h-6.25" :class="['route-status', { done, attempted: !done && !!route.climbUser?.totalTries }]"><AppIcon v-if="done" :name="tickType === 3 ? 'doubleCheck' : tickType === 2 ? 'flash' : 'check'" /><span v-else class="status-circle w-5.5 h-5.5 [border:1.5px_solid_currentColor] rounded-full shrink-0" aria-hidden="true" /><span class="sr-only">{{ done ? ascentLabel(tickType) : route.climbUser?.totalTries ? 'Attempted' : 'To do' }}</span></span>
  </button>
</template>
