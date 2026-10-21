// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ReservationEditorModal from './ReservationEditorModal.vue'
import { useAuthStore } from '../stores/auth'
import { useCatalogStore } from '../stores/catalog'
import { useClientsStore } from '../stores/clients'
import { useReservationsStore } from '../stores/reservations'
import { ReservationStatus } from '../types/api'
import type {
  Client,
  Reservation,
  ReservationEditorValue,
  ReservationWrite,
} from '../types/api'
import { formatearFecha, limpiarFechaApi, fechaHoy } from '../utils/fechas'
import { formatearMoneda } from '../utils/formato'

const route = useRoute()
const router = useRouter()
const reservations = useReservationsStore()
const clients = useClientsStore()
const catalog = useCatalogStore()
const auth = useAuthStore()
const query = ref('')
const statusFilter = ref<'all' | ReservationStatus>('all')
const modalOpen = ref(false)
const editing = ref<Reservation | null>(null)
const editorInitialDate = ref('')
const formError = ref('')
const successMessage = ref('')

const filteredReservations = computed(() => {
  const text = query.value.trim().toLocaleLowerCase('es')
  return reservations.items
    .filter((item) => {
      const searchable = [
        item.nombreCliente,
        item.nombreRecurso,
        item.nombreServicio,
        String(item.clienteId),
        String(item.id),
      ]
        .join(' ')
        .toLocaleLowerCase('es')
      return (
        (!text || searchable.includes(text)) &&
        (statusFilter.value === 'all' || item.estado === statusFilter.value)
      )
    })
    .sort((a, b) => a.fechaHoraInicio.localeCompare(b.fechaHoraInicio))
})

function formatDate(value: string) {
  return formatearFecha(value)
}

function statusLabel(status: ReservationStatus) {
  return ['Pendiente', 'Confirmada', 'Cancelada', 'Completada'][status] ?? 'Desconocida'
}

function statusClass(status: ReservationStatus) {
  return (
    ['status-pending', 'status-confirmed', 'status-cancelled', 'status-completed'][status] ??
    'status-pending'
  )
}

function openCreate(date?: string | null) {
  editing.value = null
  editorInitialDate.value = date ?? fechaHoy()
  formError.value = ''
  successMessage.value = ''
  modalOpen.value = true
}

function openEdit(reservation: Reservation) {
  editing.value = reservation
  editorInitialDate.value = ''
  formError.value = ''
  successMessage.value = ''
  modalOpen.value = true
}

function clearRouteIntent() {
  if (!route.query.reserva && !route.query.fecha) return
  void router.replace({ name: 'reservations' })
}

// El calendario abre una reserva o una fecha concreta mediante query params.
// Así el enlace sobrevive a un refresco de la página.
watch(
  () => [route.query.reserva, route.query.fecha, reservations.items.length] as const,
  () => {
    const reservaId = Number(route.query.reserva)
    const fecha = typeof route.query.fecha === 'string' ? route.query.fecha : ''

    if (Number.isInteger(reservaId) && reservaId > 0) {
      const reservation = reservations.items.find((item) => item.id === reservaId)
      if (reservation) {
        openEdit(reservation)
        clearRouteIntent()
      }
      return
    }

    if (fecha) {
      openCreate(fecha)
      clearRouteIntent()
    }
  },
  { immediate: true },
)

async function save(value: ReservationEditorValue) {
  formError.value = ''
  const fechaHoraInicio = limpiarFechaApi(value.fechaHoraInicioLocal)
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(fechaHoraInicio)) {
    formError.value = 'Selecciona una fecha y hora válidas.'
    return
  }
  if (!value.clienteId || !value.tipoServicioId) {
    formError.value = 'Selecciona un cliente y una opción de reserva.'
    return
  }
  const draft: ReservationWrite = {
    clienteId: value.clienteId,
    tipoServicioId: value.tipoServicioId,
    fechaHoraInicio,
    estado: value.estado,
    nota: value.nota || null,
  }
  try {
    if (editing.value) {
      const updated = await reservations.update(editing.value.id, draft)
      successMessage.value = `Reserva #${updated.id} actualizada · ${formatDate(updated.fechaHoraInicio)}`
    } else {
      const created = await reservations.create(draft)
      successMessage.value = `Reserva #${created.id} creada · ${created.nombreCliente} · ${
        created.nombreRecurso
      } / ${created.nombreServicio} · ${formatDate(created.fechaHoraInicio)} · ${formatearMoneda(
        created.precio,
      )}`
    }
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar la reserva.'
  }
}

async function changeStatus(reservation: Reservation, estado: ReservationStatus) {
  const labels: Record<number, string> = {
    1: 'confirmar',
    2: 'cancelar',
    3: 'marcar como completada',
  }
  if (!window.confirm(`¿Deseas ${labels[estado] ?? 'cambiar'} la reserva #${reservation.id}?`))
    return
  try {
    await reservations.changeStatus(reservation.id, estado)
    successMessage.value = `Reserva #${reservation.id}: ${statusLabel(estado)}.`
  } catch {
    // The store exposes the API error in the page.
  }
}

