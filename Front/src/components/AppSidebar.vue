// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import type { ApiRole, AppSection } from '../types/api'

defineProps<{
  activeSection: AppSection
  reservationCount: number
  userName: string
  roles: ApiRole[]
}>()

const emit = defineEmits<{
  navigate: [section: AppSection]
}>()
</script>

<template>
  <aside class="sidebar">
    <a
      class="brand"
      href="#inicio"
      @click.prevent="emit('navigate', 'overview')"
      aria-label="Reservas, inicio"
    >
      <span class="brand-mark">r.</span>
      <span class="brand-name">reservas<span class="brand-period">.</span></span>
    </a>

    <div class="sidebar-caption">ESPACIO DE TRABAJO</div>
    <nav class="main-nav" aria-label="Navegación principal">
      <button
        :class="['nav-item', { active: activeSection === 'overview' }]"
        @click="emit('navigate', 'overview')"
      >
        <span class="nav-glyph">◫</span>Resumen
      </button>
      <button
        :class="['nav-item', { active: activeSection === 'reservations' }]"
        @click="emit('navigate', 'reservations')"
      >
        <span class="nav-glyph">▤</span>Reservas<span class="nav-count">{{
          reservationCount
        }}</span>
      </button>
      <button
        :class="['nav-item', { active: activeSection === 'calendar' }]"
        @click="emit('navigate', 'calendar')"
      >
        <span class="nav-glyph">▦</span>Calendario
      </button>
      <button
        :class="['nav-item', { active: activeSection === 'clients' }]"
        @click="emit('navigate', 'clients')"
      >
        <span class="nav-glyph">◎</span>Clientes
      </button>
      <button
        :class="['nav-item', { active: activeSection === 'resources' }]"
        @click="emit('navigate', 'resources')"
      >
        <span class="nav-glyph">⌂</span>Recursos
      </button>
      <button
        :class="['nav-item', { active: activeSection === 'services' }]"
        @click="emit('navigate', 'services')"
      >
        <span class="nav-glyph">◷</span>Servicios
      </button>
      <button
        v-if="roles.includes('Admin')"
        :class="['nav-item', { active: activeSection === 'users' }]"
        @click="emit('navigate', 'users')"
      >
        <span class="nav-glyph">♕</span>Usuarios
      </button>
      <button
        v-if="roles.includes('Admin')"
        :class="['nav-item', { active: activeSection === 'users' }]"
        @click="emit('navigate', 'users')"
      >
        <span class="nav-glyph">♙</span>Vendedores
      </button>
    </nav>

    <div class="sidebar-bottom">
      <div class="space-card">
        <div class="space-icon">R</div>
        <div><strong>Mi espacio</strong><span>Plan personal</span></div>
        <span class="space-menu">···</span>
      </div>
      <div class="profile-row">
        <span class="avatar avatar-owner">H</span>
        <span class="profile-copy"
          ><strong>{{ userName }}</strong
          ><span>{{ roles.includes('Admin') ? 'Administradora' : 'Vendedor' }}</span></span
        >
        <span class="profile-menu">⌄</span>
      </div>
    </div>
  </aside>
</template>
