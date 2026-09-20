<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../../services/api'
import { joinEventGroup, leaveEventGroup, onSeatStatusChanged, offSeatStatusChanged } from '../../services/signalr'
import { useAuthStore } from '../../stores/auth'
import type { EventDetail, Seat } from '../../types'
import { formatJalaliDateTime, formatPrice } from '../../utils/date'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const eventId = route.params.id as string

const event = ref<EventDetail | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')
const isSubmitting = ref(false)

const pickedIds = ref<Set<string>>(new Set())

const rows = computed(() => {
  if (!event.value) return []
  const grouped = new Map<string, Seat[]>()
  for (const seat of event.value.seats) {
    if (!grouped.has(seat.row)) grouped.set(seat.row, [])
    grouped.get(seat.row)!.push(seat)
  }
  return Array.from(grouped.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([row, seats]) => ({ row, seats: seats.sort((a, b) => a.number - b.number) }))
})

const pickedSeats = computed(() =>
  event.value ? event.value.seats.filter((s) => pickedIds.value.has(s.id)) : []
)

const totalPrice = computed(() => pickedSeats.value.reduce((sum, s) => sum + s.price, 0))

function seatState(seat: Seat): 'mine-picked' | 'available' | 'held' | 'booked' {
  if (pickedIds.value.has(seat.id)) return 'mine-picked'
  if (seat.status === 'Booked') return 'booked'
  if (seat.status === 'Held') return 'held'
  return 'available'
}

function handleSeatClick(seat: Seat) {
  errorMessage.value = ''
  const state = seatState(seat)

  if (state === 'mine-picked') {
    pickedIds.value.delete(seat.id)
    pickedIds.value = new Set(pickedIds.value)
    return
  }

  if (state !== 'available') return

  if (!authStore.isAuthenticated) {
    errorMessage.value = 'برای انتخاب صندلی ابتدا وارد شوید.'
    return
  }

  pickedIds.value.add(seat.id)
  pickedIds.value = new Set(pickedIds.value)
}

async function handleStartReservation() {
  errorMessage.value = ''
  const seatIds = Array.from(pickedIds.value)
  if (seatIds.length === 0) return

  isSubmitting.value = true
  const results = await Promise.allSettled(
    seatIds.map((seatId) => api.post(`/events/${eventId}/seats/${seatId}/hold`))
  )

  const succeededIds: string[] = []
  let earliestUntil: number | null = null
  let anyFailed = false

  results.forEach((result, i) => {
    if (result.status === 'fulfilled') {
      succeededIds.push(seatIds[i])
      const t = new Date(result.value.data.heldUntil).getTime()
      if (earliestUntil === null || t < earliestUntil) earliestUntil = t
    } else {
      anyFailed = true
    }
  })

  isSubmitting.value = false
  pickedIds.value = new Set()

  if (succeededIds.length === 0) {
    errorMessage.value = 'رزرو صندلی‌ها ناموفق بود. لطفاً دوباره تلاش کنید.'
    return
  }
  if (anyFailed) {
    errorMessage.value = 'برخی صندلی‌ها همین الان توسط شخص دیگری انتخاب شدند.'
  }

  router.push({
    name: 'checkout',
    params: { id: eventId },
    query: { seats: succeededIds.join(','), until: new Date(earliestUntil!).toISOString() },
  })
}

function handleSeatStatusChanged(seatId: string, status: string) {
  if (!event.value) return
  const seat = event.value.seats.find((s) => s.id === seatId)
  if (seat) seat.status = status as Seat['status']
}

onMounted(async () => {
  try {
    const { data } = await api.get<EventDetail>(`/events/${eventId}`)
    event.value = data
  } catch {
    errorMessage.value = 'رویداد یافت نشد.'
  } finally {
    isLoading.value = false
  }

  await joinEventGroup(eventId)
  onSeatStatusChanged(handleSeatStatusChanged)
})

onUnmounted(() => {
  offSeatStatusChanged(handleSeatStatusChanged)
  leaveEventGroup(eventId)
})
</script>

<template>
  <div v-if="isLoading" class="max-w-5xl mx-auto px-6 py-10 text-text-muted">در حال بارگذاری...</div>

  <div v-else-if="!event" class="max-w-5xl mx-auto px-6 py-10 text-red-400">
    {{ errorMessage || 'رویداد یافت نشد.' }}
  </div>

  <div v-else class="max-w-5xl mx-auto px-6 py-10 pb-32">
    <h1 class="text-2xl font-bold text-text-primary mb-1">{{ event.name }}</h1>
    <p class="text-text-muted mb-1">{{ event.venue }}</p>
    <p class="font-mono text-sm text-text-muted mb-8">{{ formatJalaliDateTime(event.dateTime) }}</p>

    <div class="mb-10">
      <div class="h-2 rounded-full bg-accent mx-auto" style="width: 60%; box-shadow: 0 0 40px 4px var(--color-accent);"></div>
      <p class="text-center text-xs text-text-muted mt-2 tracking-wide">صحنه اجرا</p>
    </div>

    <div class="flex items-center gap-5 justify-center mb-8 text-xs text-text-muted">
      <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-sm border border-text-muted"></span> آزاد</span>
      <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-sm bg-accent"></span> انتخاب شما</span>
      <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-sm bg-seat-held"></span> در حال انتخاب</span>
      <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-sm bg-seat-booked"></span> رزرو شده</span>
    </div>

    <p v-if="errorMessage" class="text-center text-red-400 text-sm mb-4">{{ errorMessage }}</p>

    <div class="flex flex-col items-center gap-2">
      <div v-for="rowGroup in rows" :key="rowGroup.row" class="flex items-center gap-2">
        <span class="w-6 text-xs font-mono text-text-muted text-center">{{ rowGroup.row }}</span>
        <button
          v-for="seat in rowGroup.seats"
          :key="seat.id"
          @click="handleSeatClick(seat)"
          :disabled="seatState(seat) === 'booked' || seatState(seat) === 'held'"
          :title="`${seat.row}${seat.number} — ${formatPrice(seat.price)}`"
          class="relative w-9 h-9 rounded-md text-xs font-mono flex items-center justify-center transition-colors"
          :class="{
            'border border-text-muted text-text-muted hover:border-accent hover:text-accent cursor-pointer': seatState(seat) === 'available',
            'bg-accent text-white cursor-pointer': seatState(seat) === 'mine-picked',
            'bg-seat-held/40 text-seat-held cursor-not-allowed': seatState(seat) === 'held',
            'bg-seat-booked/30 text-seat-booked cursor-not-allowed': seatState(seat) === 'booked',
          }"
        >
          {{ seat.number.toLocaleString('fa-IR') }}
        </button>
      </div>
    </div>

    <div v-if="pickedSeats.length > 0" class="fixed bottom-0 inset-x-0 bg-surface border-t border-surface-raised px-6 py-4">
      <div class="max-w-5xl mx-auto flex items-center justify-between gap-4 flex-wrap">
        <div class="flex items-center gap-4 text-sm">
          <span class="text-text-muted">{{ pickedSeats.length.toLocaleString('fa-IR') }} صندلی انتخاب شده</span>
          <span class="font-mono text-text-primary">{{ formatPrice(totalPrice) }}</span>
        </div>
        <button
          @click="handleStartReservation"
          :disabled="isSubmitting"
          class="bg-accent hover:bg-accent-hover disabled:opacity-50 text-white font-medium px-6 py-2.5 rounded-lg transition-colors"
        >
          {{ isSubmitting ? 'در حال رزرو...' : 'ادامه به پرداخت' }}
        </button>
      </div>
    </div>
  </div>
</template>