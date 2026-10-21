// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { ReservationStatus } from '../types/api'
import type {
  Client,
  ReservableResource,
  Reservation,
  ReservationEditorValue,
  ServiceType,
} from '../types/api'
import { clientsService } from '../services/clientsService'
import { reservationsService } from '../services/reservationsService'
import {
  aDateLocal,
  ahoraApi,
  claveFecha,
  fechaHoy,
  formatearHora,
  horaDe,
  sumarMinutos,
} from '../utils/fechas'
import { formatearMoneda } from '../utils/formato'

const props = defineProps<{
  open: boolean
  initialDate: string
  reservation: Reservation | null
  clients: Client[]
  resources: ReservableResource[]
  serviceTypes: ServiceType[]
  loading: boolean
  error: string
}>()

const emit = defineEmits<{
  close: []
  submit: [value: ReservationEditorValue]
  clientCreated: [client: Client]
}>()

const clientId = ref<number | ''>('')
const selectedClient = ref<Client | null>(null)
const selectedClientArchived = ref(false)
const clientQuery = ref('')
const clientResults = ref<Client[]>([])
const clientsSearching = ref(false)
const clientSearchError = ref('')
const serviceTypeId = ref<number | ''>('')
const reservationDate = ref('')
const selectedTime = ref('')
const status = ref<ReservationStatus>(0)
const note = ref('')
const resourceReservations = ref<Reservation[]>([])
const availabilityLoading = ref(false)
const availabilityError = ref('')
const showClientForm = ref(false)
const clientSaving = ref(false)
const clientFormError = ref('')
const clientForm = ref<{ nombre: string; telefono: string; email: string }>({
  nombre: '',
  telefono: '',
  email: '',
})
let clientRequestId = 0
let availabilityRequestId = 0
let clientSearchTimer: ReturnType<typeof setTimeout> | undefined

const selectedReservationService = computed<ServiceType | null>(() => {
  const reservation = props.reservation
  if (!reservation) return null
  return {
    id: reservation.tipoServicioId,
    recursoReservableId: reservation.recursoReservableId,
    nombre: reservation.nombreServicio,
    duracionMinutos: reservation.duracionMinutos,
    precio: reservation.precio,
    activo: false,
  }
})

const selectedService = computed<ServiceType | undefined>(() => {
  const found = props.serviceTypes.find((item) => item.id === Number(serviceTypeId.value))
  if (found) return found
  return selectedReservationService.value?.id === Number(serviceTypeId.value)
    ? selectedReservationService.value
    : undefined
})

/**
 * Opciones que se pueden elegir. Si se edita una reserva histórica cuya opción
 * fue archivada, se conserva esa opción para no romper el registro.
 */
const bookableOptions = computed(() => {
  const currentId = props.reservation?.tipoServicioId
  const options = props.serviceTypes.filter((item) => {
    if (item.id === currentId) return true
    if (!item.activo) return false
    // Una opción solo se puede ofrecer si su espacio sigue activo.
    const resource = props.resources.find((resource) => resource.id === item.recursoReservableId)
    return resource?.activo === true
  })
  const current = selectedReservationService.value
  if (current && !options.some((item) => item.id === current.id)) options.push(current)
  return options.sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'))
})

const bookableGroups = computed(() => {
  const groups = new Map<number, { id: number; nombre: string; options: ServiceType[] }>()
  for (const option of bookableOptions.value) {
    const resource = props.resources.find((item) => item.id === option.recursoReservableId)
    const group = groups.get(option.recursoReservableId) ?? {
      id: option.recursoReservableId,
      nombre: resource?.nombre ?? props.reservation?.nombreRecurso ?? `Espacio #${option.recursoReservableId}`,
      options: [],
    }
    group.options.push(option)
    groups.set(option.recursoReservableId, group)
  }
  return [...groups.values()].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'))
})

const selectedResource = computed<ReservableResource | undefined>(() => {
  const service = selectedService.value
  if (!service) return undefined
  const resource = props.resources.find((item) => item.id === service.recursoReservableId)
  if (resource) return resource

  // Reserva histórica de un espacio archivado: permitimos editar el registro
  // sin inventar un horario de apertura.
  const reservation = props.reservation
  if (!reservation || reservation.recursoReservableId !== service.recursoReservableId)
    return undefined
  return {
    id: reservation.recursoReservableId,
    nombre: reservation.nombreRecurso || `Espacio #${reservation.recursoReservableId}`,
    categoria: '',
    activo: false,
    horarioAperturaDefault: '00:00',
    horarioCierreDefault: '23:59',
  }
})

