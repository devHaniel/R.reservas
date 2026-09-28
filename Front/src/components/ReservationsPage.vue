  <script setup lang="ts">
  import { computed, ref, watch } from 'vue'
  import ReservationEditorModal from './ReservationEditorModal.vue'
  import { useAuthStore } from '../stores/auth'
  import { useCatalogStore } from '../stores/catalog'
  import { useClientsStore } from '../stores/clients'
  import { useReservationsStore } from '../stores/reservations'
  import type {
    Reservation,
    ReservationEditorValue,
    ReservationStatus,
    ReservationWrite,
  } from '../types/api'
  import { formatearFecha, limpiarFechaApi, fechaHoy } from '../utils/fechas'

  const props = defineProps<{
    focusReservationId: number | null
    initialDate: string | null
  }>()
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
        const clientName =
          clients.page.items.find((client) => client.id === item.clienteId)?.nombre ?? ''
        const matchesText =
          !text || `${clientName} ${item.clienteId} ${item.id}`.toLocaleLowerCase('es').includes(text)
        return matchesText && (statusFilter.value === 'all' || item.estado === statusFilter.value)
      })
      .sort((a, b) => a.fechaHoraInicio.localeCompare(b.fechaHoraInicio))
  })

  function formatDate(value: string) {
    return formatearFecha(value)
  }

  function clientName(id: number) {
    return clients.page.items.find((client) => client.id === id)?.nombre ?? `Cliente #${id}`
  }

  function resourceName(id: number) {
    return catalog.resources.find((resource) => resource.id === id)?.nombre ?? `Recurso #${id}`
  }

  function serviceName(id: number) {
    return catalog.serviceTypes.find((service) => service.id === id)?.nombre ?? `Servicio #${id}`
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

  watch(
    () => props.focusReservationId,
    (id) => {
      const reservation = id === null ? undefined : reservations.items.find((item) => item.id === id)
      if (reservation) openEdit(reservation)
    },
    { immediate: true },
  )

  watch(
    () => props.initialDate,
    (date) => {
      if (date) openCreate(date)
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
      formError.value = 'Selecciona un cliente y un servicio.'
      return
    }
    const draft: ReservationWrite = {
      clienteId: value.clienteId,
      tipoServicioId: value.tipoServicioId,
      fechaHoraInicio,
      estado: value.estado,
    }
    try {
      if (editing.value) await reservations.update(editing.value.id, draft)
      else {
        const created = await reservations.create(draft)
        successMessage.value = `Reserva #${created.id} · ${clientName(created.clienteId)} · ${resourceName(created.recursoReservableId)} / ${serviceName(created.tipoServicioId)} · Inicio: ${formatDate(created.fechaHoraInicio)} · Fin: ${formatDate(created.fechaHoraFin)} · ${statusLabel(created.estado)}`
      }
      modalOpen.value = false
    } catch (cause) {
      formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar la reserva.'
    }
  }

  async function cancel(reservation: Reservation) {
    if (!window.confirm(`¿Cancelar la reserva #${reservation.id}?`)) return
    try {
      await reservations.update(reservation.id, {
        clienteId: reservation.clienteId,
        tipoServicioId: reservation.tipoServicioId,
        fechaHoraInicio: reservation.fechaHoraInicio,
        estado: 2,
      })
    } catch {
      // The store exposes the API error in the page.
    }
  }

  async function remove(reservation: Reservation) {
    if (!auth.isAdmin || !window.confirm(`¿Eliminar permanentemente la reserva #${reservation.id}?`))
      return
    try {
      await reservations.remove(reservation.id)
    } catch {
      // The store exposes the API error in the page.
    }
  }
  </script>

  <template>
    <section class="list-toolbar">
      <label class="search-box"
        ><span>⌕</span
        ><input
          v-model="query"
          type="search"
          placeholder="Buscar por cliente o folio"
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
              <th>RECURSO / SERVICIO</th>
              <th>ESTADO</th>
              <th><span class="sr-only">Acciones</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredReservations" :key="item.id">
              <td>#{{ item.id }}</td>
              <td>
                <strong>{{ clientName(item.clienteId) }}</strong>
              </td>
              <td>{{ formatDate(item.fechaHoraInicio) }}</td>
              <td>{{ formatDate(item.fechaHoraFin) }}</td>
              <td>
                {{ resourceName(item.recursoReservableId)
                }}<small class="table-subline">{{ serviceName(item.tipoServicioId) }}</small>
              </td>
              <td>
                <span :class="['status-pill', statusClass(item.estado)]">{{
                  statusLabel(item.estado)
                }}</span>
              </td>
              <td>
                <div class="row-actions">
                  <button class="row-edit" @click="openEdit(item)">Editar</button
                  ><button
                    v-if="item.estado !== 2 && item.estado !== 3"
                    class="row-cancel"
                    @click="cancel(item)"
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
    />
  </template>
