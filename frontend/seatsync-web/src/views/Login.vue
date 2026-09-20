<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()

async function handleSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await authStore.login(email.value, password.value)
    const redirectTo = (route.query.redirect as string) || '/events'
    router.push(redirectTo)
  } catch (err: any) {
    errorMessage.value = err.response?.data?.error || 'ورود ناموفق بود. لطفاً دوباره تلاش کنید.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center px-4">
    <div class="w-full max-w-sm bg-surface rounded-2xl border border-surface-raised p-8">
      <h1 class="text-2xl font-bold text-text-primary mb-1">ورود به سیت‌سینک</h1>
      <p class="text-sm text-text-muted mb-6">برای مشاهده و رزرو رویدادها وارد شوید</p>

      <form @submit.prevent="handleSubmit" class="space-y-4">
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

        <p v-if="errorMessage" class="text-sm text-red-400">{{ errorMessage }}</p>

        <button
          type="submit"
          :disabled="isSubmitting"
          class="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 text-white font-medium py-2.5 rounded-lg transition-colors"
        >
          {{ isSubmitting ? 'در حال ورود...' : 'ورود' }}
        </button>
      </form>

      <p class="text-sm text-text-muted mt-6 text-center">
        حساب کاربری ندارید؟
        <router-link to="/register" class="text-accent font-medium hover:underline">ثبت‌نام کنید</router-link>
      </p>
    </div>
  </div>
</template>