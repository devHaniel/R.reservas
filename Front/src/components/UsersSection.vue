// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, ref } from 'vue'

import { useAuthStore } from '../stores/auth'
import { useUsersStore } from '../stores/users'

import type { AuthUser } from '../types/api'

const store = useUsersStore()
const auth = useAuthStore()

const query = ref('')

const modalOpen = ref(false)
const editingId = ref<number | null>(null)

const formError = ref('')

const form = ref<{
  nombre: string
  email: string
  password: string
  rol: 'Admin' | 'Vendedor'
}>({
  nombre: '',
  email: '',
  password: '',
  rol: 'Vendedor',
})

const filteredUsers = computed(() =>
  store.users.filter((user) =>
    [
      user.name,
      user.email,
      ...user.roles,
    ].some((value) =>
      value.toLocaleLowerCase('es').includes(query.value.trim().toLocaleLowerCase('es')),
    ),
  ),
)

function openCreate() {
  editingId.value = null

  form.value = {
    nombre: '',
    email: '',
    password: '',
    rol: 'Vendedor',
  }

  formError.value = ''
  modalOpen.value = true
}

function openEdit(user: AuthUser) {
  editingId.value = Number(user.id)

  form.value = {
    nombre: user.name,
    email: user.email,
    password: '',
    rol: 'Vendedor',
  }

  formError.value = ''
  modalOpen.value = true
}

async function submit() {
  formError.value = ''

  try {
    if (editingId.value === null) {
      await store.create({
        nombre: form.value.nombre,
        email: form.value.email,
        password: form.value.password,
        rol: form.value.rol,
      })
    } else {
      await store.update(editingId.value, {
        nombre: form.value.nombre,
        email: form.value.email,
        rol: form.value.rol,
      })
    }

    modalOpen.value = false
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : 'No se pudo guardar el usuario.'
  }
}

async function remove(user: AuthUser) {
  if (!auth.isAdmin || !window.confirm(`¿Eliminar a ${user.name}?`)) return

  try {
    await store.remove(Number(user.id))
  } catch {
    // El store muestra el error de la API.
  }
}
</script>

<template>
  <section class="list-toolbar">
    <label class="search-box">
      <span>⌕</span>

      <input
        v-model="query"
        type="search"
        placeholder="Buscar usuarios"
        aria-label="Buscar usuarios"
      />
    </label>

    <button v-if="auth.isAdmin" class="button button-primary" @click="openCreate">
      + Nuevo usuario
    </button>
  </section>

  <p v-if="store.error" class="form-error" role="alert">
    {{ store.error }}
  </p>

  <section class="panel table-panel">
    <div class="table-heading">
      <div>
        <h2>Usuarios</h2>

        <span> {{ filteredUsers.length }} registros </span>
      </div>

      <div class="pager">
        <button
          class="button button-quiet"
          :disabled="store.loading"
          @click="store.load().catch(() => undefined)"
        >
          Actualizar
        </button>
      </div>
    </div>

    <div v-if="store.loading" class="empty-state">
      <strong>Cargando usuarios…</strong>
    </div>

    <div v-else-if="filteredUsers.length" class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>NOMBRE</th>
            <th>CORREO</th>
            <th>ROL</th>
            <th>
              <span class="sr-only">Acciones</span>
            </th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="user in filteredUsers" :key="user.id">
            <td>
              <strong>{{ user.name }}</strong>
            </td>

            <td>
              {{ user.email }}
            </td>

            <td>
              {{ user.roles }}
            </td>

            <td>
              <div class="row-actions">
                <button v-if="auth.isAdmin" class="row-edit" @click="openEdit(user)">Editar</button>

                <button v-if="auth.isAdmin" class="row-cancel" @click="remove(user)">
                  Eliminar
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else class="empty-state table-empty">
      <span class="empty-mark">◎</span>

      <strong>
        {{ query ? 'Sin coincidencias' : 'Aún no hay usuarios' }}
      </strong>

      <span>
        {{ query ? 'Prueba con otro término.' : 'Los usuarios registrados aparecerán aquí.' }}
      </span>
    </div>
  </section>

  <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
    <section
      class="reservation-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="user-form-title"
    >
      <div class="modal-heading">
        <div>
          <p class="eyebrow">Administración</p>

          <h2 id="user-form-title">
            {{ editingId === null ? 'Nuevo usuario' : 'Editar usuario' }}
          </h2>
        </div>

        <button class="modal-close" aria-label="Cerrar formulario" @click="modalOpen = false">
          ×
        </button>
      </div>

      <form class="reservation-form" @submit.prevent="submit">
        <label class="field">
          <span> Nombre <b>*</b> </span>

          <input v-model.trim="form.nombre" required maxlength="120" />
        </label>

        <label class="field">
          <span> Email <b>*</b> </span>

          <input v-model.trim="form.email" type="email" required maxlength="160" />
        </label>

        <label v-if="editingId === null" class="field">
          <span> Contraseña <b>*</b> </span>

          <input v-model="form.password" type="password" required minlength="6" maxlength="100" />
        </label>

        <label class="field">
          <span> Rol <b>*</b> </span>

          <select v-model="form.rol" required>
            <option value="Vendedor">Vendedor</option>

            <option value="Admin">Admin</option>
          </select>
        </label>

        <p v-if="formError" class="form-error" role="alert">
          {{ formError }}
        </p>

        <div class="modal-actions">
          <button type="button" class="button button-quiet" @click="modalOpen = false">
            Descartar
          </button>

          <button class="button button-primary" type="submit" :disabled="store.loading">
            {{ editingId === null ? 'Crear usuario' : 'Guardar cambios' }}
          </button>
        </div>
      </form>
    </section>
  </div>
</template>
