import { ref } from 'vue'
import { defineStore } from 'pinia'
import { clientsService } from '../services/clientsService'
import type { Client, ClientWrite, PagedResult } from '../types/api'

const emptyPage = (): PagedResult<Client> => ({
  items: [],
  pagina: 1,
  cantidad: 10,
  total: 0,
  totalPaginas: 0,
})

export const useClientsStore = defineStore('clients', () => {
  const page = ref<PagedResult<Client>>(emptyPage())
  const loading = ref(false)
  const error = ref('')

  async function load(pagina = page.value.pagina, cantidad = page.value.cantidad) {
    loading.value = true
    error.value = ''
    try {
      page.value = await clientsService.list(pagina, cantidad)
      return page.value
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudieron cargar los clientes.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function searchByEmail(email: string): Promise<Client | null> {
    loading.value = true
    error.value = ''
    try {
      return await clientsService.searchByEmail(email)
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo buscar el cliente.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function create(client: ClientWrite) {
    try {
      error.value = ''
      const created = await clientsService.create(client)
      await load(1, page.value.cantidad)
      return created
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo guardar el cliente.'
      throw cause
    }
  }

  async function update(id: number, client: ClientWrite) {
    try {
      error.value = ''
      const updated = await clientsService.update(id, client)
      await load()
      return updated
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo actualizar el cliente.'
      throw cause
    }
  }

  async function remove(id: number) {
    try {
      error.value = ''
      await clientsService.remove(id)
      await load()
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo eliminar el cliente.'
      throw cause
    }
  }

  return { page, loading, error, load, searchByEmail, create, update, remove }
})