async function remove(reservation: Reservation) {
  if (!auth.isAdmin || !window.confirm(`¿Eliminar permanentemente la reserva #${reservation.id}?`))
    return
  try {
    await reservations.remove(reservation.id)
    successMessage.value = `Reserva #${reservation.id} eliminada.`
  } catch {
    // The store exposes the API error in the page.
  }
}

function handleClientCreated(client: Client) {
  successMessage.value = `Cliente "${client.nombre}" creado y seleccionado.`
}
</script>

<template>
  <section class="list-toolbar">
    <label class="search-box"
      ><span>⌕</span
      ><input
        v-model="query"
        type="search"
        placeholder="Buscar por cliente, espacio u opción"
        aria-label="Buscar reservas"
    /></label>
    <div class="filter-group">
      <label for="reservation-status-filter">Estado</label
      ><select id="reservation-status-filter" v-model="statusFilter">
        <option value="all">Todos</option>
        <option :value="0">Pendiente</option>
        <option :value="1">Confirmada</option>
        <option :value="2">Cancelada</option>
        <option :value="3">Completada</option>
      </select>
    </div>
    <button class="button button-primary" @click="openCreate()">+ Nueva reserva</button>
  </section>
  <p v-if="reservations.error" class="form-error" role="alert">{{ reservations.error }}</p>
  <p v-if="successMessage" class="form-success" role="status">{{ successMessage }}</p>
  <section class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Reservas</h2>
        <span>{{ filteredReservations.length }} visibles</span>
      </div>
      <button
        class="button button-quiet"
        :disabled="reservations.loading"
        @click="reservations.load()"
      >
        Actualizar
      </button>
    </div>
    <div v-if="reservations.loading" class="empty-state"><strong>Cargando reservas…</strong></div>
    <div v-else-if="filteredReservations.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>FOLIO</th>
            <th>CLIENTE</th>
            <th>INICIO</th>
            <th>FIN</th>
            <th>ESPACIO / OPCIÓN</th>
            <th>TOTAL</th>
            <th>ESTADO</th>
            <th><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in filteredReservations" :key="item.id">
            <td>#{{ item.id }}</td>
            <td>
              <strong>{{ item.nombreCliente || `Cliente #${item.clienteId}` }}</strong>
            </td>
            <td>{{ formatDate(item.fechaHoraInicio) }}</td>
            <td>{{ formatDate(item.fechaHoraFin) }}</td>
            <td>
              {{ item.nombreRecurso || `Espacio #${item.recursoReservableId}` }}<small
                class="table-subline"
                >{{ item.nombreServicio || `Opción #${item.tipoServicioId}` }}</small
              >
            </td>
            <td>{{ formatearMoneda(item.precio) }}</td>
            <td>
              <span :class="['status-pill', statusClass(item.estado)]">{{
                statusLabel(item.estado)
              }}</span>
            </td>
            <td>
              <div class="row-actions">
                <button class="row-edit" @click="openEdit(item)">Editar</button
                ><button
                  v-if="item.estado === ReservationStatus.Pending"
                  class="row-edit"
                  @click="changeStatus(item, ReservationStatus.Confirmed)"
                >
                  Confirmar</button
                ><button
                  v-if="item.estado === ReservationStatus.Confirmed"
                  class="row-edit"
                  @click="changeStatus(item, ReservationStatus.Completed)"
                >
                  Completar</button
                ><button
                  v-if="item.estado === ReservationStatus.Cancelled"
                  class="row-edit"
                  @click="changeStatus(item, ReservationStatus.Confirmed)"
                >
                  Reactivar</button
                ><button
                  v-if="
                    item.estado !== ReservationStatus.Cancelled &&
                    item.estado !== ReservationStatus.Completed
                  "
                  class="row-cancel"
                  @click="changeStatus(item, ReservationStatus.Cancelled)"
                >
                  Cancelar</button
                ><button v-if="auth.isAdmin" class="row-cancel" @click="remove(item)">
                  Eliminar
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">▤</span
      ><strong>{{
        query || statusFilter !== 'all' ? 'Sin coincidencias' : 'No hay reservas'
      }}</strong
      ><span>{{
        query || statusFilter !== 'all'
          ? 'Cambia los filtros e inténtalo de nuevo.'
          : 'Crea la primera reserva para empezar.'
      }}</span
      ><button v-if="!query && statusFilter === 'all'" class="text-action" @click="openCreate()">
        Crear reserva <span>↗</span>
      </button>
    </div>
  </section>
  <ReservationEditorModal
    :open="modalOpen"
    :initial-date="editorInitialDate"
    :reservation="editing"
    :clients="clients.page.items"
    :resources="catalog.resources"
    :service-types="catalog.serviceTypes"
    :loading="reservations.loading || clients.loading || catalog.loading"
    :error="formError || catalog.error"
    @close="modalOpen = false"
    @submit="save"
    @client-created="handleClientCreated"
  />
</template>
