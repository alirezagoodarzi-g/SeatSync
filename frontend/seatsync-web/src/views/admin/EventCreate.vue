<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import * as jalaali from 'jalaali-js'
import api from '../../services/api'
import { iranLocalToUtcIso } from '../../utils/date'

interface RowDraft {
  label: string
  count: number
  price: number
}

const name = ref('')
const venue = ref('')

const dateYear = ref('')
const dateMonth = ref('')
const dateDay = ref('')
const dateHour = ref('20')
const dateMinute = ref('00')

const rows = ref<RowDraft[]>([{ label: 'A', count: 8, price: 0 }])
const errorMessage = ref('')
const isSubmitting = ref(false)
const router = useRouter()

function addRow() {
  rows.value.push({ label: String.fromCharCode(65 + rows.value.length), count: 8, price: 0 })
}
function removeRow(index: number) {
  rows.value.splice(index, 1)
}
function formatPriceInput(value: number): string {
  if (!value) return ''
  return value.toLocaleString('en-US')
}

function parsePriceInput(event: Event): number {
  const raw = (event.target as HTMLInputElement).value.replace(/,/g, '')
  const parsed = Number(raw)
  return isNaN(parsed) ? 0 : parsed
}
const totalSeats = () => rows.value.reduce((sum, r) => sum + (r.count || 0), 0)

async function handleSubmit() {
  errorMessage.value = ''

  if (!name.value || !venue.value || !dateYear.value || !dateMonth.value || !dateDay.value) {
    errorMessage.value = 'لطفاً همه فیلدها را پر کنید.'
    return
  }
  if (totalSeats() === 0) {
    errorMessage.value = 'حداقل یک ردیف با صندلی تعریف کنید.'
    return
  }

  const jy = Number(dateYear.value)
  const jm = Number(dateMonth.value)
  const jd = Number(dateDay.value)

  if (!jalaali.isValidJalaaliDate(jy, jm, jd)) {
    errorMessage.value = 'تاریخ واردشده معتبر نیست.'
    return
  }

  const { gy, gm, gd } = jalaali.toGregorian(jy, jm, jd)
  const pad = (n: number) => String(n).padStart(2, '0')
  const localDateTime = `${gy}-${pad(gm)}-${pad(gd)}T${pad(Number(dateHour.value))}:${pad(Number(dateMinute.value))}`

  const seats = rows.value.flatMap((row) =>
    Array.from({ length: row.count }, (_, i) => ({ row: row.label, number: i + 1, price: row.price }))
  )

  isSubmitting.value = true
  try {
    const { data } = await api.post('/events', {
      name: name.value,
      venue: venue.value,
      dateTime: iranLocalToUtcIso(localDateTime),
      seats,
    })
    router.push(`/events/${data.id}`)
  } catch {
    errorMessage.value = 'خطا در ایجاد رویداد.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl mx-auto px-6 py-10">
    <h1 class="text-2xl font-bold text-text-primary mb-8">ایجاد رویداد جدید</h1>

    <form @submit.prevent="handleSubmit" class="space-y-6">
      <div class="bg-surface border border-surface-raised rounded-2xl p-6 space-y-4">
        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">نام رویداد</label>
          <input
            v-model="name"
            type="text"
            class="w-full rounded-lg border border-surface-raised bg-bg px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">مکان برگزاری</label>
          <input
            v-model="venue"
            type="text"
            class="w-full rounded-lg border border-surface-raised bg-bg px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>

       <div>
  <label class="block text-sm font-medium text-text-muted mb-1">تاریخ و ساعت (به وقت ایران)</label>
  <div
    class="flex items-center gap-1 font-mono rounded-lg border border-surface-raised bg-bg px-3 py-2 focus-within:ring-2 focus-within:ring-accent"
    dir="ltr"
  >
    <input
      v-model="dateYear"
      type="text"
      inputmode="numeric"
      placeholder="yyyy"
      maxlength="4"
      class="w-12 bg-transparent text-sm text-text-primary text-center focus:outline-none"
    />
    <span class="text-text-muted">/</span>
    <input
      v-model="dateMonth"
      type="text"
      inputmode="numeric"
      placeholder="mm"
      maxlength="2"
      class="w-8 bg-transparent text-sm text-text-primary text-center focus:outline-none"
    />
    <span class="text-text-muted">/</span>
    <input
      v-model="dateDay"
      type="text"
      inputmode="numeric"
      placeholder="dd"
      maxlength="2"
      class="w-8 bg-transparent text-sm text-text-primary text-center focus:outline-none"
    />
    <span class="text-text-muted mx-3">—</span>
    <input
      v-model="dateHour"
      type="text"
      inputmode="numeric"
      placeholder="hh"
      maxlength="2"
      class="w-8 bg-transparent text-sm text-text-primary text-center focus:outline-none"
    />
    <span class="text-text-muted">:</span>
    <input
      v-model="dateMinute"
      type="text"
      inputmode="numeric"
      placeholder="mm"
      maxlength="2"
      class="w-8 bg-transparent text-sm text-text-primary text-center focus:outline-none"
    />
  </div>
  <p class="text-xs text-text-muted mt-1">تاریخ شنسی — مثال: 05 / 11 / 1405</p>
</div>
      </div>

      <div class="bg-surface border border-surface-raised rounded-2xl p-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-sm font-medium text-text-muted">چیدمان صندلی‌ها</h2>
          <button type="button" @click="addRow" class="text-xs text-accent hover:underline">+ افزودن ردیف</button>
        </div>

        <div class="space-y-3">
          <div v-for="(row, index) in rows" :key="index" class="flex items-center gap-3">
            <input
              v-model="row.label"
              type="text"
              maxlength="2"
              class="w-14 rounded-lg border border-surface-raised bg-bg px-2 py-2 text-sm font-mono text-text-primary text-center focus:outline-none focus:ring-2 focus:ring-accent"
            />
            <div class="flex-1">
              <label class="block text-[11px] text-text-muted mb-1">تعداد صندلی</label>
              <input
                v-model.number="row.count"
                type="number"
                min="1"
                class="w-full rounded-lg border border-surface-raised bg-bg px-2 py-1.5 text-sm font-mono text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
              />
            </div>
            <div class="flex-1">
              <label class="block text-[11px] text-text-muted mb-1">قیمت (تومان)</label>
              <input
                :value="formatPriceInput(row.price)"
                @input="row.price = parsePriceInput($event)"
                type="text"
                inputmode="numeric"
                class="w-full rounded-lg border border-surface-raised bg-bg px-2 py-1.5 text-sm font-mono text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
              />
            </div>
            <button type="button" @click="removeRow(index)" class="text-text-muted hover:text-red-400 text-sm mt-4">حذف</button>
          </div>
        </div>

        <p class="text-xs text-text-muted mt-4 font-mono">مجموع صندلی‌ها: {{ totalSeats().toLocaleString('fa-IR') }}</p>
      </div>

      <p v-if="errorMessage" class="text-sm text-red-400">{{ errorMessage }}</p>

      <button
        type="submit"
        :disabled="isSubmitting"
        class="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 text-white font-medium py-2.5 rounded-lg transition-colors"
      >
        {{ isSubmitting ? 'در حال ایجاد...' : 'ایجاد رویداد' }}
      </button>
    </form>
  </div>
</template>