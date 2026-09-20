import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/events' },
    { path: '/login', name: 'login', component: () => import('../views/Login.vue') },
    { path: '/register', name: 'register', component: () => import('../views/Register.vue') },

    {
      path: '/events',
      name: 'events',
      component: () => import('../views/customer/EventList.vue'),
    },
    {
      path: '/events/:id',
      name: 'event-detail',
      component: () => import('../views/customer/SeatMap.vue'),
    },
    {
      path: '/my-bookings',
      name: 'my-bookings',
      component: () => import('../views/customer/MyBookings.vue'),
      meta: { requiresAuth: true },
    },

    {
      path: '/admin/events/new',
      name: 'admin-event-create',
      component: () => import('../views/admin/EventCreate.vue'),
      meta: { requiresAuth: true, requiresRole: 'Organizer' },
    },
    {
      path: '/admin/events',
      name: 'admin-event-dashboard',
      component: () => import('../views/admin/EventDashboard.vue'),
      meta: { requiresAuth: true, requiresRole: 'Organizer' },
    },
    {
      path: '/events/:id/checkout',
      name: 'checkout',
      component: () => import('../views/customer/Checkout.vue'),
      meta: { requiresAuth: true },
    },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFound.vue') },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresRole && authStore.role !== to.meta.requiresRole) {
    // Logged in, but wrong role (e.g. a Customer trying to hit /admin/*)
    return { name: 'events' }
  }

  return true
})

export default router