// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { apiRequest } from './apiClient'
import type { Client, ClientWrite, PagedResult } from '../types/api'

export const clientsService = {
  list(pagina = 1, cantidad = 10, buscar = '') {
    const query = new URLSearchParams({ pagina: String(pagina), cantidad: String(cantidad) })
    if (buscar.trim()) query.set('buscar', buscar.trim())
    return apiRequest<PagedResult<Client>>(`/api/clientes?${query}`)
  },
  get(id: number) {
    return apiRequest<Client>(`/api/clientes/${id}`)
  },
  searchByEmail(email: string) {
    const query = new URLSearchParams({ email })
    return apiRequest<Client | null>(`/api/clientes/buscar?${query}`)
  },
  /**
   * Búsqueda global en el servidor (nombre, email o teléfono). No descarga
   * el directorio completo ni se limita a la página visible.
   */
  async search(term: string) {
    const value = term.trim()
    if (!value) return []
    const page = await this.list(1, 50, value)
    return page.items
  },
  create(client: ClientWrite) {
    return apiRequest<Client>('/api/clientes', { method: 'POST', body: client })
  },
  update(id: number, client: ClientWrite) {
    return apiRequest<Client>(`/api/clientes/${id}`, {
      method: 'PUT',
      body: { ...client, id },
    })
  },
  remove(id: number) {
    return apiRequest<void>(`/api/clientes/${id}`, { method: 'DELETE' })
  },
}
