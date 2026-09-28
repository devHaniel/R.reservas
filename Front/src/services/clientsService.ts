import { apiRequest } from './apiClient'
import type { Client, ClientWrite, PagedResult } from '../types/api'

export const clientsService = {
  list(pagina = 1, cantidad = 10) {
    const query = new URLSearchParams({ pagina: String(pagina), cantidad: String(cantidad) })
    return apiRequest<PagedResult<Client>>(`/api/clientes?${query}`)
  },
  get(id: number) {
    return apiRequest<Client>(`/api/clientes/${id}`)
  },
  searchByEmail(email: string) {
    const query = new URLSearchParams({ email })
    return apiRequest<Client | null>(`/api/clientes/buscar?${query}`)
  },
  async search(term: string) {
    const value = term.trim().toLocaleLowerCase('es')
    if (!value) return []

    if (value.includes('@')) {
      const exact = await this.searchByEmail(term.trim())
      if (exact) return [exact]
    }

    const firstPage = await this.list(1, 100)
    const otherPages = await Promise.all(
      Array.from({ length: Math.max(0, firstPage.totalPaginas - 1) }, (_, index) =>
        this.list(index + 2, 100),
      ),
    )
    return [firstPage, ...otherPages]
      .flatMap((page) => page.items)
      .filter((client) =>
        [client.nombre, client.email, client.telefono].some((field) =>
          field.toLocaleLowerCase('es').includes(value),
        ),
      )
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
