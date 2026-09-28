// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type {
  Client,
  ReservableResource,
  Reservation,
  ReservationStatus,
  ServiceType,
} from '../types/api'
import { reservationsService } from '../services/reservationsService'
import { claveFecha, formatearFechaLarga, formatearHora } from '../utils/fechas'

const props = defineProps<{
  reservations: Reservation[]
  clients: Client[]
  resources: ReservableResource[]
  serviceTypes: ServiceType[]
  todayKey: string
}>()

const emit = defineEmits<{
  openReservation: [id: number]
  newReservation: [date: string]
}>()

const selectedDate = ref(props.todayKey)
const displayedMonth = ref(new Date(`${props.todayKey}T12:00:00`))
const monthReservations = ref<Reservation[]>([])
const monthLoading = ref(false)
const monthError = ref('')
const weekdayLabels = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom']

function toDateKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function formatTime(value: string) {
  return formatearHora(value)
}

async function loadMonth() {
  const anio = displayedMonth.value.getFullYear()
  const mes = displayedMonth.value.getMonth() + 1
  monthLoading.value = true
  monthError.value = ''
  try {
    monthReservations.value = await reservationsService.byMonth(anio, mes)
  } catch (cause) {
    monthError.value =
      cause instanceof Error ? cause.message : 'No se pudo cargar la agenda del mes.'
  } finally {
    monthLoading.value = false
  }
}

watch(
  displayedMonth,
  () => {
    void loadMonth()
  },
  { immediate: true },
)

// Refresca el mes cuando llega una notificación en vivo (el store recarga la lista).
watch(
  () => props.reservations,
  () => {
    void loadMonth()
  },
)

function statusLabel(status: ReservationStatus) {
  return ['Pendiente', 'Confirmada', 'Cancelada', 'Completada'][status] ?? 'Desconocida'
}

function statusClass(status: ReservationStatus) {
  return (
    ['status-pending', 'status-confirmed', 'status-cancelled', 'status-completed'][status] ??
    'status-pending'
  )
}

const monthLabel = computed(() =>
  new Intl.DateTimeFormat('es-MX', { month: 'long', year: 'numeric' }).format(displayedMonth.value),
)
const reservationsByDate = computed(() => {
  const grouped = new Map<string, Reservation[]>()
  for (const reservation of monthReservations.value) {
    const key = claveFecha(reservation.fechaHoraInicio)
    const day = grouped.get(key) ?? []
    day.push(reservation)
    grouped.set(key, day)
  }
  return grouped
})
const calendarDays = computed(() => {
  const first = new Date(displayedMonth.value.getFullYear(), displayedMonth.value.getMonth(), 1)
  const start = new Date(first)
  start.setDate(first.getDate() - ((first.getDay() + 6) % 7))
  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start)
    date.setDate(start.getDate() + index)
    const key = toDateKey(date)
    return {
      key,
      day: date.getDate(),
      inMonth: date.getMonth() === displayedMonth.value.getMonth(),
      isToday: key === props.todayKey,
      count: reservationsByDate.value.get(key)?.length ?? 0,
    }
  })
})
const selectedDateLabel = computed(() => formatearFechaLarga(selectedDate.value))
const selectedReservations = computed(() =>
  [...(reservationsByDate.value.get(selectedDate.value) ?? [])].sort((a, b) =>
    a.fechaHoraInicio.localeCompare(b.fechaHoraInicio),
  ),
)
const activeReservationCount = computed(
  () => selectedReservations.value.filter((item) => item.estado !== 2).length,
)
const cancelledReservationCount = computed(
  () => selectedReservations.value.filter((item) => item.estado === 2).length,
)

function changeMonth(amount: number) {
  displayedMonth.value = new Date(
    displayedMonth.value.getFullYear(),
    displayedMonth.value.getMonth() + amount,
    1,
  )
}

function selectDay(key: string) {
  selectedDate.value = key
  const date = new Date(`${key}T12:00:00`)
  if (date.getMonth() !== displayedMonth.value.getMonth()) displayedMonth.value = date
}

function clientName(id: number) {
  return props.clients.find((client) => client.id === id)?.nombre ?? `Cliente #${id}`
}

function resourceName(id: number) {
  return props.resources.find((resource) => resource.id === id)?.nombre ?? `Recurso #${id}`
}

function serviceName(id: number) {
  return props.serviceTypes.find((service) => service.id === id)?.nombre ?? `Servicio #${id}`
}
</script>

<template>
  <section class="calendar-layout">
    <article class="panel calendar-panel">
      <div class="calendar-heading">
        <div>
          <p class="eyebrow">Agenda mensual</p>
          <h2>{{ monthLabel }}</h2>
        </div>
        <div class="month-controls">
          <button class="icon-button" aria-label="Mes anterior" @click="changeMonth(-1)">←</button>
          <button class="icon-button" aria-label="Mes siguiente" @click="changeMonth(1)">→</button>
          <button class="button button-quiet calendar-today" @click="selectDay(todayKey)">
            Hoy
          </button>
        </div>
      </div>
      <div class="calendar-grid">
        <div v-for="weekday in weekdayLabels" :key="weekday" class="weekday-label">
          {{ weekday }}
        </div>
        <button
          v-for="day in calendarDays"
          :key="day.key"
          :class="[
            'calendar-day',
            { outside: !day.inMonth, selected: selectedDate === day.key, 'is-today': day.isToday },
          ]"
          :aria-label="`${formatearFechaLarga(day.key)}: ${day.count} reservas`"
          :aria-pressed="selectedDate === day.key"
          @click="selectDay(day.key)"
        >
          <span>{{ day.day }}</span>
          <i v-if="day.count" class="day-count">{{ day.count }}</i>
        </button>
      </div>
      <div class="calendar-legend">
        <span><i class="legend-today"></i>Hoy</span>
        <span><i class="legend-event"></i>Día con reservas</span>
      </div>
    </article>

    <article class="panel selected-day-panel">
      <div class="panel-heading">
        <div>
          <p class="eyebrow">Agenda del día</p>
          <h2>{{ selectedDateLabel }}</h2>
        </div>
        <button
          class="button button-primary calendar-add"
          @click="emit('newReservation', selectedDate)"
        >
          + Nueva reserva
        </button>
      </div>
      <div class="day-summary">
        <strong>{{ selectedReservations.length }}</strong
        ><span
          >{{ activeReservationCount }} activas · {{ cancelledReservationCount }} canceladas</span
        >
      </div>
      <div v-if="selectedReservations.length" class="selected-day-list">
        <div v-for="item in selectedReservations" :key="item.id" class="day-reservation">
          <div class="day-res-time">{{ formatTime(item.fechaHoraInicio) }}</div>
          <div class="day-res-content">
            <strong>{{ clientName(item.clienteId) }}</strong>
            <span
              >{{ resourceName(item.recursoReservableId) }} ·
              {{ serviceName(item.tipoServicioId) }}</span
            >
            <span :class="['status-pill', statusClass(item.estado)]">{{
              statusLabel(item.estado)
            }}</span>
          </div>
          <button
            class="row-edit"
            :aria-label="`Abrir reserva ${item.id}`"
            @click="emit('openReservation', item.id)"
          >
            Abrir
          </button>
        </div>
      </div>
      <div v-else class="empty-state calendar-empty">
        <span class="empty-mark">▦</span><strong>Sin reservas para este día</strong>
        <span>Elige otra fecha o crea una reserva aquí.</span>
        <button class="text-action" @click="emit('newReservation', selectedDate)">
          Crear reserva <span>↗</span>
        </button>
      </div>
    </article>
  </section>
</template>
