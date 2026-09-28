<script setup lang="ts">
import { computed } from 'vue'
import type { Client, ReservableResource, Reservation, ReservationStatus } from '../types/api'
import { claveFecha, formatearHora, formatearFechaLarga, fechaHoy } from '../utils/fechas'

const props = defineProps<{
  reservations: Reservation[]
  clients: Client[]
  resources: ReservableResource[]
  loading: boolean
  error: string
}>()
const emit = defineEmits<{ openReservations: [] }>()

const today = fechaHoy()
const activeReservations = computed(() => props.reservations.filter((item) => item.estado !== 2))
const todayReservations = computed(() =>
  activeReservations.value
    .filter((item) => claveFecha(item.fechaHoraInicio) === today)
    .sort((a, b) => a.fechaHoraInicio.localeCompare(b.fechaHoraInicio)),
)
const pendingCount = computed(() => props.reservations.filter((item) => item.estado === 0).length)
const todayRevenueEstimate = computed(() => todayReservations.value.length)

function clientName(id: number) {
  return props.clients.find((client) => client.id === id)?.nombre ?? `Cliente #${id}`
}

function resourceName(id: number) {
  return props.resources.find((resource) => resource.id === id)?.nombre ?? `Recurso #${id}`
}

function statusLabel(status: ReservationStatus) {
  return ['Pendiente', 'Confirmada', 'Cancelada', 'Completada'][status] ?? 'Desconocida'
}

function formatTime(date: string) {
  return formatearHora(date)
}
</script>

<template>
  <p v-if="error" class="form-error" role="alert">{{ error }}</p>
  <section class="metrics-grid" aria-label="Resumen de reservas">
    <article class="metric-card metric-main">
      <div class="metric-top">
        <span>Reservas activas</span><span class="metric-icon metric-icon-coral">↗</span>
      </div>
      <div class="metric-value">{{ activeReservations.length }}</div>
      <div class="metric-foot">Datos recibidos de la API</div>
    </article>
    <article class="metric-card">
      <div class="metric-top">
        <span>Pendientes</span><span class="metric-icon metric-icon-yellow">◷</span>
      </div>
      <div class="metric-value">{{ pendingCount }}</div>
      <div class="metric-foot">Requieren seguimiento</div>
    </article>
    <article class="metric-card">
      <div class="metric-top">
        <span>Clientes visibles</span><span class="metric-icon metric-icon-green">◎</span>
      </div>
      <div class="metric-value">{{ clients.length }}</div>
      <div class="metric-foot">En la página actual</div>
    </article>
  </section>
  <section class="overview-grid">
    <article class="panel today-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">
            {{
              formatearFechaLarga(fechaHoy(), {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
              })
            }}
          </p>
          <h2>Reservas de hoy</h2>
        </div>
        <button class="text-action" @click="emit('openReservations')">
          Ver todas <span>↗</span>
        </button>
      </div>
      <div v-if="loading" class="empty-state"><strong>Cargando desde la API…</strong></div>
      <div v-else-if="todayReservations.length" class="schedule-list">
        <div v-for="item in todayReservations" :key="item.id" class="schedule-row">
          <div class="schedule-time">{{ formatTime(item.fechaHoraInicio) }}</div>
          <div class="schedule-rail"><span></span></div>
          <div class="schedule-detail">
            <div class="schedule-name-line">
              <strong>{{ clientName(item.clienteId) }}</strong
              ><span
                :class="[
                  'status-pill',
                  `status-${['pending', 'confirmed', 'cancelled', 'completed'][item.estado]}`,
                ]"
                >{{ statusLabel(item.estado) }}</span
              >
            </div>
            <p>{{ resourceName(item.recursoReservableId) }} · reserva #{{ item.id }}</p>
          </div>
        </div>
      </div>
      <div v-else class="empty-state">
        <span class="empty-mark">▤</span><strong>Sin reservas para hoy</strong
        ><span>Cuando la API devuelva reservas, aparecerán aquí.</span
        ><button class="text-action" @click="emit('openReservations')">
          Ir a reservas <span>↗</span>
        </button>
      </div>
    </article>
    <article class="panel week-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Catálogo disponible</p>
          <h2>Recursos activos</h2>
        </div>
      </div>
      <div class="resource-count">
        {{ resources.filter((item) => item.activo).length }}<span> recursos reservables</span>
      </div>
      <div class="resource-list">
        <div
          v-for="resource in resources.filter((item) => item.activo).slice(0, 5)"
          :key="resource.id"
          class="resource-line"
        >
          <span>{{ resource.nombre }}</span
          ><small
            >{{ resource.tipoDeporte }} · {{ resource.horarioAperturaDefault.slice(0, 5) }}–{{
              resource.horarioCierreDefault.slice(0, 5)
            }}</small
          >
        </div>
        <p v-if="!resources.some((item) => item.activo)" class="form-hint">
          No hay recursos activos disponibles.
        </p>
      </div>
      <div class="week-legend">
        <span>Reservas de hoy</span><strong>{{ todayRevenueEstimate }}</strong>
      </div>
    </article>
  </section>
</template>
