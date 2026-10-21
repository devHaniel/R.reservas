// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useClientsStore } from '../stores/clients'
import type { Client } from '../types/api'

interface ClientForm {
  nombre: string
  telefono: string
  email: string
}

const store = useClientsStore()
const auth = useAuthStore()
const query = ref('')
const pageSize = ref(store.page.cantidad)
const modalOpen = ref(false)
const editingId = ref<number | null>(null)
const formError = ref('')
const form = ref<ClientForm>({ nombre: '', telefono: '', email: '' })
let searchTimer: ReturnType<typeof setTimeout> | undefined

const currentPageLabel = computed(() => {
  const total = Math.max(1, store.page.totalPaginas)
  return `página ${store.page.pagina} de ${total}`
})

function loadPage(pagina = store.page.pagina) {
  return store.load(pagina, pageSize.value, query.value).catch(() => undefined)
}

function changePageSize() {
  void loadPage(1)
}

watch(query, () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void loadPage(1), 300)
})

onMounted(() => {
  void loadPage(1)
})

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer)
})

function openCreate() {
  editingId.value = null
  form.value = { nombre: '', telefono: '', email: '' }
  formError.value = ''
  modalOpen.value = true
}

function openEdit(client: Client) {
  editingId.value = client.id
  form.value = {
    nombre: client.nombre,
    telefono: client.telefono,
    email: client.email ?? '',
  }
  formError.value = ''
  modalOpen.value = true
}

async function submit() {
  formError.value = ''
  const value = {
    nombre: form.value.nombre.trim(),
    telefono: form.value.telefono.trim(),
    email: form.value.email.trim() || null,
  }
  try {
    if (editingId.value === null) await store.create(value)
    else await store.update(editingId.value, value)
    modalOpen.value = false
    void loadPage(editingId.value === null ? 1 : store.page.pagina)
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el cliente.'
  }
}

async function remove(client: Client) {
  if (
    !auth.isAdmin ||
    !window.confirm(
      `¿Archivar a "${client.nombre}"? Dejará de aparecer en el directorio, pero sus reservas se conservan.`,
    )
  )
    return
  try {
    await store.remove(client.id)
    const pagina =
      store.page.items.length === 1 && store.page.pagina > 1
        ? store.page.pagina - 1
        : store.page.pagina
    void loadPage(pagina)
  } catch {
    /* The store displays the API error. */
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
        placeholder="Buscar en todo el directorio: nombre, correo o teléfono"
        aria-label="Buscar clientes"
    /></label>
    <button class="button button-primary" @click="openCreate">+ Nuevo cliente</button>
  </section>
  <p v-if="store.error" class="form-error" role="alert">{{ store.error }}</p>
  <section class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Clientes</h2>
        <span>{{ store.page.total }} registros · {{ currentPageLabel }}</span>
      </div>
      <div class="pager">
        <div class="filter-group page-size-control">
          <label for="client-page-size">Por página</label>
          <select id="client-page-size" v-model.number="pageSize" @change="changePageSize">
            <option :value="10">10</option>
            <option :value="20">20</option>
            <option :value="30">30</option>
            <option :value="40">40</option>
          </select>
        </div>
        <button
          class="button button-quiet"
          :disabled="store.loading"
          @click="loadPage()"
        >
          Actualizar
        </button>
        <button
          class="icon-button"
          aria-label="Página anterior"
          :disabled="store.page.pagina <= 1 || store.loading"
          @click="loadPage(store.page.pagina - 1)"
        >
          ←</button
        ><button
          class="icon-button"
          aria-label="Página siguiente"
          :disabled="store.page.pagina >= store.page.totalPaginas || store.loading"
          @click="loadPage(store.page.pagina + 1)"
        >
          →
        </button>
      </div>
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando clientes…</strong></div>
    <div v-else-if="store.page.items.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>NOMBRE</th>
            <th>CORREO</th>
            <th>TELÉFONO</th>
            <th><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="client in store.page.items" :key="client.id">
            <td>
              <strong>{{ client.nombre }}</strong>
            </td>
            <td>{{ client.email || '—' }}</td>
            <td>{{ client.telefono }}</td>
            <td>
              <div class="row-actions">
                <button class="row-edit" @click="openEdit(client)">Editar</button
                ><button v-if="auth.isAdmin" class="row-cancel" @click="remove(client)">
                  Archivar
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">◎</span
      ><strong>{{ query ? 'Sin coincidencias' : 'Aún no hay clientes' }}</strong
      ><span>{{
        query
          ? 'Prueba con otro nombre, correo o teléfono.'
          : 'Registra el primer cliente para poder agendar reservas.'
      }}</span
      ><button v-if="!query" class="text-action" @click="openCreate">
        Crear cliente <span>↗</span>
      </button>
    </div>
  </section>

  <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
    <section
      class="reservation-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="client-form-title"
    >
      <div class="modal-heading">
        <div>
          <p class="eyebrow">Directorio</p>
          <h2 id="client-form-title">{{ editingId ? 'Editar cliente' : 'Nuevo cliente' }}</h2>
        </div>
        <button class="modal-close" aria-label="Cerrar formulario" @click="modalOpen = false">
          ×
        </button>
      </div>
      <form class="reservation-form" @submit.prevent="submit">
        <label class="field"
          ><span>Nombre <b>*</b></span
          ><input v-model.trim="form.nombre" required maxlength="120" /></label
        ><label class="field"
          ><span>Teléfono <b>*</b></span
          ><input v-model.trim="form.telefono" type="tel" required maxlength="30" /></label
        ><label class="field"
          ><span>Email <small>opcional</small></span
          ><input v-model.trim="form.email" type="email" maxlength="256" /></label
        >
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar</button
          ><button class="button button-primary" type="submit" :disabled="store.loading">
            Guardar cliente
          </button>
        </div>
      </form>
    </section>
  </div>
</template>
