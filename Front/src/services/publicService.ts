// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { apiRequest } from './apiClient'
import type {
  PublicAvailability,
  PublicBookingSpace,
  PublicReservation,
  PublicReservationInput,
  PublicVerificationConfirmed,
  PublicVerificationRequested,
} from '../types/public'

export const publicService = {
  catalog() {
    return apiRequest<PublicBookingSpace[]>('/api/public/catalogo', { auth: false })
  },
  availability(optionId: number, fecha: string) {
    const query = new URLSearchParams({ fecha })
    return apiRequest<PublicAvailability>(
      `/api/public/opciones/${optionId}/disponibilidad?${query}`,
      { auth: false },
    )
  },
  requestVerification(email: string) {
    return apiRequest<PublicVerificationRequested>('/api/public/verificacion/solicitar', {
      method: 'POST',
      body: { email },
      auth: false,
    })
  },
  confirmVerification(email: string, codigo: string) {
    return apiRequest<PublicVerificationConfirmed>('/api/public/verificacion/confirmar', {
      method: 'POST',
      body: { email, codigo },
      auth: false,
    })
  },
  createReservation(input: PublicReservationInput) {
    return apiRequest<PublicReservation>('/api/public/reservas', {
      method: 'POST',
      body: input,
      auth: false,
    })
  },
}
