import { apiRequest } from './apiClient'
import type { ReservableResource, ResourceWrite } from '../types/api'

export const resourcesService = {
  list() {
    return apiRequest<ReservableResource[]>('/api/recursos-reservables')
  },
  get(id: number) {
    return apiRequest<ReservableResource>(`/api/recursos-reservables/${id}`)
  },
  searchBySport(tipoDeporte: string) {
    const query = new URLSearchParams({ tipoDeporte })
    return apiRequest<ReservableResource[]>(`/api/recursos-reservables/buscar?${query}`)
  },
  create(resource: ResourceWrite) {
    return apiRequest<ReservableResource>('/api/recursos-reservables', {
      method: 'POST',
      body: resource,
    })
  },
  update(id: number, resource: ResourceWrite) {
    return apiRequest<ReservableResource>(`/api/recursos-reservables/${id}`, {
      method: 'PUT',
      body: { ...resource, id },
    })
  },
  remove(id: number) {
    return apiRequest<void>(`/api/recursos-reservables/${id}`, { method: 'DELETE' })
  },
}
