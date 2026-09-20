<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../../services/api'
import type { EventSummary } from '../../types'
import { formatJalaliDateTime } from '../../utils/date'

const events = ref<EventSummary[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const router = useRouter()

onMounted(async () => {
  try {
    const { data } = await api.get<EventSummary[]>('/events')
    events.value = data
  } catch {
    errorMessage.value = 'خطا در دریافت رویدادها. لطفاً دوباره تلاش کنید.'
  } finally {
    isLoading.value = false
  }
})

function goToEvent(id: string) {
  router.push(`/events/${id}`)
}
</script>

<template>
  <div class="max-w-5xl mx-auto px-6 py-10">
    <h1 class="text-2xl font-bold text-text-primary mb-8">رویدادهای پیش‌رو</h1>

    <div v-if="isLoading" class="text-text-muted">در حال بارگذاری...</div>

    <div v-else-if="errorMessage" class="text-red-400">{{ errorMessage }}</div>

    <div v-else-if="events.length === 0" class="text-text-muted">
      در حال حاضر رویدادی برای نمایش وجود ندارد.
    </div>

    <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <button
        v-for="event in events"
        :key="event.id"
        @click="goToEvent(event.id)"
        class="text-right bg-surface border border-surface-raised rounded-2xl p-6 hover:border-accent transition-colors"
      >
        <h2 class="text-lg font-semibold text-text-primary mb-1">{{ event.name }}</h2>
        <p class="text-sm text-text-muted mb-4">{{ event.venue }}</p>

        <div class="flex items-center justify-between text-sm">
          <span class="font-mono text-text-muted">{{ formatJalaliDateTime(event.dateTime) }}</span>
          <span class="font-mono text-seat-available">{{ event.totalSeats.toLocaleString('fa-IR') }} صندلی</span>
        </div>
      </button>
    </div>
  </div>
</template>