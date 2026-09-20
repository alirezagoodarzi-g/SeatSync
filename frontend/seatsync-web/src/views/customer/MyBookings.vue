<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../../services/api'
import type { Booking, EventDetail } from '../../types'
import { formatJalaliDateTime, formatPrice } from '../../utils/date'

interface EnrichedBooking extends Booking {
  eventName: string
  eventVenue: string
  eventDateTime: string
  seatLabels: string[]
  totalPrice: number
}

const bookings = ref<EnrichedBooking[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    const { data: rawBookings } = await api.get<Booking[]>('/bookings/mine')
    const eventCache = new Map<string, EventDetail>()
    const enriched: EnrichedBooking[] = []

    for (const booking of rawBookings) {
      let event = eventCache.get(booking.eventId)
      if (!event) {
        const { data } = await api.get<EventDetail>(`/events/${booking.eventId}`)
        event = data
        eventCache.set(booking.eventId, event)
      }

      const seats = booking.seatIds
        .map((id) => event!.seats.find((s) => s.id === id))
        .filter((s): s is NonNullable<typeof s> => !!s)

      enriched.push({
        ...booking,
        eventName: event.name,
        eventVenue: event.venue,
        eventDateTime: event.dateTime,
        seatLabels: seats.map((s) => `${s.row}${s.number}`),
        totalPrice: seats.reduce((sum, s) => sum + s.price, 0),
      })
    }

    bookings.value = enriched.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )
  } catch {
    errorMessage.value = 'خطا در دریافت رزروها.'
  } finally {
    isLoading.value = false
  }
})

function statusLabel(status: string): string {
  return { Confirmed: 'تأیید شده', Pending: 'در حال بررسی', Cancelled: 'لغو شده' }[status] || status
}

function statusClass(status: string): string {
  return {
    Confirmed: 'text-seat-available',
    Pending: 'text-seat-held',
    Cancelled: 'text-seat-booked',
  }[status] || 'text-text-muted'
}
</script>

<template>
  <div class="max-w-3xl mx-auto px-6 py-10">
    <h1 class="text-2xl font-bold text-text-primary mb-8">رزروهای من</h1>

    <div v-if="isLoading" class="text-text-muted">در حال بارگذاری...</div>
    <div v-else-if="errorMessage" class="text-red-400">{{ errorMessage }}</div>
    <div v-else-if="bookings.length === 0" class="text-text-muted">هنوز رزروی ثبت نکرده‌اید.</div>

    <div v-else class="space-y-4">
      <div
        v-for="booking in bookings"
        :key="booking.id"
        class="bg-surface border border-surface-raised rounded-2xl p-6 flex justify-between items-start flex-wrap gap-4"
      >
        <div>
          <h2 class="text-lg font-semibold text-text-primary">{{ booking.eventName }}</h2>
          <p class="text-sm text-text-muted">{{ booking.eventVenue }}</p>
          <p class="font-mono text-xs text-text-muted mt-1">{{ formatJalaliDateTime(booking.eventDateTime) }}</p>
        </div>

        <div class="text-left">
          <span :class="['text-xs font-medium', statusClass(booking.status)]">{{ statusLabel(booking.status) }}</span>
          <p class="font-mono text-sm text-text-primary mt-2">{{ booking.seatLabels.join('، ') }}</p>
          <p class="font-mono text-xs text-text-muted mt-1">{{ formatPrice(booking.totalPrice) }}</p>
        </div>
      </div>
    </div>
  </div>
</template>