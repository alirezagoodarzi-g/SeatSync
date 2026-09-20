import { defineStore } from 'pinia'
import { jwtDecode } from 'jwt-decode'
import api from '../services/api'
import type { AuthResponse, Role } from '../types'

interface DecodedToken {
  sub: string
  email: string
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': string
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': Role
  exp: number
}

interface AuthState {
  token: string | null
  userId: string | null
  email: string | null
  displayName: string | null
  role: Role | null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('seatsync_token'),
    userId: null,
    email: null,
    displayName: null,
    role: null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
    isOrganizer: (state) => state.role === 'Organizer',
    isCustomer: (state) => state.role === 'Customer',
  },

  actions: {
    // Reconstructs user info from a stored token on page load/refresh.
    hydrateFromToken() {
      if (!this.token) return
      try {
        const decoded = jwtDecode<DecodedToken>(this.token)
        if (decoded.exp * 1000 < Date.now()) {
          this.logout()
          return
        }
        this.userId = decoded.sub
        this.email = decoded.email
        this.displayName = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
        this.role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      } catch {
        this.logout()
      }
    },

    setToken(token: string) {
      this.token = token
      localStorage.setItem('seatsync_token', token)
      this.hydrateFromToken()
    },

    async login(email: string, password: string) {
      const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
      this.setToken(data.token)
    },

    async register(email: string, password: string, displayName: string, role: Role) {
      const { data } = await api.post<AuthResponse>('/auth/register', {
        email,
        password,
        displayName,
        role,
      })
      this.setToken(data.token)
    },

    logout() {
      this.token = null
      this.userId = null
      this.email = null
      this.displayName = null
      this.role = null
      localStorage.removeItem('seatsync_token')
    },
  },
})