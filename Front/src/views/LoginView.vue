<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const email = ref('')
const password = ref('')

async function submit() {
  try {
    await auth.login({ email: email.value.trim(), password: password.value })
    await router.replace({ name: 'home' })
  } catch {
    // The store exposes the API message for the form.
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-brand">
      <a class="brand" href="/"
        ><span class="brand-mark">r.</span
        ><span class="brand-name">reservas<span class="brand-period">.</span></span></a
      >
      <div class="login-message">
        <p class="eyebrow">Panel de gestión</p>
        <h1>Tu operación,<br />en buenas manos.</h1>
        <p>Clientes, recursos y reservas conectados en un solo lugar.</p>
      </div>
      <span class="login-note">RESERVAS · ADMINISTRACIÓN</span>
    </section>
    <section class="login-form-side">
      <form class="login-form" @submit.prevent="submit">
        <div>
          <p class="eyebrow">Bienvenido de vuelta</p>
          <h2>Inicia sesión</h2>
          <p class="login-subtitle">Ingresa tus credenciales para continuar.</p>
        </div>
        <label class="field"
          ><span>Correo electrónico</span
          ><input
            v-model.trim="email"
            type="email"
            autocomplete="username"
            required
            placeholder="admin@reservas.local" /></label
        ><label class="field"
          ><span>Contraseña</span
          ><input v-model="password" type="password" autocomplete="current-password" required
        /></label>
        <p v-if="auth.error" class="form-error" role="alert">{{ auth.error }}</p>
        <button class="button button-primary login-submit" type="submit" :disabled="auth.loading">
          {{ auth.loading ? 'Conectando…' : 'Entrar' }}
        </button>
        <p class="login-help">
          La sesión se renueva automáticamente mientras el refresh token siga vigente.
        </p>
      </form>
    </section>
  </main>
</template>
