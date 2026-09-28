import { apiRequest } from './apiClient'
import type { Reservation, ReservationWrite } from '../types/api'

export const reservationsService = {
  list() {
    return apiRequest<Reservation[]>('/api/reservas')
  },
  get(id: number) {
    return apiRequest<Reservation>(`/api/reservas/${id}`)
  },
  byClient(clientId: number) {
    return apiRequest<Reservation[]>(`/api/reservas/por-cliente/${clientId}`)
  },
  byResource(resourceId: number) {
    return apiRequest<Reservation[]>(`/api/reservas/por-recurso/${resourceId}`)
  },
  byDates(filters: { desde?: string; hasta?: string; recursoReservableId?: number } = {}) {
    const query = new URLSearchParams()
    if (filters.desde) query.set('desde', filters.desde)
    if (filters.hasta) query.set('hasta', filters.hasta)
    if (filters.recursoReservableId !== undefined)
      query.set('recursoReservableId', String(filters.recursoReservableId))
    const suffix = query.toString()
    return apiRequest<Reservation[]>(`/api/reservas/por-fechas${suffix ? `?${suffix}` : ''}`)
  },
  /**
   * Consulta las reservas que se solapan con un mes (yyyy-MM-01 .. fin de mes).
   * Pensado para el calendario; recibe el mes en formato 1-12.
   */
  byMonth(anio: number, mes: number, resourceId?: number) {
    const mm = String(mes).padStart(2, '0')
    const ultimoDia = new Date(anio, mes, 0).getDate()
    return this.byDates({
      desde: `${anio}-${mm}-01T00:00`,
      hasta: `${anio}-${mm}-${String(ultimoDia).padStart(2, '0')}T23:59`,
      recursoReservableId: resourceId,
    })
  },
  create(reservation: ReservationWrite) {
    return apiRequest<Reservation>('/api/reservas', { method: 'POST', body: reservation })
  },
  update(id: number, reservation: ReservationWrite) {
    return apiRequest<Reservation>(`/api/reservas/${id}`, {
      method: 'PUT',
      body: { ...reservation, id },
    })
  },
  remove(id: number) {
    return apiRequest<void>(`/api/reservas/${id}`, { method: 'DELETE' })
  },
}
