<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const router = useRouter()

function handleLogout() {
  authStore.logout()
  router.push('/events')
}
</script>

<template>
  <nav class="border-b border-surface-raised bg-surface">
    <div class="max-w-5xl mx-auto px-6 h-16 flex items-center justify-between">
      <router-link to="/events" class="font-semibold text-lg text-text-primary">
        سیت‌سینک
      </router-link>

      <div class="flex items-center gap-6 text-sm">
        <router-link
          to="/events"
          class="text-text-muted hover:text-text-primary transition-colors"
        >
          رویدادها
        </router-link>

        <template v-if="authStore.isAuthenticated">
          <router-link
            v-if="authStore.isCustomer"
            to="/my-bookings"
            class="text-text-muted hover:text-text-primary transition-colors"
          >
            رزروهای من
          </router-link>

          <router-link
            v-if="authStore.isOrganizer"
            to="/admin/events"
            class="text-text-muted hover:text-text-primary transition-colors"
          >
            داشبورد
          </router-link>

          <router-link
            v-if="authStore.isOrganizer"
            to="/admin/events/new"
            class="text-text-muted hover:text-text-primary transition-colors"
          >
            رویداد جدید
          </router-link>

          <span class="text-text-muted">|</span>

          <span class="text-text-primary">{{ authStore.displayName }}</span>
          <button
            @click="handleLogout"
            class="text-text-muted hover:text-text-primary transition-colors"
          >
            خروج
          </button>
        </template>

        <template v-else>
          <router-link
            to="/login"
            class="text-text-muted hover:text-text-primary transition-colors"
          >
            ورود
          </router-link>
          <router-link
            to="/register"
            class="bg-accent hover:bg-accent-hover text-white px-4 py-1.5 rounded-lg transition-colors"
          >
            ثبت‌نام
          </router-link>
        </template>
      </div>
    </div>
  </nav>
</template>