// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useCatalogStore } from '../stores/catalog'
import type { ReservableResource, ResourceWrite, ServiceType, ServiceTypeWrite } from '../types/api'

const props = defineProps<{ initialTab: 'resources' | 'services' }>()
const store = useCatalogStore()
const auth = useAuthStore()
const activeTab = ref<'resources' | 'services'>(props.initialTab)
watch(
  () => props.initialTab,
  (tab) => {
    activeTab.value = tab
  },
)
const modalOpen = ref(false)
const formError = ref('')
const editingResourceId = ref<number | null>(null)
const editingServiceId = ref<number | null>(null)
const resourceForm = ref<ResourceWrite>(emptyResource())
const serviceForm = ref<ServiceTypeWrite>(emptyService())
const serviceFilter = ref<number | 'all'>('all')
const filteredServices = computed(() =>
  store.serviceTypes.filter(
    (item) => serviceFilter.value === 'all' || item.recursoReservableId === serviceFilter.value,
  ),
)

function emptyResource(): ResourceWrite {
  return {
    nombre: '',
    tipoDeporte: '',
    activo: true,
    horarioAperturaDefault: '08:00',
    horarioCierreDefault: '22:00',
  }
}

function emptyService(): ServiceTypeWrite {
  return {
    recursoReservableId: store.resources[0]?.id ?? 0,
    nombre: '',
    duracionMinutos: 60,
    precio: 0,
  }
}

function openNewResource() {
  editingResourceId.value = null
  resourceForm.value = emptyResource()
  formError.value = ''
  modalOpen.value = true
}

function editResource(item: ReservableResource) {
  editingResourceId.value = item.id
  resourceForm.value = {
    nombre: item.nombre,
    tipoDeporte: item.tipoDeporte,
    activo: item.activo,
    horarioAperturaDefault: item.horarioAperturaDefault,
    horarioCierreDefault: item.horarioCierreDefault,
  }
  formError.value = ''
  modalOpen.value = true
}

function openNewService() {
  editingServiceId.value = null
  serviceForm.value = { ...emptyService(), recursoReservableId: store.resources[0]?.id ?? 0 }
  formError.value = ''
  modalOpen.value = true
}

function editService(item: ServiceType) {
  editingServiceId.value = item.id
  serviceForm.value = {
    recursoReservableId: item.recursoReservableId,
    nombre: item.nombre,
    duracionMinutos: item.duracionMinutos,
    precio: item.precio,
  }
  formError.value = ''
  modalOpen.value = true
}

async function saveResource() {
  try {
    const value = {
      ...resourceForm.value,
      horarioAperturaDefault: normalizeTime(resourceForm.value.horarioAperturaDefault),
      horarioCierreDefault: normalizeTime(resourceForm.value.horarioCierreDefault),
    }
    await store.saveResource(editingResourceId.value, value)
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el recurso.'
  }
}

async function saveService() {
  try {
    await store.saveServiceType(editingServiceId.value, serviceForm.value)
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el servicio.'
  }
}

function normalizeTime(value: string) {
  return value.slice(0, 5)
}

async function removeResource(item: ReservableResource) {
  if (!auth.isAdmin || !window.confirm(`¿Eliminar ${item.nombre}?`)) return
  try {
    await store.removeResource(item.id)
  } catch {
    /* The store exposes the API error. */
  }
}

async function removeService(item: ServiceType) {
  if (!auth.isAdmin || !window.confirm(`¿Eliminar ${item.nombre}?`)) return
  try {
    await store.removeServiceType(item.id)
  } catch {
    /* The store exposes the API error. */
  }
}

function resourceName(id: number) {
  return store.resources.find((resource) => resource.id === id)?.nombre ?? `Recurso #${id}`
}
</script>

