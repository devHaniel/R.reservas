// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed } from 'vue'
import type { ReservableResource, Reservation, ReservationStatus } from '../types/api'
import { claveFecha, formatearHora, formatearFechaLarga, fechaHoy } from '../utils/fechas'
import { formatearMoneda } from '../utils/formato'

const props = defineProps<{
  reservations: Reservation[]
  resources: ReservableResource[]
  loading: boolean
  error: string
}>()
const emit = defineEmits<{ openReservations: [] }>()

const today = fechaHoy()
const activeReservations = computed(() =>
  props.reservations.filter((item) => item.estado === 0 || item.estado === 1),
)
const todayReservations = computed(() =>
  props.reservations
    .filter((item) => item.estado !== 2 && claveFecha(item.fechaHoraInicio) === today)
    .sort((a, b) => a.fechaHoraInicio.localeCompare(b.fechaHoraInicio)),
)
const pendingCount = computed(() => props.reservations.filter((item) => item.estado === 0).length)
const todayRevenue = computed(() =>
  todayReservations.value.reduce((total, item) => total + Number(item.precio || 0), 0),
)
const activeResources = computed(() => props.resources.filter((item) => item.activo))

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
      <div class="metric-foot">Pendientes y confirmadas</div>
    </article>
    <article class="metric-card">
      <div class="metric-top">
        <span>Pendientes</span><span class="metric-icon metric-icon-yellow">◷</span>
      </div>
      <div class="metric-value">{{ pendingCount }}</div>
      <div class="metric-foot">Requieren confirmación</div>
    </article>
    <article class="metric-card">
      <div class="metric-top">
        <span>Ingresos de hoy</span><span class="metric-icon metric-icon-green">$</span>
      </div>
      <div class="metric-value metric-value-money">{{ formatearMoneda(todayRevenue) }}</div>
      <div class="metric-foot">{{ todayReservations.length }} reservas agendadas</div>
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
          <h2>Agenda de hoy</h2>
        </div>
        <button class="text-action" @click="emit('openReservations')">
          Ver todas <span>↗</span>
        </button>
      </div>
      <div v-if="loading" class="empty-state"><strong>Cargando reservas…</strong></div>
      <div v-else-if="todayReservations.length" class="schedule-list">
        <div v-for="item in todayReservations" :key="item.id" class="schedule-row">
          <div class="schedule-time">{{ formatTime(item.fechaHoraInicio) }}</div>
          <div class="schedule-rail"><span></span></div>
          <div class="schedule-detail">
            <div class="schedule-name-line">
              <strong>{{ item.nombreCliente || `Cliente #${item.clienteId}` }}</strong
              ><span
                :class="[
                  'status-pill',
                  `status-${['pending', 'confirmed', 'cancelled', 'completed'][item.estado]}`,
                ]"
                >{{ statusLabel(item.estado) }}</span
              >
            </div>
            <p>
              {{ item.nombreRecurso || `Espacio #${item.recursoReservableId}` }} ·
              {{ item.nombreServicio || `Opción #${item.tipoServicioId}` }} ·
              {{ formatearMoneda(item.precio) }}
            </p>
          </div>
        </div>
      </div>
      <div v-else class="empty-state">
        <span class="empty-mark">▤</span><strong>Sin reservas para hoy</strong
        ><span>Crea una reserva o revisa el calendario.</span
        ><button class="text-action" @click="emit('openReservations')">
          Ir a reservas <span>↗</span>
        </button>
      </div>
    </article>
    <article class="panel week-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Catálogo</p>
          <h2>Espacios disponibles</h2>
        </div>
      </div>
      <div class="resource-count">
        {{ activeResources.length }}<span> espacios activos</span>
      </div>
      <div class="resource-list">
        <div
          v-for="resource in activeResources.slice(0, 5)"
          :key="resource.id"
          class="resource-line"
        >
          <span>{{ resource.nombre }}</span
          ><small
            >{{ resource.categoria }} · {{ resource.horarioAperturaDefault.slice(0, 5) }}–{{
              resource.horarioCierreDefault.slice(0, 5)
            }}</small
          >
        </div>
        <p v-if="!activeResources.length" class="form-hint">
          No hay espacios activos. Crea uno en Configuración → Espacios.
        </p>
      </div>
      <div class="week-legend">
        <span>Reservas de hoy</span><strong>{{ todayReservations.length }}</strong>
      </div>
    </article>
  </section>
</template>
