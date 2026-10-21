// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useCatalogStore } from '../stores/catalog'
import type { ReservableResource, ResourceWrite, ServiceType, ServiceTypeWrite } from '../types/api'
import { formatearMoneda } from '../utils/formato'

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
const activeServices = computed(
  () =>
    store.serviceTypes.filter(
      (item) =>
        item.activo &&
        store.resources.some(
          (resource) => resource.id === item.recursoReservableId && resource.activo,
        ),
    ).length,
)
const activeResources = computed(() => store.resources.filter((item) => item.activo).length)

function emptyResource(): ResourceWrite {
  return {
    nombre: '',
    categoria: '',
    activo: true,
    horarioAperturaDefault: '08:00',
    horarioCierreDefault: '22:00',
  }
}

function emptyService(resourceId?: number): ServiceTypeWrite {
  return {
    recursoReservableId:
      resourceId ?? store.resources.find((item) => item.activo)?.id ?? store.resources[0]?.id ?? 0,
    nombre: '',
    duracionMinutos: 60,
    precio: 0,
    activo: true,
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
    categoria: item.categoria,
    activo: item.activo,
    horarioAperturaDefault: item.horarioAperturaDefault,
    horarioCierreDefault: item.horarioCierreDefault,
  }
  formError.value = ''
  modalOpen.value = true
}

function openNewService(resourceId?: number) {
  // El formulario que se muestra depende de la pestaña activa, no del botón
  // desde el que se abrió. Cambiamos a Opciones para editar una opción.
  activeTab.value = 'services'
  editingServiceId.value = null
  serviceForm.value = emptyService(resourceId)
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
    activo: item.activo,
  }
  formError.value = ''
  modalOpen.value = true
}

function openOptionsForResource(id: number) {
  serviceFilter.value = id
  activeTab.value = 'services'
}

async function saveResource() {
  try {
    const value = {
      ...resourceForm.value,
      nombre: resourceForm.value.nombre.trim(),
      categoria: resourceForm.value.categoria.trim(),
      horarioAperturaDefault: normalizeTime(resourceForm.value.horarioAperturaDefault),
      horarioCierreDefault: normalizeTime(resourceForm.value.horarioCierreDefault),
    }
    await store.saveResource(editingResourceId.value, value)
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el espacio.'
  }
}

async function saveService() {
  if (!serviceForm.value.recursoReservableId) {
    formError.value = 'Selecciona un espacio.'
    return
  }
  try {
    await store.saveServiceType(editingServiceId.value, {
      ...serviceForm.value,
      nombre: serviceForm.value.nombre.trim(),
    })
    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar la opción.'
  }
}

function normalizeTime(value: string) {
  return value.slice(0, 5)
}

async function removeResource(item: ReservableResource) {
  if (
    !auth.isAdmin ||
    !window.confirm(
      `¿Archivar "${item.nombre}"? También se archivarán sus opciones y dejará de poder reservarse. Las reservas históricas se conservan.`,
    )
  )
    return
  try {
    await store.removeResource(item.id)
    if (serviceFilter.value === item.id) serviceFilter.value = 'all'
  } catch {
    /* The store exposes the API error. */
  }
}

async function removeService(item: ServiceType) {
  if (
    !auth.isAdmin ||
    !window.confirm(
      `¿Archivar "${item.nombre}"? Dejará de ofrecerse, pero las reservas históricas se conservan.`,
    )
  )
    return
  try {
    await store.removeServiceType(item.id)
  } catch {
    /* The store exposes the API error. */
  }
}

function resourceName(id: number) {
  return store.resources.find((resource) => resource.id === id)?.nombre ?? `Espacio #${id}`
}

function optionsCount(id: number) {
  return store.serviceTypes.filter((item) => item.recursoReservableId === id).length
}
</script>

