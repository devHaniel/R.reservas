import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authService } from '../services/authService'
import { currentUser } from '../services/authSession'
import type { AuthUser, LoginInput, RegisterVendorInput } from '../types/api'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const initialized = ref(false)
  const loading = ref(false)
  const error = ref('')
  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.roles.includes('Admin') ?? false)

  function initialize() {
    if (initialized.value) return
    user.value = currentUser()
    initialized.value = true
    if (typeof window !== 'undefined') {
      window.addEventListener('reservas:session-expired', () => {
        user.value = null
        window.dispatchEvent(new CustomEvent('reservas:auth-required'))
      })
    }
  }

  async function login(input: LoginInput) {
    loading.value = true
    error.value = ''
    try {
      user.value = await authService.login(input)
      return user.value
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo iniciar sesión.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  function logout() {
    authService.logout()
    user.value = null
    error.value = ''
  }

  async function registerVendor(input: RegisterVendorInput) {
    if (!isAdmin.value) throw new Error('Solo Admin puede registrar vendedores.')
    return authService.registerVendor(input)
  }

  initialize()

  return {
    user,
    initialized,
    loading,
    error,
    isAuthenticated,
    isAdmin,
    initialize,
    login,
    logout,
    registerVendor,
  }
})