const selectedResourceIsHistorical = computed(() => {
  const service = selectedService.value
  return (
    !!service &&
    !props.resources.some((resource) => resource.id === service.recursoReservableId)
  )
})

const clientOptions = computed(() => {
  const term = clientQuery.value.trim().toLocaleLowerCase('es')
  const matches = clientResults.value
    .filter((client) =>
      [client.nombre, client.email ?? '', client.telefono].some((value) =>
        value.toLocaleLowerCase('es').includes(term),
      ),
    )
    .slice(0, 100)
  // Mantenemos visible el cliente elegido aunque la búsqueda actual ya no lo
  // devuelva (por ejemplo, después de crear uno nuevo o cambiar el término).
  const selected = selectedClient.value
  return selected && !matches.some((client) => client.id === selected.id)
    ? [selected, ...matches]
    : matches
})

const startLocal = computed(() =>
  reservationDate.value && selectedTime.value
    ? `${reservationDate.value}T${selectedTime.value}`
    : '',
)
const minimumDate = computed(() => (props.reservation ? undefined : fechaHoy()))

function minutesFromTime(value: string) {
  const [hours, minutes] = value.slice(0, 5).split(':').map(Number)
  return (hours ?? 0) * 60 + (minutes ?? 0)
}

function timeFromMinutes(value: number) {
  const hours = Math.floor(value / 60)
  const minutes = value % 60
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
}

const estimatedEnd = computed(() => {
  if (!startLocal.value || !selectedService.value) return ''
  return formatearHora(sumarMinutos(startLocal.value, selectedService.value.duracionMinutos))
})

const availableSlots = computed(() => {
  if (
    !selectedResource.value ||
    !selectedService.value ||
    !reservationDate.value ||
    availabilityLoading.value ||
    availabilityError.value
  )
    return []

  const opening = minutesFromTime(selectedResource.value.horarioAperturaDefault)
  const closing = minutesFromTime(selectedResource.value.horarioCierreDefault)
  const duration = selectedService.value.duracionMinutos
  const nowLocal = ahoraApi()
  const reserved = resourceReservations.value
    .filter(
      (item) =>
        item.estado !== ReservationStatus.Cancelled &&
        item.id !== props.reservation?.id &&
        claveFecha(item.fechaHoraInicio) === reservationDate.value,
    )
    .map((item) => ({
      start: aDateLocal(item.fechaHoraInicio).getTime(),
      end: aDateLocal(item.fechaHoraFin).getTime(),
    }))
  const slots: { value: string; label: string }[] = []

  for (let start = opening; start + duration <= closing; start += 30) {
    const time = timeFromMinutes(start)
    const startLocalValue = `${reservationDate.value}T${time}`
    if (!props.reservation && startLocalValue <= nowLocal) continue

    const startMs = aDateLocal(startLocalValue).getTime()
    const endMs = startMs + duration * 60_000
    const overlaps = reserved.some((item) => startMs < item.end && endMs > item.start)
    if (overlaps) continue

    const endTime = formatearHora(sumarMinutos(startLocalValue, duration))
    slots.push({ value: time, label: `${time}–${endTime}` })
  }

  if (
    props.reservation &&
    claveFecha(props.reservation.fechaHoraInicio) === reservationDate.value &&
    !slots.some((slot) => slot.value === selectedTime.value)
  ) {
    const currentTime = horaDe(props.reservation.fechaHoraInicio)
    slots.unshift({ value: currentTime, label: `${currentTime} · horario actual` })
  }

  const uniqueSlots = new Map<string, { value: string; label: string }>()
  for (const slot of slots) uniqueSlots.set(slot.value, slot)
  return [...uniqueSlots.values()]
})

async function searchClients() {
  const term = clientQuery.value.trim()
  if (!term) {
    clientResults.value = [...props.clients]
    return
  }
  const requestId = ++clientRequestId
  clientsSearching.value = true
  clientSearchError.value = ''
  try {
    const results = await clientsService.search(term)
    if (requestId === clientRequestId) clientResults.value = results
  } catch (cause) {
    if (requestId === clientRequestId) {
      clientSearchError.value =
        cause instanceof Error ? cause.message : 'No se pudo buscar el cliente.'
    }
  } finally {
    if (requestId === clientRequestId) clientsSearching.value = false
  }
}

