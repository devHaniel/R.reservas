// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppSidebar from '../components/AppSidebar.vue'
import CalendarSection from '../components/CalendarSection.vue'
import CatalogSection from '../components/CatalogSection.vue'
import ClientsSection from '../components/ClientsSection.vue'
import DashboardSection from '../components/DashboardSection.vue'
import ReservationsPage from '../components/ReservationsPage.vue'
import UsersSection from '../components/UsersSection.vue'
import VendorsSection from '../components/VendorsSection.vue'
import { connectReservationNotifications } from '../services/notificationService'
import { useAuthStore } from '../stores/auth'
import { useCatalogStore } from '../stores/catalog'
import { useClientsStore } from '../stores/clients'
import { useReservationsStore } from '../stores/reservations'
import type { AppSection } from '../types/api'
import { formatearFechaLarga, fechaHoy } from '../utils/fechas'

const router = useRouter()
const auth = useAuthStore()
const reservations = useReservationsStore()
const clients = useClientsStore()
const catalog = useCatalogStore()
const activeSection = ref<AppSection>('overview')
const reservationFocusId = ref<number | null>(null)
const reservationInitialDate = ref<string | null>(null)
const liveConnection = ref<'connecting' | 'connected' | 'offline'>('connecting')
const todayKey = fechaHoy()
const todayLabel = formatearFechaLarga(todayKey, {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
})
let stopNotifications: (() => Promise<void>) | undefined

const sectionDetails: Record<AppSection, { eyebrow: string; title: string }> = {
  overview: { eyebrow: 'Operación', title: 'Panel de control.' },
  reservations: { eyebrow: 'Agenda', title: 'Reservas.' },
  calendar: { eyebrow: 'Agenda visual', title: 'Calendario.' },
  clients: { eyebrow: 'Directorio', title: 'Clientes.' },
  resources: { eyebrow: 'Configuración', title: 'Recursos.' },
  services: { eyebrow: 'Configuración', title: 'Tipos de servicio.' },
  users: { eyebrow: 'Administración', title: 'Vendedores.' },
}
const activeDetails = computed(() => sectionDetails[activeSection.value])

function navigate(section: AppSection) {
  if (section === 'users' && !auth.isAdmin) return
  reservationFocusId.value = null
  reservationInitialDate.value = null
  activeSection.value = section
}

function editCalendarReservation(id: number) {
  navigate('reservations')
  reservationFocusId.value = id
}

function createCalendarReservation(date: string) {
  navigate('reservations')
  reservationInitialDate.value = date
}

function handleSessionExpired() {
  void router.replace({ name: 'login' })
}

onMounted(async () => {
  window.addEventListener('reservas:auth-required', handleSessionExpired)
  await Promise.allSettled([
    reservations.load(),
    clients.load(),
    catalog.loadResources(),
    catalog.loadServiceTypes(),
  ])
  try {
    stopNotifications = await connectReservationNotifications(() => {
      void reservations.load().catch(() => undefined)
    })
    liveConnection.value = 'connected'
  } catch {
    liveConnection.value = 'offline'
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('reservas:auth-required', handleSessionExpired)
  if (stopNotifications) void stopNotifications()
})

function logout() {
  auth.logout()
  void router.replace({ name: 'login' })
}
</script>

<template>
  <div class="app-shell">
    <AppSidebar
      :active-section="activeSection"
      :reservation-count="reservations.items.length"
      :user-name="auth.user?.name ?? auth.user?.email ?? 'Usuario'"
      :roles="auth.user?.roles ?? []"
      @navigate="navigate"
    />
    <main class="main-area">
      <header class="topbar">
        <div class="date-stamp">
          <span :class="['live-dot', { 'live-dot-offline': liveConnection === 'offline' }]" />{{
            todayLabel
          }}
        </div>
        <div class="topbar-right">
          <span class="today-label">{{
            liveConnection === 'connected'
              ? 'ACTUALIZACIÓN EN VIVO'
              : liveConnection === 'connecting'
                ? 'CONECTANDO SIGNALR'
                : 'API HTTP'
          }}</span
          ><button class="button button-quiet logout-button" @click="logout">Cerrar sesión</button>
        </div>
      </header>
      <div class="content-wrap">
        <section class="page-heading">
          <div>
            <p class="eyebrow">{{ activeDetails.eyebrow }}</p>
            <h1>{{ activeDetails.title }}</h1>
          </div>
        </section>
        <DashboardSection
          v-if="activeSection === 'overview'"
          :reservations="reservations.items"
          :clients="clients.page.items"
          :resources="catalog.resources"
          :loading="reservations.loading"
          :error="reservations.error"
          @open-reservations="navigate('reservations')"
        />
        <ReservationsPage
          v-else-if="activeSection === 'reservations'"
          :focus-reservation-id="reservationFocusId"
          :initial-date="reservationInitialDate"
        />
        <CalendarSection
          v-else-if="activeSection === 'calendar'"
          :reservations="reservations.items"
          :clients="clients.page.items"
          :resources="catalog.resources"
          :service-types="catalog.serviceTypes"
          :today-key="todayKey"
          @open-reservation="editCalendarReservation"
          @new-reservation="createCalendarReservation"
        />
        <ClientsSection v-else-if="activeSection === 'clients'" />
        <CatalogSection
          v-else-if="activeSection === 'resources' || activeSection === 'services'"
          :initial-tab="activeSection"
        />
        <UsersSection v-else-if="activeSection === 'users' && auth.isAdmin" />
        <section v-else class="panel empty-state">
          <strong>Acceso no disponible</strong
          ><span>El backend valida el rol requerido para cada operación.</span>
        </section>
      </div>
    </main>
  </div>
</template>