<template>
  <div class="catalog-toolbar">
    <div class="segmented-control" role="tablist" aria-label="Configuración de reservas">
      <button
        role="tab"
        :aria-selected="activeTab === 'resources'"
        :class="{ selected: activeTab === 'resources' }"
        @click="activeTab = 'resources'"
      >
        Espacios <span class="tab-count">{{ store.resources.length }}</span></button
      ><button
        role="tab"
        :aria-selected="activeTab === 'services'"
        :class="{ selected: activeTab === 'services' }"
        @click="activeTab = 'services'"
      >
        Opciones <span class="tab-count">{{ store.serviceTypes.length }}</span>
      </button>
    </div>
    <div class="catalog-actions">
      <button
        v-if="auth.isAdmin && activeTab === 'resources'"
        class="button button-primary"
        @click="openNewResource()"
      >
        + Nuevo espacio
      </button>
      <button
        v-if="auth.isAdmin && activeTab === 'services'"
        class="button button-primary"
        :disabled="!store.resources.length"
        @click="openNewService()"
      >
        + Nueva opción
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
  </div>

  <p class="catalog-hint">
    <template v-if="activeTab === 'resources'">
      Un <strong>espacio</strong> es lo que se reserva (una sala, cancha, consultorio…). Define su
      categoría y horario de atención. <strong>{{ activeResources }}</strong> activos.
    </template>
    <template v-else>
      Una <strong>opción</strong> es lo que el cliente elige al reservar: nombre, duración y precio.
      Cada opción pertenece a un espacio. <strong>{{ activeServices }}</strong> activas.
    </template>
  </p>

  <p v-if="store.error" class="form-error" role="alert">{{ store.error }}</p>

  <section v-if="activeTab === 'resources'" class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Espacios</h2>
        <span>{{ store.resources.length }} registros</span>
      </div>
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando espacios…</strong></div>
    <div v-else-if="store.resources.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>ESPACIO</th>
            <th>CATEGORÍA</th>
            <th>HORARIO</th>
            <th>OPCIONES</th>
            <th>ESTADO</th>
            <th><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in store.resources" :key="item.id">
            <td>
              <strong>{{ item.nombre }}</strong>
            </td>
            <td>{{ item.categoria }}</td>
            <td>
              {{ item.horarioAperturaDefault.slice(0, 5) }}–{{
                item.horarioCierreDefault.slice(0, 5)
              }}
            </td>
            <td>
              <button class="row-edit" @click="openOptionsForResource(item.id)">
                {{ optionsCount(item.id) }} {{ optionsCount(item.id) === 1 ? 'opción' : 'opciones' }}
              </button>
            </td>
            <td>
              <span
                :class="['status-pill', item.activo ? 'status-confirmed' : 'status-cancelled']"
                >{{ item.activo ? 'Activo' : 'Inactivo' }}</span
              >
            </td>
            <td v-if="auth.isAdmin">
              <div class="row-actions">
                <button
                  class="row-edit"
                  title="Crear una opción para este espacio"
                  @click="openNewService(item.id)"
                >
                  + Opción</button
                ><button class="row-edit" @click="editResource(item)">Editar</button
                ><button class="row-cancel" @click="removeResource(item)">Archivar</button>
              </div>
            </td>
            <td v-else>Solo lectura</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">⌂</span><strong>Aún no hay espacios</strong
      ><span>Crea el primero para poder definir opciones y recibir reservas.</span
      ><button v-if="auth.isAdmin" class="text-action" @click="openNewResource()">
        Crear espacio <span>↗</span>
      </button>
    </div>
  </section>

  <section v-else class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Opciones de reserva</h2>
        <span>{{ filteredServices.length }} registros</span>
      </div>
      <label class="filter-group"
        ><span>Espacio</span
        ><select v-model="serviceFilter">
          <option value="all">Todos</option>
          <option v-for="item in store.resources" :key="item.id" :value="item.id">
            {{ item.nombre }}
          </option>
        </select></label
      >
    </div>
    <div v-if="store.loading" class="empty-state"><strong>Cargando opciones…</strong></div>
    <div v-else-if="filteredServices.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>OPCIÓN</th>
            <th>ESPACIO</th>
            <th>DURACIÓN</th>
            <th>PRECIO</th>
            <th>ESTADO</th>
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
            <td>{{ formatearMoneda(item.precio) }}</td>
            <td>
              <span
                :class="['status-pill', item.activo ? 'status-confirmed' : 'status-cancelled']"
                >{{ item.activo ? 'Activa' : 'Inactiva' }}</span
              >
            </td>
            <td v-if="auth.isAdmin">
              <div class="row-actions">
                <button class="row-edit" @click="editService(item)">Editar</button
                ><button class="row-cancel" @click="removeService(item)">Archivar</button>
              </div>
            </td>
            <td v-else>Solo lectura</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="empty-state table-empty">
      <span class="empty-mark">◷</span
      ><strong>{{
        store.resources.length ? 'No hay opciones de reserva' : 'Primero crea un espacio'
      }}</strong
      ><span>{{
        store.resources.length
          ? 'Define duración y precio para que los clientes puedan reservar.'
          : 'Las opciones siempre pertenecen a un espacio.'
      }}</span
      ><button
        v-if="auth.isAdmin"
        class="text-action"
        @click="store.resources.length ? openNewService() : openNewResource()"
      >
        {{ store.resources.length ? 'Crear opción' : 'Crear espacio' }} <span>↗</span>
      </button>
    </div>
  </section>

  <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
    <section class="reservation-modal" role="dialog" aria-modal="true">
      <div class="modal-heading">
        <div>
          <p class="eyebrow">Configuración</p>
          <h2>
            {{
              activeTab === 'resources'
                ? editingResourceId
                  ? 'Editar espacio'
                  : 'Nuevo espacio'
                : editingServiceId
                  ? 'Editar opción'
                  : 'Nueva opción de reserva'
            }}
          </h2>
        </div>
        <button class="modal-close" aria-label="Cerrar formulario" @click="modalOpen = false">
          ×
        </button>
      </div>
      <form v-if="activeTab === 'resources'" class="reservation-form" @submit.prevent="saveResource">
        <label class="field"
          ><span>Nombre del espacio <b>*</b></span
          ><input
            v-model.trim="resourceForm.nombre"
            required
            maxlength="100"
            placeholder="Ej. Sala Norte, Cancha 1, Consultorio 2"
        /></label>
        <label class="field"
          ><span>Categoría <b>*</b></span
          ><input
            v-model.trim="resourceForm.categoria"
            required
            maxlength="60"
            placeholder="Ej. Sala, Deporte, Consulta"
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
          ><input v-model="resourceForm.activo" type="checkbox" /><span
            >Espacio disponible para reservar</span
          ></label
        >
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar</button
          ><button class="button button-primary" type="submit">Guardar espacio</button>
        </div>
      </form>
      <form v-else class="reservation-form" @submit.prevent="saveService">
        <label class="field"
          ><span>Espacio <b>*</b></span
          ><select v-model.number="serviceForm.recursoReservableId" required>
            <option disabled :value="0">Selecciona un espacio</option>
            <option v-for="item in store.resources" :key="item.id" :value="item.id">
              {{ item.nombre }} · {{ item.categoria }}{{ item.activo ? '' : ' (inactivo)' }}
            </option>
          </select></label
        ><label class="field"
          ><span>Nombre de la opción <b>*</b></span
          ><input
            v-model.trim="serviceForm.nombre"
            required
            maxlength="100"
            placeholder="Ej. Turno 1 hora, Sesión completa"
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
        <label class="field checkbox-field"
          ><input v-model="serviceForm.activo" type="checkbox" /><span
            >Ofrecer esta opción al reservar</span
          ></label
        >
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar</button
          ><button class="button button-primary" type="submit">Guardar opción</button>
        </div>
      </form>
    </section>
  </div>
</template>