watch(clientQuery, (term) => {
  if (clientSearchTimer) clearTimeout(clientSearchTimer)
  if (!term.trim()) {
    clientRequestId += 1
    clientsSearching.value = false
    clientResults.value = [...props.clients]
    return
  }
  clientSearchTimer = setTimeout(() => void searchClients(), 350)
})

watch(clientId, (id) => {
  if (id === '') {
    selectedClient.value = null
    return
  }
  const found =
    clientResults.value.find((client) => client.id === id) ??
    props.clients.find((client) => client.id === id)
  if (found) {
    selectedClient.value = found
    selectedClientArchived.value = false
  }
})

function toggleClientForm() {
  showClientForm.value = !showClientForm.value
  clientFormError.value = ''
  if (showClientForm.value) {
    clientForm.value = { nombre: '', telefono: '', email: '' }
  }
}

async function saveClient() {
  clientFormError.value = ''
  if (!clientForm.value.nombre.trim() || !clientForm.value.telefono.trim()) {
    clientFormError.value = 'El nombre y el teléfono son obligatorios.'
    return
  }
  clientSaving.value = true
  try {
    const created = await clientsService.create({
      nombre: clientForm.value.nombre.trim(),
      telefono: clientForm.value.telefono.trim(),
      email: clientForm.value.email.trim() || null,
    })
    clientResults.value = [created, ...clientResults.value]
    selectedClient.value = created
    selectedClientArchived.value = false
    clientId.value = created.id
    clientQuery.value = ''
    showClientForm.value = false
    emit('clientCreated', created)
  } catch (cause) {
    clientFormError.value =
      cause instanceof Error ? cause.message : 'No se pudo crear el cliente.'
  } finally {
    clientSaving.value = false
  }
}

function selectService() {
  selectedTime.value = ''
}

watch(
  () => [props.open, serviceTypeId.value, reservationDate.value] as const,
  async ([open, id, date]) => {
    const requestId = ++availabilityRequestId
    resourceReservations.value = []
    availabilityError.value = ''
    availabilityLoading.value = false
    const service = selectedService.value
    if (!open || id === '' || !service || !date) return

    availabilityLoading.value = true
    try {
      const result = await reservationsService.byDates({
        desde: `${date}T00:00`,
        hasta: `${date}T23:59`,
        recursoReservableId: service.recursoReservableId,
      })
      if (requestId === availabilityRequestId) resourceReservations.value = result
    } catch (cause) {
      if (requestId === availabilityRequestId) {
        availabilityError.value =
          cause instanceof Error ? cause.message : 'No se pudo consultar la disponibilidad.'
      }
    } finally {
      if (requestId === availabilityRequestId) availabilityLoading.value = false
    }
  },
)

