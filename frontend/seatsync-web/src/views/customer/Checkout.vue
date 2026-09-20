<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../../services/api'
import { joinEventGroup, leaveEventGroup, onSeatStatusChanged, offSeatStatusChanged } from '../../services/signalr'
import type { EventDetail, Seat } from '../../types'
import { formatJalaliDateTime, formatPrice } from '../../utils/date'
import { waitForConfirmedBooking, downloadReceipt } from '../../utils/receiptDownload'
const route = useRoute()
const router = useRouter()
const eventId = route.params.id as string
const seatIds = ((route.query.seats as string) || '').split(',').filter(Boolean)
const heldUntilIso = route.query.until as string | undefined

const event = ref<EventDetail | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')
const isPaying = ref(false)
const isDone = ref(false)
const now = ref(Date.now())
let tickInterval: number | undefined

const seats = computed<Seat[]>(() =>
  event.value ? event.value.seats.filter((s) => seatIds.includes(s.id)) : []
)
const totalPrice = computed(() => seats.value.reduce((sum, s) => sum + s.price, 0))

const remainingSeconds = computed(() => {
  if (!heldUntilIso) return 0
  return Math.max(0, Math.floor((new Date(heldUntilIso).getTime() - now.value) / 1000))
})

function formatCountdown(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}:${s.toString().padStart(2, '0')}`
}

function handleSeatStatusChanged(seatId: string, status: string) {
  if (!seatIds.includes(seatId) || isDone.value) return
  if (status === 'Available') {
    errorMessage.value = 'زمان رزرو یکی از صندلی‌ها به پایان رسید. لطفاً به نقشه صندلی بازگردید.'
  }
}

async function handleCancel() {
  await Promise.allSettled(seatIds.map((seatId) => api.delete(`/events/${eventId}/seats/${seatId}/hold`)))
  router.push(`/events/${eventId}`)
}

async function handlePay() {
  if (isPaying.value) return // guard against a double-click racing ahead of the disabled attribute

  errorMessage.value = ''
  isPaying.value = true
  try {
    const { data } = await api.post('/bookings/confirm', { eventId, seatIds })
    const confirmedBooking = await waitForConfirmedBooking(data.bookingRequestId)
    await downloadReceipt(confirmedBooking.id)
    isDone.value = true
  } catch {
    errorMessage.value = 'پرداخت با خطا مواجه شد یا تأیید نهایی طول کشید. لطفاً رزروهای خود را بررسی کنید.'
  } finally {
    isPaying.value = false
  }
}

onMounted(async () => {
  if (seatIds.length === 0) {
    router.replace(`/events/${eventId}`)
    return
  }
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
  tickInterval = window.setInterval(() => { now.value = Date.now() }, 1000)
})

onUnmounted(() => {
  offSeatStatusChanged(handleSeatStatusChanged)
  leaveEventGroup(eventId)
  if (tickInterval) clearInterval(tickInterval)
})
</script>

<template>
  <div v-if="isLoading" class="max-w-2xl mx-auto px-6 py-10 text-text-muted">در حال بارگذاری...</div>

  <div v-else-if="isDone" class="max-w-2xl mx-auto px-6 py-16 text-center">
    <div class="w-14 h-14 rounded-full bg-seat-available/20 text-seat-available flex items-center justify-center mx-auto mb-4 text-2xl">✓</div>
    <h1 class="text-xl font-bold text-text-primary mb-2">پرداخت با موفقیت انجام شد</h1>
    <p class="text-text-muted mb-8">رسید خرید شما به صورت خودکار دانلود شد.</p>
    <router-link to="/my-bookings" class="bg-accent hover:bg-accent-hover text-white font-medium px-6 py-2.5 rounded-lg transition-colors inline-block">
      مشاهده رزروهای من
    </router-link>
  </div>

  <div v-else-if="!event" class="max-w-2xl mx-auto px-6 py-10 text-red-400">
    {{ errorMessage || 'رویداد یافت نشد.' }}
  </div>

  <div v-else class="max-w-2xl mx-auto px-6 py-10">
    <h1 class="text-2xl font-bold text-text-primary mb-8">تکمیل خرید</h1>

    <div class="bg-surface border border-surface-raised rounded-2xl p-6 mb-6">
      <h2 class="font-semibold text-text-primary mb-1">{{ event.name }}</h2>
      <p class="text-sm text-text-muted mb-1">{{ event.venue }}</p>
      <p class="font-mono text-xs text-text-muted mb-5">{{ formatJalaliDateTime(event.dateTime) }}</p>

      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-xs">
            <th class="text-right font-normal pb-2 border-b border-surface-raised">شماره صندلی</th>
            <th class="text-right font-normal pb-2 border-b border-surface-raised">قیمت</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="seat in seats" :key="seat.id">
            <td class="py-2.5 font-mono text-text-primary border-b border-surface-raised">{{ seat.row }}{{ seat.number.toLocaleString('fa-IR') }}</td>
            <td class="py-2.5 font-mono text-text-muted border-b border-surface-raised">{{ formatPrice(seat.price) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between items-center pt-4 mt-2 border-t border-dashed border-text-muted">
        <span class="text-sm text-text-muted">{{ seats.length.toLocaleString('fa-IR') }} بلیط</span>
        <span class="font-mono text-text-primary font-semibold">{{ formatPrice(totalPrice) }}</span>
      </div>
    </div>

    <div class="flex items-center justify-between mb-6 text-sm">
      <span class="text-text-muted">زمان باقی‌مانده برای تکمیل خرید</span>
      <span class="font-mono text-accent">{{ formatCountdown(remainingSeconds) }}</span>
    </div>

    <p v-if="errorMessage" class="text-red-400 text-sm mb-4">{{ errorMessage }}</p>

    <div class="flex gap-3">
      <button @click="handleCancel" class="flex-1 border border-surface-raised text-text-muted hover:text-text-primary py-2.5 rounded-lg transition-colors">
        انصراف
      </button>
      <button
        @click="handlePay"
        :disabled="isPaying"
        class="flex-1 bg-accent hover:bg-accent-hover disabled:opacity-50 text-white font-medium py-2.5 rounded-lg transition-colors"
      >
        {{ isPaying ? 'در حال پردازش...' : 'پرداخت' }}
      </button>
    </div>
  </div>
</template>