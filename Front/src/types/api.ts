export type ApiRole = 'Admin' | 'Vendedor'
export type AppSection =
  'overview' | 'reservations' | 'calendar' | 'clients' | 'resources' | 'services' | 'users'

export enum ReservationStatus {
  Pending = 0,
  Confirmed = 1,
  Cancelled = 2,
  Completed = 3,
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
}

export interface AuthUser {
  id: string
  email: string
  name: string
  roles: ApiRole[]
}

export interface Client {
  id: number
  nombre: string
  telefono: string
  email: string | null
}

export type ClientWrite = Omit<Client, 'id'> & { id?: number }

export interface PagedResult<T> {
  items: T[]
  pagina: number
  cantidad: number
  total: number
  totalPaginas: number
}

export interface Reservation {
  id: number
  clienteId: number
  nombreCliente: string
  recursoReservableId: number
  nombreRecurso: string
  tipoServicioId: number
  nombreServicio: string
  fechaHoraInicio: string
  fechaHoraFin: string
  estado: ReservationStatus
  precio: number
  duracionMinutos: number
  nota: string | null
  fechaCreacion: string
}

export type ReservationWrite = Pick<
  Reservation,
  'clienteId' | 'tipoServicioId' | 'fechaHoraInicio' | 'estado' | 'nota'
> & { id?: number }

export interface ReservationEditorValue {
  clienteId: number
  tipoServicioId: number
  fechaHoraInicioLocal: string
  estado: ReservationStatus
  nota: string
}

export interface ReservableResource {
  id: number
  nombre: string
  categoria: string
  activo: boolean
  horarioAperturaDefault: string
  horarioCierreDefault: string
}

export type ResourceWrite = Omit<ReservableResource, 'id'> & { id?: number }

export interface ServiceType {
  id: number
  recursoReservableId: number
  nombre: string
  duracionMinutos: number
  precio: number
  activo: boolean
}

export type ServiceTypeWrite = Omit<ServiceType, 'id'> & { id?: number }

export interface LoginInput {
  email: string
  password: string
}

export interface RegisterVendorInput {
  nombre: string
  email: string
  password: string
}

export interface RegisterVendorResult {
  id: number
  mensaje: string
}
