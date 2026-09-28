// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { ref } from 'vue'
import { defineStore } from 'pinia'
import { apiRequest } from '../services/apiClient'
import type { AuthUser } from '../types/api'
import type { ApiRole } from '../types/api'

export const useUsersStore = defineStore('users', () => {
  const users = ref<AuthUser[]>([])
  const loading = ref(false)
  const error = ref('')

  async function load() {
    loading.value = true
    error.value = ''
    try {
      const result = await apiRequest<AuthUser[]>('/api/usuarios', { auth: true })
      users.value = result
      return result
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudieron cargar los usuarios.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function create(input: { nombre: string; email: string; password: string; rol: 'Admin' | 'Vendedor' }) {
    loading.value = true
    error.value = ''
    try {
      const result = await apiRequest<AuthUser>('/api/auth/register', {
        method: 'POST',
        body: input,
        auth: false,
      })
      users.value.push(result)
      return result
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo crear el usuario.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function update(id: number, input: { nombre?: string; email?: string; rol?: 'Admin' | 'Vendedor' }) {
    loading.value = true
    error.value = ''
    try {
      const result = await apiRequest<AuthUser>(`/api/usuarios/${id}`, {
        method: 'PUT',
        body: input,
        auth: true,
      })
      const index = users.value.findIndex((u) => Number(u.id) === id)
      if (index !== -1) {
        users.value[index] = result
      }
      return result
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo actualizar el usuario.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function remove(id: number) {
    loading.value = true
    error.value = ''
    try {
      await apiRequest<void>(`/api/usuarios/${id}`, { method: 'DELETE', auth: true })
      users.value = users.value.filter((u) => Number(u.id) !== id)
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo eliminar el usuario.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  return {
    users,
    loading,
    error,
    load,
    create,
    update,
    remove,
  }
})