<template>
  <div class="catalog-toolbar">
    <div class="segmented-control" role="tablist" aria-label="Catálogo">
      <button
        role="tab"
        :aria-selected="activeTab === 'resources'"
        :class="{ selected: activeTab === 'resources' }"
        @click="activeTab = 'resources'"
      >
        Recursos</button
      ><button
        role="tab"
        :aria-selected="activeTab === 'services'"
        :class="{ selected: activeTab === 'services' }"
        @click="activeTab = 'services'"
      >
        Tipos de servicio
      </button>
    </div>
    <button
      v-if="auth.isAdmin"
      class="button button-primary"
      @click="activeTab === 'resources' ? openNewResource() : openNewService()"
    >
      + {{ activeTab === 'resources' ? 'Nuevo recurso' : 'Nuevo tipo' }}
    </button>
    <button
      class="button button-quiet"
      :disabled="store.loading"
      @click="
        (activeTab === 'resources' ? store.loadResources() : store.loadServiceTypes()).catch(
          () => undefined,
        )
      "
    >
      Actualizar
    </button>
  </div>
  <p v-if="store.error" class="form-error" role="alert">{{ store.error }}</p>

  <section v-if="activeTab === 'resources'" class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Recursos reservables</h2>
        <span>{{ store.resources.length }} registros</span>
      </div>
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando recursos…</strong></div>
    <div v-else-if="store.resources.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>NOMBRE</th>
            <th>TIPO</th>
            <th>HORARIO</th>
            <th>ESTADO</th>
            <th><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in store.resources" :key="item.id">
            <td>
              <strong>{{ item.nombre }}</strong>
            </td>
            <td>{{ item.tipoDeporte }}</td>
            <td>
              {{ item.horarioAperturaDefault.slice(0, 5) }}–{{
                item.horarioCierreDefault.slice(0, 5)
              }}
            </td>
            <td>
              <span
                :class="['status-pill', item.activo ? 'status-confirmed' : 'status-cancelled']"
                >{{ item.activo ? 'Activo' : 'Inactivo' }}</span
              >
            </td>
            <td v-if="auth.isAdmin">
              <div class="row-actions">
                <button class="row-edit" @click="editResource(item)">Editar</button
                ><button class="row-cancel" @click="removeResource(item)">Eliminar</button>
              </div>
            </td>
            <td v-else>Solo lectura</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">⌂</span><strong>No hay recursos</strong
      ><span>Los recursos se configuran desde una cuenta Admin.</span>
    </div>
  </section>

  <section v-else class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Tipos de servicio</h2>
        <span>{{ filteredServices.length }} registros</span>
      </div>
      <label class="filter-group"
        ><span>Recurso</span
        ><select v-model="serviceFilter">
          <option value="all">Todos</option>
          <option v-for="item in store.resources" :key="item.id" :value="item.id">
            {{ item.nombre }}
          </option>
        </select></label
      >
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando servicios…</strong></div>
    <div v-else-if="filteredServices.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>NOMBRE</th>
            <th>RECURSO</th>
            <th>DURACIÓN</th>
            <th>PRECIO</th>
            <th><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in filteredServices" :key="item.id">
            <td>
              <strong>{{ item.nombre }}</strong>
            </td>
            <td>{{ resourceName(item.recursoReservableId) }}</td>
            <td>{{ item.duracionMinutos }} min</td>
            <td>
              {{
                new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(
                  item.precio,
                )
              }}
            </td>
            <td v-if="auth.isAdmin">
              <div class="row-actions">
                <button class="row-edit" @click="editService(item)">Editar</button
                ><button class="row-cancel" @click="removeService(item)">Eliminar</button>
              </div>
            </td>
            <td v-else>Solo lectura</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">◷</span><strong>No hay tipos de servicio</strong
      ><span>Los tipos de servicio se configuran desde una cuenta Admin.</span>
    </div>
  </section>

  <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
    <section class="reservation-modal" role="dialog" aria-modal="true">
      <div class="modal-heading">
        <div>
          <p class="eyebrow">Configuración Admin</p>
          <h2>
            {{
              activeTab === 'resources'
                ? editingResourceId
                  ? 'Editar recurso'
                  : 'Nuevo recurso'
                : editingServiceId
                  ? 'Editar tipo'
                  : 'Nuevo tipo de servicio'
            }}
          </h2>
        </div>
        <button class="modal-close" aria-label="Cerrar formulario" @click="modalOpen = false">
          ×
        </button>
      </div>
      <form
        v-if="activeTab === 'resources'"
        class="reservation-form"
        @submit.prevent="saveResource"
      >
        <label class="field"
          ><span>Nombre <b>*</b></span
          ><input v-model.trim="resourceForm.nombre" required maxlength="120" /></label
        ><label class="field"
          ><span>Tipo / deporte <b>*</b></span
          ><input v-model.trim="resourceForm.tipoDeporte" required maxlength="80"
        /></label>
        <div class="field-row">
          <label class="field"
            ><span>Apertura <b>*</b></span
            ><input v-model="resourceForm.horarioAperturaDefault" type="time" required /></label
          ><label class="field"
            ><span>Cierre <b>*</b></span
            ><input v-model="resourceForm.horarioCierreDefault" type="time" required
          /></label>
        </div>
        <label class="field checkbox-field"
          ><input v-model="resourceForm.activo" type="checkbox" /><span>Recurso activo</span></label
        >
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar</button
          ><button class="button button-primary" type="submit">Guardar recurso</button>
        </div>
      </form>
      <form v-else class="reservation-form" @submit.prevent="saveService">
        <label class="field"
          ><span>Recurso <b>*</b></span
          ><select v-model.number="serviceForm.recursoReservableId" required>
            <option disabled :value="0">Selecciona un recurso</option>
            <option v-for="item in store.resources" :key="item.id" :value="item.id">
              {{ item.nombre }}
            </option>
          </select></label
        ><label class="field"
          ><span>Nombre <b>*</b></span
          ><input v-model.trim="serviceForm.nombre" required maxlength="120"
        /></label>
        <div class="field-row">
          <label class="field"
            ><span>Duración (minutos) <b>*</b></span
            ><input
              v-model.number="serviceForm.duracionMinutos"
              type="number"
              min="1"
              max="1440"
              required /></label
          ><label class="field"
            ><span>Precio <b>*</b></span
            ><input v-model.number="serviceForm.precio" type="number" min="0" step="0.01" required
          /></label>
        </div>
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar</button
          ><button class="button button-primary" type="submit">Guardar tipo</button>
        </div>
      </form>
    </section>
  </div>
</template>
