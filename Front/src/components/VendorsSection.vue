// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import type { RegisterVendorInput } from '../types/api'

const auth = useAuthStore()
const loading = ref(false)
const error = ref('')
const result = ref('')
const form = ref<RegisterVendorInput>({ nombre: '', email: '', password: '' })

async function submit() {
  error.value = ''
  result.value = ''
  loading.value = true
  try {
    const created = await auth.registerVendor(form.value)
    result.value = `${created.mensaje} (ID ${created.id})`
    form.value = { nombre: '', email: '', password: '' }
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'No se pudo registrar el vendedor.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="panel vendor-panel">
    <div>
      <p class="eyebrow">Solo Admin</p>
      <h2>Registrar vendedor</h2>
      <p class="form-hint">
        La cuenta se crea con el rol Vendedor. El usuario no puede elegir ni modificar su propio
        rol.
      </p>
    </div>
    <form class="vendor-form" @submit.prevent="submit">
      <label class="field"
        ><span>Nombre completo <b>*</b></span
        ><input v-model.trim="form.nombre" required maxlength="160" autocomplete="name" /></label
      ><label class="field"
        ><span>Correo electrónico <b>*</b></span
        ><input v-model.trim="form.email" type="email" required autocomplete="email" /></label
      ><label class="field"
        ><span>Contraseña temporal <b>*</b></span
        ><input
          v-model="form.password"
          type="password"
          required
          minlength="8"
          autocomplete="new-password"
        /><small class="form-hint"
          >Mínimo 8 caracteres, con mayúscula, minúscula y número.</small
        ></label
      >
      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <p v-if="result" class="form-success" role="status">{{ result }}</p>
      <button class="button button-primary" type="submit" :disabled="loading">
        {{ loading ? 'Registrando…' : 'Registrar vendedor' }}
      </button>
    </form>
  </section>
</template>
