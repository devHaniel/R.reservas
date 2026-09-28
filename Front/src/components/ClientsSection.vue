<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useClientsStore } from '../stores/clients'
import type { Client, ClientWrite } from '../types/api'

const store = useClientsStore()
const auth = useAuthStore()
const query = ref('')
const pageSize = ref(store.page.cantidad)
const exactEmailResult = ref<Client | null | undefined>(undefined)
const modalOpen = ref(false)
const editingId = ref<number | null>(null)
const formError = ref('')
const form = ref<ClientWrite>({ nombre: '', telefono: '', email: '' })
const filteredClients = computed(() =>
  store.page.items.filter((client) =>
    [client.nombre, client.email, client.telefono].some((value) =>
      value.toLocaleLowerCase('es').includes(query.value.trim().toLocaleLowerCase('es')),
    ),
  ),
)

function changePageSize() {
  void store.load(1, pageSize.value).catch(() => undefined)
}

function openCreate() {
  editingId.value = null
  form.value = { nombre: '', telefono: '', email: '' }
  formError.value = ''
  modalOpen.value = true
}

function openEdit(client: Client) {
  editingId.value = client.id
  form.value = { nombre: client.nombre, telefono: client.telefono, email: client.email }
  formError.value = ''
  modalOpen.value = true
}

async function submit() {
  formError.value = ''
  try {
    if (editingId.value === null) await store.create(form.value)
    else await store.update(editingId.value, form.value)
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el cliente.'
  }
}

async function searchEmail() {
  if (!query.value.trim()) return
  try {
    exactEmailResult.value = await store.searchByEmail(query.value.trim())
  } catch {
    exactEmailResult.value = undefined
  }
}

async function remove(client: Client) {
  if (!auth.isAdmin || !window.confirm(`¿Eliminar a ${client.nombre}?`)) return
  try {
    await store.remove(client.id)
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
        placeholder="Buscar en esta página"
        aria-label="Buscar clientes"
        @input="exactEmailResult = undefined" /></label
    ><button class="button button-quiet" @click="searchEmail">Buscar email</button
    ><button class="button button-primary" @click="openCreate">+ Nuevo cliente</button>
  </section>
  <p v-if="store.error" class="form-error" role="alert">{{ store.error }}</p>
  <p v-if="exactEmailResult !== undefined" class="search-result" role="status">
    {{
      exactEmailResult
        ? `Coincidencia: ${exactEmailResult.nombre} (${exactEmailResult.email})`
        : 'No se encontró un cliente con ese email.'
    }}
  </p>
  <section class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Clientes</h2>
        <span
          >{{ store.page.total }} registros · página {{ store.page.pagina }} de
          {{ store.page.totalPaginas || 1 }}</span
        >
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
          @click="store.load().catch(() => undefined)"
        >
          Actualizar
        </button>
        <button
          class="icon-button"
          aria-label="Página anterior"
          :disabled="store.page.pagina <= 1 || store.loading"
          @click="store.load(store.page.pagina - 1).catch(() => undefined)"
        >
          ←</button
        ><button
          class="icon-button"
          aria-label="Página siguiente"
          :disabled="store.page.pagina >= store.page.totalPaginas || store.loading"
          @click="store.load(store.page.pagina + 1).catch(() => undefined)"
        >
          →
        </button>
      </div>
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando clientes…</strong></div>
    <div v-else-if="filteredClients.length" class="table-scroll">
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
          <tr v-for="client in filteredClients" :key="client.id">
            <td>
              <strong>{{ client.nombre }}</strong>
            </td>
            <td>{{ client.email }}</td>
            <td>{{ client.telefono }}</td>
            <td>
              <div class="row-actions">
                <button class="row-edit" @click="openEdit(client)">Editar</button
                ><button v-if="auth.isAdmin" class="row-cancel" @click="remove(client)">
                  Eliminar
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
          ? 'Prueba con otro término.'
          : 'Los clientes creados aquí quedan asociados al vendedor autenticado.'
      }}</span>
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
          ><span>Email <b>*</b></span
          ><input v-model.trim="form.email" type="email" required maxlength="160" /></label
        ><label class="field"
          ><span>Teléfono <b>*</b></span
          ><input v-model.trim="form.telefono" type="tel" required maxlength="40"
        /></label>
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
