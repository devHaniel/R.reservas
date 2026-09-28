import { apiRequest } from './apiClient'
import type { ServiceType, ServiceTypeWrite } from '../types/api'

export const serviceTypesService = {
  list() {
    return apiRequest<ServiceType[]>('/api/tipos-servicio')
  },
  get(id: number) {
    return apiRequest<ServiceType>(`/api/tipos-servicio/${id}`)
  },
  byResource(resourceId: number) {
    return apiRequest<ServiceType[]>(`/api/tipos-servicio/por-recurso/${resourceId}`)
  },
  create(serviceType: ServiceTypeWrite) {
    return apiRequest<ServiceType>('/api/tipos-servicio', {
      method: 'POST',
      body: serviceType,
    })
  },
  update(id: number, serviceType: ServiceTypeWrite) {
    return apiRequest<ServiceType>(`/api/tipos-servicio/${id}`, {
      method: 'PUT',
      body: { ...serviceType, id },
    })
  },
  remove(id: number) {
    return apiRequest<void>(`/api/tipos-servicio/${id}`, { method: 'DELETE' })
  },
}
