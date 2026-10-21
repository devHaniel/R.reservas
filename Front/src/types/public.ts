// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

export interface PublicBookingOption {
  id: number
  nombre: string
  duracionMinutos: number
  precio: number
}

export interface PublicBookingSpace {
  id: number
  nombre: string
  categoria: string
  horarioApertura: string
  horarioCierre: string
  precioDesde: number
  opciones: PublicBookingOption[]
}

export interface PublicAvailabilitySlot {
  horaInicio: string
  horaFin: string
}

export interface PublicAvailability {
  opcionId: number
  nombreOpcion: string
  fecha: string
  duracionMinutos: number
  horarios: PublicAvailabilitySlot[]
}

export interface PublicVerificationRequested {
  mensaje: string
  expiraEnSegundos: number
  reenviarEnSegundos: number
  codigoDesarrollo: string | null
}

export interface PublicVerificationConfirmed {
  token: string
  expiraEnSegundos: number
  mensaje: string
}

export interface PublicReservationInput {
  nombre: string
  telefono: string
  email: string
  tipoServicioId: number
  fechaHoraInicio: string
  tokenVerificacion: string
  nota?: string | null
}

export interface PublicReservation {
  id: number
  folio: string
  nombreCliente: string
  nombreRecurso: string
  nombreServicio: string
  fechaHoraInicio: string
  fechaHoraFin: string
  estado: number
  precio: number
  duracionMinutos: number
  mensaje: string
}
