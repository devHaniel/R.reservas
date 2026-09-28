// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { ref } from 'vue'
import { defineStore } from 'pinia'
import { reservationsService } from '../services/reservationsService'
import type { Reservation, ReservationWrite } from '../types/api'

export const useReservationsStore = defineStore('reservations', () => {
  const items = ref<Reservation[]>([])
  const loading = ref(false)
  const error = ref('')

  async function load() {
    loading.value = true
    error.value = ''
    try {
      items.value = await reservationsService.list()
      return items.value
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudieron cargar las reservas.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function byClient(clientId: number) {
    items.value = await reservationsService.byClient(clientId)
    return items.value
  }

  async function byResource(resourceId: number) {
    items.value = await reservationsService.byResource(resourceId)
    return items.value
  }

  async function byDates(filters: {
    desde?: string
    hasta?: string
    recursoReservableId?: number
  } = {}) {
    items.value = await reservationsService.byDates(filters)
    return items.value
  }

  async function byMonth(anio: number, mes: number, resourceId?: number) {
    items.value = await reservationsService.byMonth(anio, mes, resourceId)
    return items.value
  }

  async function create(reservation: ReservationWrite) {
    try {
      error.value = ''
      const created = await reservationsService.create(reservation)
      items.value = [...items.value.filter((item) => item.id !== created.id), created]
      try {
        await load()
      } catch {
        // Keep the confirmed POST result in the list if the follow-up refresh fails.
      }
      return created
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo guardar la reserva.'
      throw cause
    }
  }

  async function update(id: number, reservation: ReservationWrite) {
    try {
      error.value = ''
      const updated = await reservationsService.update(id, reservation)
      await load()
      return updated
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo actualizar la reserva.'
      throw cause
    }
  }

  async function remove(id: number) {
    try {
      error.value = ''
      await reservationsService.remove(id)
      await load()
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo eliminar la reserva.'
      throw cause
    }
  }

  return {
    items,
    loading,
    error,
    load,
    byClient,
    byResource,
    byDates,
    byMonth,
    create,
    update,
    remove,
  }
})
