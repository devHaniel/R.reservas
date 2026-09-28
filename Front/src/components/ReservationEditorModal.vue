<script setup lang="ts">
import { computed, ref, watch } from 'vue'
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
}>()

const clientId = ref<number | ''>('')
const clientQuery = ref('')
const clientResults = ref<Client[]>([])
const clientsSearching = ref(false)
const clientSearchError = ref('')
const resourceId = ref<number | ''>('')
const serviceTypeId = ref<number | ''>('')
const reservationDate = ref('')
const selectedTime = ref('')
const status = ref<ReservationStatus>(0)
const resourceReservations = ref<Reservation[]>([])
const availabilityLoading = ref(false)
const availabilityError = ref('')
let clientRequestId = 0
let availabilityRequestId = 0

const selectableResources = computed(() =>
  props.resources.filter(
    (resource) => resource.activo || resource.id === props.reservation?.recursoReservableId,
  ),
)
const availableServiceTypes = computed(() =>
  props.serviceTypes.filter((service) => service.recursoReservableId === Number(resourceId.value)),
)
const selectedService = computed(() =>
  availableServiceTypes.value.find((item) => item.id === Number(serviceTypeId.value)),
)
const selectedResource = computed(() =>
  props.resources.find((item) => item.id === Number(resourceId.value)),
)
const clientOptions = computed(() => {
  const term = clientQuery.value.trim().toLocaleLowerCase('es')
  const matches = clientResults.value
    .filter((client) =>
      [client.nombre, client.email, client.telefono].some((value) =>
        value.toLocaleLowerCase('es').includes(term),
      ),
    )
    .slice(0, 100)
  const selected = clientResults.value.find((client) => client.id === clientId.value)
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
  if (!term) return
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

function selectResource() {
  serviceTypeId.value = ''
  selectedTime.value = ''
}

function selectService() {
  selectedTime.value = ''
}

watch(
  () => [props.open, resourceId.value, reservationDate.value] as const,
  async ([open, id, date]) => {
    const requestId = ++availabilityRequestId
    resourceReservations.value = []
    availabilityError.value = ''
    availabilityLoading.value = false
    if (!open || id === '' || !date) return

    availabilityLoading.value = true
    try {
      const result = await reservationsService.byDates({
        desde: `${date}T00:00`,
        hasta: `${date}T23:59`,
        recursoReservableId: Number(id),
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
    clientId.value = item?.clienteId ?? ''
    clientQuery.value = ''
    clientResults.value = [...props.clients]
    clientsSearching.value = false
    clientSearchError.value = ''
    resourceId.value = item?.recursoReservableId ?? ''
    serviceTypeId.value = item?.tipoServicioId ?? ''
    reservationDate.value = item
      ? claveFecha(item.fechaHoraInicio)
      : props.initialDate || fechaHoy()
    selectedTime.value = item ? horaDe(item.fechaHoraInicio) : ''
    status.value = item?.estado ?? 0

    if (item && !clientResults.value.some((client) => client.id === item.clienteId)) {
      const requestId = ++clientRequestId
      try {
        const client = await clientsService.get(item.clienteId)
        if (requestId === clientRequestId && props.open)
          clientResults.value = [...clientResults.value, client]
      } catch {}
    }
  },
  { immediate: true },
)

function submit() {
  emit('submit', {
    clienteId: Number(clientId.value),
    tipoServicioId: Number(serviceTypeId.value),
    fechaHoraInicioLocal: startLocal.value,
    estado: Number(status.value) as ReservationStatus,
  })
}
</script>

<template>
  <div v-if="open" class="modal-backdrop" @click.self="emit('close')" @keydown.esc="emit('close')">
    <section
      class="reservation-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="reservation-editor-title"
    >
      <div class="modal-heading">
        <div>
          <p class="eyebrow">{{ reservation ? 'Editar registro' : 'Nueva entrada' }}</p>
          <h2 id="reservation-editor-title">
            {{ reservation ? 'Actualizar reserva' : 'Crear reserva' }}
          </h2>
        </div>
        <button class="modal-close" aria-label="Cerrar formulario" @click="emit('close')">×</button>
      </div>
      <form class="reservation-form" @submit.prevent="submit">
        <label class="field">
          <span>Recurso <b>*</b></span>
          <select v-model.number="resourceId" required :disabled="loading" @change="selectResource">
            <option disabled value="">Selecciona un recurso</option>
            <option v-for="resource in selectableResources" :key="resource.id" :value="resource.id">
              {{ resource.nombre }} · {{ resource.tipoDeporte }}
            </option>
          </select>
        </label>
        <label class="field">
          <span>Servicio <b>*</b></span>
          <select
            v-model.number="serviceTypeId"
            required
            :disabled="loading || resourceId === '' || !availableServiceTypes.length"
            @change="selectService"
          >
            <option disabled value="">
              {{ resourceId === '' ? 'Elige primero un recurso' : 'Selecciona un servicio' }}
            </option>
            <option v-for="service in availableServiceTypes" :key="service.id" :value="service.id">
              {{ service.nombre }} · {{ service.duracionMinutos }} min ·
              {{ service.precio.toFixed(2) }}
            </option>
          </select>
        </label>
        <p v-if="resourceId !== '' && !availableServiceTypes.length" class="form-hint">
          Este recurso no tiene servicios disponibles.
        </p>
        <p v-if="selectedResource" class="form-hint">
          Horario del recurso: {{ selectedResource.horarioAperturaDefault.slice(0, 5) }}–{{
            selectedResource.horarioCierreDefault.slice(0, 5)
          }}
        </p>
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
          <span>Horario disponible <b>*</b></span>
          <select
            v-model="selectedTime"
            required
            :disabled="availabilityLoading || !selectedService || !availableSlots.length"
          >
            <option disabled value="">
              {{
                availabilityLoading
                  ? 'Consultando horarios…'
                  : !selectedService
                    ? 'Elige recurso y servicio primero'
                    : 'Selecciona un horario'
              }}
            </option>
            <option v-for="slot in availableSlots" :key="slot.value" :value="slot.value">
              {{ slot.label }}
            </option>
          </select>
        </label>
        <p v-if="availabilityError" class="form-error" role="alert">{{ availabilityError }}</p>
        <p
          v-else-if="selectedService && !availabilityLoading && !availableSlots.length"
          class="form-warning"
          role="status"
        >
          No hay horarios disponibles para este servicio en la fecha elegida.
        </p>
        <p v-if="selectedService" class="form-hint">
          Duración: {{ selectedService.duracionMinutos }} minutos · Fin estimado:
          {{ estimatedEnd || 'selecciona un horario' }}
        </p>
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
              {{ client.nombre }}<template v-if="client.email"> · {{ client.email }}</template>
            </option>
          </select>
        </label>
        <p v-if="clientQuery && !clientOptions.length && !clientsSearching" class="form-hint">
          No se encontraron clientes. Prueba otro dato de búsqueda.
        </p>
        <p v-if="selectedResource" class="form-hint">
          Recurso seleccionado: {{ selectedResource.nombre }} · {{ selectedResource.tipoDeporte }}
        </p>
        <label v-if="reservation" class="field"
          ><span>Estado</span
          ><select v-model.number="status">
            <option :value="0">Pendiente</option>
            <option :value="1">Confirmada</option>
            <option :value="2">Cancelada</option>
            <option :value="3">Completada</option>
          </select></label
        >
        <p v-if="reservation" class="form-hint">
          La hora de fin se calcula en el servidor según la duración del servicio.
        </p>
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
              !clientId ||
              !resourceId ||
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
