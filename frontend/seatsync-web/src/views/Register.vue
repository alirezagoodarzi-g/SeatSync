<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import type { Role } from '../types'

const email = ref('')
const password = ref('')
const displayName = ref('')
const role = ref<Role>('Customer')
const errorMessage = ref('')
const isSubmitting = ref(false)

const authStore = useAuthStore()
const router = useRouter()

async function handleSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await authStore.register(email.value, password.value, displayName.value, role.value)
    router.push('/events')
  } catch (err: any) {
    errorMessage.value = err.response?.data?.error || 'ثبت‌نام ناموفق بود. لطفاً دوباره تلاش کنید.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center px-4">
    <div class="w-full max-w-sm bg-surface rounded-2xl border border-surface-raised p-8">
      <h1 class="text-2xl font-bold text-text-primary mb-1">ساخت حساب کاربری</h1>
      <p class="text-sm text-text-muted mb-6">به سیت‌سینک بپیوندید</p>

      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">نام نمایشی</label>
          <input
            v-model="displayName"
            type="text"
            required
            class="w-full rounded-lg border border-surface-raised bg-bg px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">ایمیل</label>
          <input
            v-model="email"
            type="email"
            required
            class="w-full rounded-lg border border-surface-raised bg-bg px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">رمز عبور</label>
          <input
            v-model="password"
            type="password"
            required
            class="w-full rounded-lg border border-surface-raised bg-bg px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">نوع حساب</label>
          <div class="flex gap-3">
            <button
              type="button"
              @click="role = 'Customer'"
              :class="[
                'flex-1 py-2 rounded-lg text-sm font-medium border transition-colors',
                role === 'Customer'
                  ? 'bg-accent text-white border-accent'
                  : 'bg-bg text-text-muted border-surface-raised',
              ]"
            >
              مشتری
            </button>
            <button
              type="button"
              @click="role = 'Organizer'"
              :class="[
                'flex-1 py-2 rounded-lg text-sm font-medium border transition-colors',
                role === 'Organizer'
                  ? 'bg-accent text-white border-accent'
                  : 'bg-bg text-text-muted border-surface-raised',
              ]"
            >
              برگزارکننده
            </button>
          </div>
        </div>

        <p v-if="errorMessage" class="text-sm text-red-400">{{ errorMessage }}</p>

        <button
          type="submit"
          :disabled="isSubmitting"
          class="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 text-white font-medium py-2.5 rounded-lg transition-colors"
        >
          {{ isSubmitting ? 'در حال ثبت‌نام...' : 'ثبت‌نام' }}
        </button>
      </form>

      <p class="text-sm text-text-muted mt-6 text-center">
        قبلاً ثبت‌نام کرده‌اید؟
        <router-link to="/login" class="text-accent font-medium hover:underline">وارد شوید</router-link>
      </p>
    </div>
  </div>
</template>