watch(
  () => props.open,
  async (open) => {
    if (!open) {
      clientRequestId += 1
      clientsSearching.value = false
      return
    }
    const item = props.reservation
    const knownClient = item
      ? (props.clients.find((client) => client.id === item.clienteId) ?? null)
      : null
    // Si el cliente ya no está en el directorio, mostramos al menos el nombre
    // guardado en la reserva para no perder el contexto al editarla.
    selectedClient.value =
      knownClient ??
      (item
        ? {
            id: item.clienteId,
            nombre: item.nombreCliente || `Cliente #${item.clienteId}`,
            telefono: '',
            email: null,
          }
        : null)
    selectedClientArchived.value = Boolean(item && !knownClient)
    clientId.value = item?.clienteId ?? ''
    clientQuery.value = ''
    clientResults.value = [...props.clients]
    clientsSearching.value = false
    clientSearchError.value = ''
    serviceTypeId.value = item?.tipoServicioId ?? ''
    reservationDate.value = item
      ? claveFecha(item.fechaHoraInicio)
      : props.initialDate || fechaHoy()
    selectedTime.value = item ? horaDe(item.fechaHoraInicio) : ''
    status.value = item?.estado ?? ReservationStatus.Pending
    note.value = item?.nota ?? ''
    showClientForm.value = false
    clientFormError.value = ''

    if (item && !clientResults.value.some((client) => client.id === item.clienteId)) {
      const requestId = ++clientRequestId
      try {
        const client = await clientsService.get(item.clienteId)
        if (requestId === clientRequestId && props.open) {
          clientResults.value = [...clientResults.value, client]
          selectedClientArchived.value = false
          if (clientId.value === client.id) selectedClient.value = client
        }
      } catch {}
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  if (clientSearchTimer) clearTimeout(clientSearchTimer)
})

function submit() {
  emit('submit', {
    clienteId: Number(clientId.value),
    tipoServicioId: Number(serviceTypeId.value),
    fechaHoraInicioLocal: startLocal.value,
    estado: Number(status.value) as ReservationStatus,
    nota: note.value.trim(),
  })
}
</script>

<template>
  <div v-if="open" class="modal-backdrop" @click.self="emit('close')" @keydown.esc="emit('close')">
    <section
      class="reservation-modal reservation-modal-wide"
      role="dialog"
      aria-modal="true"
      aria-labelledby="reservation-editor-title"
    >
      <div class="modal-heading">
        <div>
          <p class="eyebrow">{{ reservation ? 'Editar reserva' : 'Nueva reserva' }}</p>
          <h2 id="reservation-editor-title">
            {{ reservation ? `Reserva #${reservation.id}` : 'Agenda una reserva' }}
          </h2>
        </div>
        <button class="modal-close" aria-label="Cerrar formulario" @click="emit('close')">×</button>
      </div>

      <form class="reservation-form" @submit.prevent="submit">
        <section class="form-section">
          <p class="form-section-title">1 · ¿Qué se reserva?</p>
          <label class="field">
            <span>Opción de reserva <b>*</b></span>
            <select
              v-model.number="serviceTypeId"
              required
              :disabled="loading"
              @change="selectService"
            >
              <option disabled value="">
                {{ bookableOptions.length ? 'Selecciona una opción' : 'No hay opciones disponibles' }}
              </option>
              <optgroup
                v-for="group in bookableGroups"
                :key="group.id"
                :label="group.nombre"
              >
                <option v-for="service in group.options" :key="service.id" :value="service.id">
                  {{ service.nombre }} · {{ service.duracionMinutos }} min ·
                  {{ formatearMoneda(service.precio) }}
                  {{ service.activo ? '' : ' (archivada)' }}
                </option>
              </optgroup>
            </select>
          </label>
          <p v-if="!bookableOptions.length" class="form-warning" role="status">
            Todavía no hay opciones de reserva. Un administrador debe crear un espacio y al menos
            una opción en Configuración.
          </p>
          <p v-if="selectedResource" class="form-hint">
            Espacio: <strong>{{ selectedResource.nombre }}</strong>
            <template v-if="selectedResource.categoria"> · {{ selectedResource.categoria }}</template>
            <template v-if="!selectedResourceIsHistorical">
              · Horario {{ selectedResource.horarioAperturaDefault.slice(0, 5) }}–{{
                selectedResource.horarioCierreDefault.slice(0, 5)
              }}
            </template>
            <template v-else> · espacio archivado</template>
          </p>
        </section>

        <section class="form-section">
          <p class="form-section-title">2 · ¿Cuándo?</p>
          <div class="field-row">
            <label class="field">
              <span>Fecha <b>*</b></span>
              <input
                v-model="reservationDate"
                type="date"
                required
                :min="minimumDate"
                @change="selectedTime = ''"
              />
            </label>
            <label class="field">
              <span>Horario <b>*</b></span>
              <select
                v-model="selectedTime"
                required
                :disabled="availabilityLoading || !selectedService || !availableSlots.length"
              >
                <option disabled value="">
                  {{
                    availabilityLoading
                      ? 'Consultando…'
                      : !selectedService
                        ? 'Elige una opción'
                        : 'Selecciona un horario'
                  }}
                </option>
                <option v-for="slot in availableSlots" :key="slot.value" :value="slot.value">
                  {{ slot.label }}
                </option>
              </select>
            </label>
          </div>
          <p v-if="availabilityError" class="form-error" role="alert">{{ availabilityError }}</p>
          <p
            v-else-if="selectedService && !availabilityLoading && !availableSlots.length"
            class="form-warning"
            role="status"
          >
            No hay horarios libres para esa opción en la fecha elegida.
          </p>
          <p v-if="selectedService" class="form-hint">
            Duración: {{ selectedService.duracionMinutos }} min · Fin estimado:
            {{ estimatedEnd || '—' }} · Total: {{ formatearMoneda(selectedService.precio) }}
          </p>
        </section>

        <section class="form-section">
          <div class="form-section-heading">
            <p class="form-section-title">3 · ¿Para quién?</p>
            <button class="text-action" type="button" @click="toggleClientForm">
              {{ showClientForm ? 'Cancelar alta' : '+ Nuevo cliente' }}
            </button>
          </div>

          <div v-if="showClientForm" class="quick-client-form">
            <p class="form-hint">Registra al cliente sin salir de la reserva.</p>
            <label class="field"
              ><span>Nombre <b>*</b></span
              ><input v-model.trim="clientForm.nombre" required maxlength="120" /></label
            ><label class="field"
              ><span>Teléfono <b>*</b></span
              ><input v-model.trim="clientForm.telefono" type="tel" required maxlength="30"
            /></label>
            <label class="field"
              ><span>Email <small>opcional</small></span
              ><input v-model.trim="clientForm.email" type="email" maxlength="256"
            /></label>
            <p v-if="clientFormError" class="form-error" role="alert">{{ clientFormError }}</p>
            <button
              class="button button-primary"
              type="button"
              :disabled="clientSaving"
              @click="saveClient"
            >
              {{ clientSaving ? 'Guardando…' : 'Guardar cliente' }}
            </button>
          </div>

          <div class="field">
            <span>Buscar cliente</span>
            <div class="client-search-row">
              <input
                v-model.trim="clientQuery"
                type="search"
                placeholder="Nombre, correo o teléfono"
                aria-label="Buscar cliente por nombre, correo o teléfono"
                @keydown.enter.prevent="searchClients"
              />
              <button
                class="button button-quiet"
                type="button"
                :disabled="clientsSearching || !clientQuery.trim()"
                @click="searchClients"
              >
                {{ clientsSearching ? 'Buscando…' : 'Buscar' }}
              </button>
            </div>
          </div>
          <p v-if="clientSearchError" class="form-error" role="alert">{{ clientSearchError }}</p>
          <label class="field">
            <span>Cliente <b>*</b></span>
            <select v-model.number="clientId" required :disabled="!clientOptions.length">
              <option disabled value="">Selecciona un cliente</option>
              <option v-for="client in clientOptions" :key="client.id" :value="client.id">
                {{ client.nombre }}<template v-if="client.email"> · {{ client.email }}</template
                ><template v-if="client.id === clientId && selectedClientArchived">
                  · archivado</template
                >
              </option>
            </select>
          </label>
          <p v-if="clientQuery && !clientOptions.length && !clientsSearching" class="form-hint">
            No se encontraron clientes. Puedes usar “+ Nuevo cliente”.
          </p>
        </section>

        <section v-if="reservation" class="form-section">
          <p class="form-section-title">4 · Seguimiento</p>
          <div class="field-row">
            <label class="field"
              ><span>Estado</span
              ><select v-model.number="status">
                <option :value="0">Pendiente</option>
                <option :value="1">Confirmada</option>
                <option :value="2">Cancelada</option>
                <option :value="3">Completada</option>
              </select></label
            >
            <label class="field"
              ><span>Nota <small>opcional</small></span
              ><input v-model="note" maxlength="300" placeholder="Ej. pidió equipo extra"
            /></label>
          </div>
        </section>

        <label v-else class="field"
          ><span>Nota <small>opcional</small></span
          ><input v-model="note" maxlength="300" placeholder="Ej. pidió equipo extra"
        /></label>

        <p v-if="error" class="form-error" role="alert">{{ error }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="emit('close')">Descartar</button
          ><button
            class="button button-primary"
            type="submit"
            :disabled="
              loading ||
              availabilityLoading ||
              clientsSearching ||
              clientSaving ||
              !clientId ||
              !serviceTypeId ||
              !selectedTime
            "
          >
            {{ loading ? 'Guardando…' : reservation ? 'Guardar cambios' : 'Crear reserva' }}
          </button>
        </div>
      </form>
    </section>
  </div>
</template>
