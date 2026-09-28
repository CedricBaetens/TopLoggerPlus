<script setup lang="ts">
import { computed } from 'vue'
import { ascentLabel, colorValue, dateLabel, frenchGrade, type RankedRoute, type Route } from '../utils/domain'
const props = defineProps<{ route: Route; rank?: number; ranked?: RankedRoute; leaving?: boolean }>()
const done = computed(() => (props.ranked?.ascent.tickType || props.route.climbUser?.tickType || 0) > 0)
defineEmits<{ open: [route: Route] }>()
</script>
<template>
  <button :class="['route-card', { 'route-done': done }]" @click="$emit('open', route)">
    <span v-if="rank" class="rank">{{ String(rank).padStart(2, '0') }}</span>
    <span class="grade-block"><span class="hold" :style="{ background: colorValue(route.holdColor?.color) }" /><strong>{{ frenchGrade(route.grade) }}</strong></span>
    <span class="route-copy">
      <span class="route-title">{{ route.name || (route.label ? `Rope ${route.label}` : route.wall?.nameLoc || 'Route') }}</span>
      <span class="route-meta">{{ route.wall?.nameLoc || 'Wall unknown' }} <span aria-hidden="true">·</span> {{ route.holdColor?.nameLoc || 'Color unknown' }}</span>
      <span v-if="leaving" class="removal">Leaves {{ dateLabel(route.outPlannedAt) }}</span>
      <span v-else-if="ranked" class="route-meta">{{ dateLabel(ranked.ascent.climbedAtDate) }} · {{ ranked.score }} points</span>
    </span>
    <span :class="['route-status', { done }]"><AppIcon v-if="done" name="check" /><span v-else class="status-circle" aria-hidden="true" /><span>{{ ranked ? ascentLabel(ranked.ascent.tickType) : done ? 'Done' : route.climbUser?.totalTries ? 'Attempted' : 'To do' }}</span></span>
  </button>
</template>
