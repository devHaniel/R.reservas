// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { publicService } from '../services/publicService'
import { useAuthStore } from '../stores/auth'
import type {
  PublicAvailability,
  PublicBookingOption,
  PublicBookingSpace,
  PublicReservation,
} from '../types/public'
import { formatearFecha, formatearFechaLarga, fechaHoy } from '../utils/fechas'
import { formatearMoneda } from '../utils/formato'

const auth = useAuthStore()

const spaces = ref<PublicBookingSpace[]>([])
const catalogLoading = ref(true)
const catalogError = ref('')
const selectedSpaceId = ref<number | null>(null)
const selectedOptionId = ref<number | null>(null)
const selectedDate = ref(fechaHoy())
const availability = ref<PublicAvailability | null>(null)
const availabilityLoading = ref(false)
const availabilityError = ref('')
const selectedTime = ref('')
const form = ref({ nombre: '', telefono: '', email: '' })
const verification = ref({
  codeSent: false,
  sending: false,
  code: '',
  verifying: false,
  token: '',
  tokenExpiresAt: 0,
  mensaje: '',
  error: '',
  devCode: '',
  resendIn: 0,
})
const submission = ref({ creating: false, error: '' })
const confirmed = ref<PublicReservation | null>(null)
const codeInput = ref<HTMLInputElement | null>(null)

let availabilityRequestId = 0
let resendTimer: ReturnType<typeof setInterval> | undefined

const minDate = fechaHoy()
const maxDate = (() => {
  const date = new Date()
  date.setDate(date.getDate() + 89)
  return fechaHoy(date)
})()

const selectedSpace = computed(
  () => spaces.value.find((space) => space.id === selectedSpaceId.value) ?? null,
)
const selectedOption = computed(
  () => selectedSpace.value?.opciones.find((option) => option.id === selectedOptionId.value) ?? null,
)
const selectedSlot = computed(
  () =>
    availability.value?.horarios.find((slot) => slot.horaInicio === selectedTime.value) ?? null,
)
const selectedDateLabel = computed(() =>
  selectedDate.value
    ? formatearFechaLarga(selectedDate.value, {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
      })
    : 'Elige una fecha',
)
const formComplete = computed(
  () =>
    Boolean(form.value.nombre.trim()) &&
    Boolean(form.value.telefono.trim()) &&
    Boolean(form.value.email.trim()),
)
const currentStep = computed(() => {
  if (!selectedSlot.value) return 1
  if (!formComplete.value) return 2
  return 3
})
const submitHint = computed(() => {
  if (!selectedSlot.value) return 'Paso 1: elige un horario disponible para continuar.'
  if (!formComplete.value) {
    return 'Paso 2: completa tu nombre, teléfono y correo electrónico.'
  }
  if (!verification.value.token) {
    return 'Paso 3: verifica tu correo para habilitar la confirmación.'
  }
  return ''
})
const spaceCount = computed(() => spaces.value.length)
const optionCount = computed(() =>
  spaces.value.reduce((total, space) => total + space.opciones.length, 0),
)
const categoryCount = computed(
  () => new Set(spaces.value.map((space) => space.categoria.toLowerCase())).size,
)
const lowestPrice = computed<number | null>(() => {
  if (spaces.value.length === 0) return null
  return Math.min(...spaces.value.map((space) => space.precioDesde))
})

function mensajeError(cause: unknown, fallback: string) {
  return cause instanceof Error ? cause.message : fallback
}

function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function categoryIcon(categoria: string) {
  const value = categoria.toLocaleLowerCase('es')
  if (value.includes('cancha') || value.includes('fútbol') || value.includes('futbol')) return '⚽'
  if (value.includes('deporte') || value.includes('pádel') || value.includes('padel')) return '🎾'
  if (value.includes('salón') || value.includes('salon') || value.includes('evento')) return '✦'
  if (value.includes('consulta') || value.includes('clínica') || value.includes('clinica')) return '✚'
  return '◫'
}

function duracion(minutos: number) {
  if (minutos >= 60 && minutos % 60 === 0) return `${minutos / 60} h`
  return `${minutos} min`
}

async function loadCatalog() {
  catalogLoading.value = true
  catalogError.value = ''
  try {
    spaces.value = await publicService.catalog()
    const firstSpace = spaces.value[0]
    if (firstSpace) selectSpace(firstSpace)
  } catch (cause) {
    catalogError.value = mensajeError(
      cause,
      'No pudimos cargar los espacios disponibles en este momento.',
    )
  } finally {
    catalogLoading.value = false
  }
}

function selectSpace(space: PublicBookingSpace, moveToBooking = false) {
  selectedSpaceId.value = space.id
  const firstOption = space.opciones[0]
  selectedOptionId.value = firstOption?.id ?? null
  selectedTime.value = ''
  confirmed.value = null
  if (moveToBooking) scrollToSection('reservar')
}

function selectOption(option: PublicBookingOption) {
  if (selectedOptionId.value === option.id) return
  confirmed.value = null
  selectedOptionId.value = option.id
}

async function loadAvailability() {
  const optionId = selectedOptionId.value
  const date = selectedDate.value
  if (!optionId || !date) {
    availability.value = null
    return
  }

  const requestId = ++availabilityRequestId
  availabilityLoading.value = true
  availabilityError.value = ''
  try {
    const result = await publicService.availability(optionId, date)
    if (requestId !== availabilityRequestId) return
    availability.value = result
  } catch (cause) {
    if (requestId !== availabilityRequestId) return
    availability.value = null
    availabilityError.value = mensajeError(cause, 'No pudimos consultar los horarios disponibles.')
  } finally {
    if (requestId === availabilityRequestId) availabilityLoading.value = false
  }
}

watch([selectedOptionId, selectedDate], () => {
  selectedTime.value = ''
  confirmed.value = null
  void loadAvailability()
})

watch(
  () => form.value.email,
  () => {
    if (verification.value.codeSent || verification.value.token) resetVerification()
  },
)

function resetVerification() {
  verification.value = {
    codeSent: false,
    sending: false,
    code: '',
    verifying: false,
    token: '',
    tokenExpiresAt: 0,
    mensaje: '',
    error: '',
    devCode: '',
    resendIn: 0,
  }
  stopResendTimer()
}

function startResendTimer() {
  stopResendTimer()
  if (verification.value.resendIn <= 0) return
  resendTimer = setInterval(() => {
    if (verification.value.resendIn > 0) verification.value.resendIn -= 1
    if (verification.value.resendIn <= 0) stopResendTimer()
  }, 1000)
}

function stopResendTimer() {
  if (resendTimer) {
    clearInterval(resendTimer)
    resendTimer = undefined
  }
}

function emailValido(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
}

async function requestCode() {
  verification.value.error = ''
  const email = form.value.email.trim()
  if (!emailValido(email)) {
    verification.value.error = 'Escribe un correo electrónico válido antes de enviar el código.'
    return
  }

  verification.value.sending = true
  try {
    const result = await publicService.requestVerification(email)
    verification.value.codeSent = true
    verification.value.code = ''
    verification.value.token = ''
    verification.value.tokenExpiresAt = 0
    verification.value.mensaje = result.mensaje
    verification.value.devCode = result.codigoDesarrollo ?? ''
    verification.value.resendIn = result.reenviarEnSegundos
    startResendTimer()
    await nextTick()
    codeInput.value?.focus()
  } catch (cause) {
    verification.value.error = mensajeError(cause, 'No pudimos enviar el código de verificación.')
  } finally {
    verification.value.sending = false
  }
}

function normalizarCodigo() {
  verification.value.code = verification.value.code.replace(/\D/g, '').slice(0, 6)
}

async function confirmCode() {
  verification.value.error = ''
  const codigo = verification.value.code.replace(/\D/g, '')
  if (codigo.length !== 6) {
    verification.value.error = 'El código debe tener exactamente 6 dígitos.'
    return
  }

  verification.value.verifying = true
  try {
    const result = await publicService.confirmVerification(form.value.email.trim(), codigo)
    verification.value.token = result.token
    verification.value.tokenExpiresAt = Date.now() + result.expiraEnSegundos * 1000
    verification.value.mensaje = result.mensaje
    verification.value.devCode = ''
    stopResendTimer()
  } catch (cause) {
    verification.value.error = mensajeError(cause, 'No pudimos verificar el código.')
  } finally {
    verification.value.verifying = false
  }
}

async function confirmReservation() {
  submission.value.error = ''
  if (!selectedOption.value || !selectedSlot.value) {
    submission.value.error = 'Elige un espacio, una fecha y un horario disponible.'
    return
  }
  if (!verification.value.token) {
    submission.value.error = 'Verifica tu correo electrónico antes de confirmar la reserva.'
    return
  }
  if (verification.value.tokenExpiresAt > 0 && Date.now() >= verification.value.tokenExpiresAt) {
    resetVerification()
    submission.value.error = 'Tu verificación expiró. Envía un código nuevo para continuar.'
    return
  }
  if (!form.value.nombre.trim() || !form.value.telefono.trim() || !form.value.email.trim()) {
    submission.value.error = 'Completa tu nombre, teléfono y correo electrónico.'
    return
  }

  submission.value.creating = true
  try {
    const result = await publicService.createReservation({
      nombre: form.value.nombre.trim(),
      telefono: form.value.telefono.trim(),
      email: form.value.email.trim(),
      tipoServicioId: selectedOption.value.id,
      fechaHoraInicio: `${selectedDate.value}T${selectedTime.value}`,
      tokenVerificacion: verification.value.token,
    })
    confirmed.value = result
    resetVerification()
    selectedTime.value = ''
    void loadAvailability()
    await nextTick()
    document.getElementById('reserva-confirmada')?.scrollIntoView({
      behavior: 'smooth',
      block: 'center',
    })
  } catch (cause) {
    const message = mensajeError(
      cause,
      'No pudimos confirmar la reserva. Revisa tus datos e inténtalo de nuevo.',
    )
    submission.value.error = message
    if (verification.value.token && /verificaci[oó]n/i.test(message) && /expir/i.test(message)) {
      resetVerification()
    }
  } finally {
    submission.value.creating = false
  }
}

function nuevaReserva() {
  confirmed.value = null
  submission.value.error = ''
  selectedTime.value = ''
  resetVerification()
  void loadAvailability()
  scrollToSection('reservar')
}

onMounted(() => {
  void loadCatalog()
})

onBeforeUnmount(() => {
  stopResendTimer()
})
</script>

<template>
  <main class="landing">
    <header class="landing-nav">
      <div class="nav-inner">
        <a class="landing-brand" href="/" aria-label="Reservas, inicio" @click.prevent="scrollToTop">
          <span class="landing-brand-mark">r.</span>
          <span class="landing-brand-name">reservas<span>.</span></span>
        </a>

        <nav class="nav-links" aria-label="Secciones de la landing">
          <a href="#espacios" @click.prevent="scrollToSection('espacios')">Espacios</a>
          <a href="#reservar" @click.prevent="scrollToSection('reservar')">Reservar</a>
          <a href="#como-funciona" @click.prevent="scrollToSection('como-funciona')">Cómo funciona</a>
        </nav>

        <div class="nav-actions">
          <RouterLink v-if="auth.isAuthenticated" class="nav-panel" :to="{ name: 'home' }">
            Mi panel
          </RouterLink>
          <RouterLink v-else class="nav-panel" :to="{ name: 'login' }">Acceso al panel</RouterLink>
          <button class="cta cta-small" type="button" @click="scrollToSection('reservar')">
            Reservar ahora
          </button>
        </div>
      </div>
    </header>

    <section class="hero">
      <div class="hero-copy">
        <p class="hero-eyebrow">
          <span class="eyebrow-dot" /> Canchas y espacios · reserva en línea
        </p>
        <h1>Tu cancha,<br /><em>tu hora.</em><br />Sin llamadas.</h1>
        <p class="hero-lead">
          Mira la disponibilidad real, elige tu horario y confirma en minutos. Verificamos tu
          correo para que cada reserva sea de una persona real.
        </p>
        <div class="hero-actions">
          <button class="cta cta-large" type="button" @click="scrollToSection('reservar')">
            Reservar una cancha
            <span>→</span>
          </button>
          <button class="cta cta-quiet" type="button" @click="scrollToSection('espacios')">
            Ver espacios
          </button>
        </div>
        <div class="hero-trust">
          <span><b>✓</b> Disponibilidad en vivo</span>
          <span><b>✓</b> Verificación por correo</span>
          <span><b>✓</b> Sin crear contraseña</span>
        </div>
      </div>

      <div class="hero-visual" aria-hidden="true">
        <div class="scene-glow" />
        <div class="court-card">
          <div class="court-top">
            <span>CANCHA 01</span>
            <span>90 MIN</span>
          </div>
          <div class="court">
            <span class="court-half" />
            <span class="court-circle" />
            <span class="court-goal court-goal-left" />
            <span class="court-goal court-goal-right" />
            <span class="court-ball" />
          </div>
          <div class="court-bottom">
            <span>HOY · 19:00</span>
            <span class="court-status">DISPONIBLE</span>
          </div>
        </div>
        <div class="float-card float-confirm">
          <span class="float-icon">✓</span>
          <span><strong>Correo verificado</strong><small>Reserva protegida</small></span>
        </div>
        <div class="float-card float-slot">
          <span class="pulse-dot" />
          <span><strong>19:00 – 20:30</strong><small>Cancha techada · Norte</small></span>
        </div>
      </div>
    </section>

    <section class="hero-stats" aria-label="Resumen del catálogo">
      <div>
        <strong>{{ catalogLoading ? '—' : spaceCount }}</strong>
        <span>espacios disponibles</span>
      </div>
      <div>
        <strong>{{ catalogLoading ? '—' : optionCount }}</strong>
        <span>opciones de reserva</span>
      </div>
      <div>
        <strong>{{ catalogLoading ? '—' : categoryCount }}</strong>
        <span>categorías para elegir</span>
      </div>
      <div class="stats-price">
        <small>Desde</small>
        <strong>{{ lowestPrice !== null ? formatearMoneda(lowestPrice) : '—' }}</strong>
      </div>
    </section>

    <section id="espacios" class="section spaces-section">
      <div class="section-heading">
        <p class="section-eyebrow">Catálogo en vivo</p>
        <h2>Elige dónde jugar.</h2>
        <p>
          Cada espacio muestra sus horarios y opciones reales. Si aparece aquí, se puede reservar
          en línea.
        </p>
      </div>

      <div v-if="catalogLoading" class="catalog-grid" aria-label="Cargando espacios">
        <div v-for="index in 3" :key="index" class="catalog-skeleton">
          <span />
          <span />
          <span />
          <span />
        </div>
      </div>

      <div v-else-if="catalogError" class="catalog-message" role="alert">
        <strong>No pudimos abrir el catálogo.</strong>
        <p>{{ catalogError }}</p>
        <button class="cta cta-quiet" type="button" @click="loadCatalog">Reintentar</button>
      </div>

      <div v-else-if="spaces.length === 0" class="catalog-message">
        <strong>Todavía no hay espacios publicados.</strong>
        <p>El equipo puede agregarlos desde el panel de gestión.</p>
        <RouterLink class="cta cta-quiet" :to="{ name: auth.isAuthenticated ? 'home' : 'login' }">
          Ir al panel
        </RouterLink>
      </div>

      <div v-else class="catalog-grid">
        <article v-for="space in spaces" :key="space.id" class="catalog-card">
          <div class="catalog-card-top">
            <span class="catalog-category">{{ space.categoria }}</span>
            <span class="catalog-icon">{{ categoryIcon(space.categoria) }}</span>
          </div>
          <h3>{{ space.nombre }}</h3>
          <p class="catalog-hours">
            Abierto de {{ space.horarioApertura }} a {{ space.horarioCierre }}
          </p>
          <div class="catalog-tags">
            <span v-for="option in space.opciones.slice(0, 3)" :key="option.id">
              {{ option.nombre }}
            </span>
          </div>
          <div class="catalog-card-footer">
            <span><small>Desde</small><strong>{{ formatearMoneda(space.precioDesde) }}</strong></span>
            <button type="button" class="catalog-link" @click="selectSpace(space, true)">
              Reservar <span>→</span>
            </button>
          </div>
        </article>
      </div>
    </section>

    <section id="reservar" class="section booking-section">
      <div class="section-heading centered">
        <p class="section-eyebrow">Reserva en línea</p>
        <h2>Elige, verifica y listo.</h2>
        <p>
          La disponibilidad se consulta al instante. Tu correo verifica que la solicitud viene de
          una persona real.
        </p>
      </div>

      <div class="booking-shell">
        <div class="booking-main">
          <div class="steps" aria-label="Pasos para reservar">
            <div :class="['step', { done: currentStep > 1, active: currentStep === 1 }]">
              <span class="step-number">{{ currentStep > 1 ? '✓' : '1' }}</span>
              <span>
                <strong>Espacio y hora</strong>
                <small>{{
                  selectedSlot
                    ? `${selectedTime} – ${selectedSlot.horaFin}`
                    : 'Elige fecha y horario'
                }}</small>
              </span>
            </div>
            <div :class="['step', { done: currentStep > 2, active: currentStep === 2 }]">
              <span class="step-number">{{ currentStep > 2 ? '✓' : '2' }}</span>
              <span>
                <strong>Tus datos</strong>
                <small>{{ formComplete ? 'Datos completos' : 'Nombre y contacto' }}</small>
              </span>
            </div>
            <div :class="['step', { done: Boolean(verification.token), active: currentStep === 3 }]">
              <span class="step-number">{{ verification.token ? '✓' : '3' }}</span>
              <span>
                <strong>Verifica tu correo</strong>
                <small>{{ verification.token ? 'Correo verificado' : 'Confirma la reserva' }}</small>
              </span>
            </div>
          </div>

          <div class="booking-block">
            <div class="block-head">
              <span class="block-number">01</span>
              <div>
                <h3>Espacio y horario</h3>
                <p>Selecciona el espacio, la opción y el turno que más te acomode.</p>
              </div>
            </div>

            <div v-if="spaces.length" class="space-picker">
              <button
                v-for="space in spaces"
                :key="space.id"
                type="button"
                :class="['space-option', { selected: selectedSpaceId === space.id }]"
                @click="selectSpace(space)"
              >
                <span class="space-option-icon">{{ categoryIcon(space.categoria) }}</span>
                <span>
                  <strong>{{ space.nombre }}</strong>
                  <small>{{ space.categoria }}</small>
                </span>
                <b>{{ formatearMoneda(space.precioDesde) }}</b>
              </button>
            </div>

            <div v-if="selectedSpace" class="option-picker">
              <p class="picker-label">Opción de reserva</p>
              <button
                v-for="option in selectedSpace.opciones"
                :key="option.id"
                type="button"
                :class="['option-option', { selected: selectedOptionId === option.id }]"
                @click="selectOption(option)"
              >
                <span>
                  <strong>{{ option.nombre }}</strong>
                  <small>{{ duracion(option.duracionMinutos) }}</small>
                </span>
                <b>{{ formatearMoneda(option.precio) }}</b>
              </button>
            </div>

            <div class="date-row">
              <label class="landing-field date-field">
                <span>Fecha</span>
                <input v-model="selectedDate" type="date" :min="minDate" :max="maxDate" />
              </label>
              <div class="date-copy">
                <small>Estás eligiendo</small>
                <strong>{{ selectedDateLabel }}</strong>
              </div>
            </div>

            <div class="slots-heading">
              <strong>Horarios disponibles</strong>
              <small v-if="selectedOption">{{ duracion(selectedOption.duracionMinutos) }} por turno</small>
            </div>

            <div v-if="availabilityLoading" class="slots-message">
              <span class="loading-dot" /> Consultando disponibilidad…
            </div>
            <div v-else-if="availabilityError" class="inline-error" role="alert">
              {{ availabilityError }}
            </div>
            <div v-else-if="availability && availability.horarios.length" class="slot-grid">
              <button
                v-for="slot in availability.horarios"
                :key="slot.horaInicio"
                type="button"
                :class="['slot', { selected: selectedTime === slot.horaInicio }]"
                @click="selectedTime = slot.horaInicio"
              >
                <strong>{{ slot.horaInicio }}</strong>
                <small>a {{ slot.horaFin }}</small>
              </button>
            </div>
            <div v-else class="slots-message empty">
              No quedan horarios para este día. Prueba con otra fecha u otra opción.
            </div>
          </div>

          <div class="booking-block">
            <div class="block-head">
              <span class="block-number">02</span>
              <div>
                <h3>Tus datos</h3>
                <p>Los usaremos para identificar tu reserva y avisarte si algo cambia.</p>
              </div>
            </div>

            <form class="contact-form" @submit.prevent="confirmReservation">
              <div class="form-grid">
                <label class="landing-field">
                  <span>Nombre completo</span>
                  <input
                    v-model.trim="form.nombre"
                    type="text"
                    name="nombre"
                    autocomplete="name"
                    maxlength="100"
                    required
                    placeholder="Tu nombre"
                  />
                </label>
                <label class="landing-field">
                  <span>Teléfono / WhatsApp</span>
                  <input
                    v-model.trim="form.telefono"
                    type="tel"
                    name="telefono"
                    autocomplete="tel"
                    maxlength="30"
                    required
                    placeholder="55 0000 0000"
                  />
                </label>
              </div>

              <label class="landing-field">
                <span>Correo electrónico</span>
                <input
                  v-model.trim="form.email"
                  type="email"
                  name="email"
                  autocomplete="email"
                  maxlength="256"
                  required
                  placeholder="tu@correo.com"
                />
              </label>

              <div :class="['verification-card', { verified: verification.token }]">
                <div class="verification-head">
                  <div>
                    <span class="verification-kicker">Paso 3</span>
                    <strong>Verifica tu correo</strong>
                    <p>Te enviamos un código de 6 dígitos para confirmar que eres una persona real.</p>
                  </div>
                  <span :class="['verification-state', { ok: verification.token }]">
                    {{ verification.token ? 'Verificado' : 'Pendiente' }}
                  </span>
                </div>

                <div v-if="!verification.token" class="verification-actions">
                  <button
                    type="button"
                    class="cta cta-outline"
                    :disabled="verification.sending || verification.resendIn > 0"
                    @click="requestCode"
                  >
                    {{
                      verification.sending
                        ? 'Enviando…'
                        : verification.resendIn > 0
                          ? `Reenviar en ${verification.resendIn}s`
                          : verification.codeSent
                            ? 'Reenviar código'
                            : 'Enviar código'
                    }}
                  </button>
                  <div v-if="verification.codeSent" class="code-entry">
                    <input
                      ref="codeInput"
                      v-model="verification.code"
                      type="text"
                      inputmode="numeric"
                      maxlength="6"
                      autocomplete="one-time-code"
                      placeholder="000000"
                      aria-label="Código de verificación"
                      @input="normalizarCodigo"
                    />
                    <button
                      type="button"
                      class="cta cta-small"
                      :disabled="verification.verifying"
                      @click="confirmCode"
                    >
                      {{ verification.verifying ? 'Verificando…' : 'Verificar' }}
                    </button>
                  </div>
                </div>

                <p v-if="verification.mensaje" class="verification-message" aria-live="polite">
                  {{ verification.mensaje }}
                </p>
                <p v-if="verification.devCode" class="dev-code">
                  Modo desarrollo · tu código es <strong>{{ verification.devCode }}</strong>
                </p>
                <p v-if="verification.error" class="inline-error" role="alert">
                  {{ verification.error }}
                </p>
              </div>

              <p v-if="submission.error" class="inline-error" role="alert">
                {{ submission.error }}
              </p>

              <p v-if="submitHint" class="submit-hint">
                <span>→</span>
                {{ submitHint }}
              </p>

              <div class="submit-row">
                <span class="submit-total">
                  <small>Total estimado</small>
                  <strong>{{
                    selectedOption ? formatearMoneda(selectedOption.precio) : '—'
                  }}</strong>
                </span>
                <button
                  class="cta cta-large"
                  type="submit"
                  :disabled="
                    submission.creating || !verification.token || !selectedSlot || catalogLoading
                  "
                >
                  {{ submission.creating ? 'Confirmando…' : 'Confirmar reserva' }}
                </button>
              </div>
              <p class="privacy-note">
                Usamos tu correo solo para verificar la reserva. La solicitud queda pendiente hasta
                que el equipo la confirme.
              </p>
            </form>
          </div>
        </div>

        <aside class="booking-summary">
          <div
            v-if="confirmed"
            id="reserva-confirmada"
            class="confirmation-card"
            aria-live="polite"
          >
            <span class="confirmation-check">✓</span>
            <p class="section-eyebrow">Solicitud recibida</p>
            <h3>¡Nos vemos en la cancha!</h3>
            <p class="confirmation-copy">{{ confirmed.mensaje }}</p>
            <div class="confirmation-folio">
              <small>Folio</small>
              <strong>{{ confirmed.folio }}</strong>
            </div>
            <dl class="summary-list">
              <div>
                <dt>Espacio</dt>
                <dd>{{ confirmed.nombreRecurso }}</dd>
              </div>
              <div>
                <dt>Opción</dt>
                <dd>{{ confirmed.nombreServicio }}</dd>
              </div>
              <div>
                <dt>Fecha y hora</dt>
                <dd>{{ formatearFecha(confirmed.fechaHoraInicio) }}</dd>
              </div>
              <div>
                <dt>Total</dt>
                <dd>{{ formatearMoneda(confirmed.precio) }}</dd>
              </div>
            </dl>
            <button class="cta cta-outline full-width" type="button" @click="nuevaReserva">
              Hacer otra reserva
            </button>
          </div>

          <div v-else class="summary-card">
            <p class="section-eyebrow">Tu reserva</p>
            <h3>{{ selectedSpace?.nombre ?? 'Elige un espacio' }}</h3>
            <p class="summary-option">
              {{ selectedOption?.nombre ?? 'Selecciona una opción para ver el precio.' }}
            </p>
            <dl class="summary-list">
              <div>
                <dt>Fecha</dt>
                <dd>{{ selectedDateLabel }}</dd>
              </div>
              <div>
                <dt>Horario</dt>
                <dd>
                  {{
                    selectedSlot
                      ? `${selectedSlot.horaInicio} – ${selectedSlot.horaFin}`
                      : 'Sin horario'
                  }}
                </dd>
              </div>
              <div>
                <dt>Duración</dt>
                <dd>{{ selectedOption ? duracion(selectedOption.duracionMinutos) : '—' }}</dd>
              </div>
              <div class="summary-total">
                <dt>Total</dt>
                <dd>{{ selectedOption ? formatearMoneda(selectedOption.precio) : '—' }}</dd>
              </div>
            </dl>
            <div class="summary-note">
              <span>✓</span>
              <p>El horario se aparta al confirmar y el equipo valida la reserva.</p>
            </div>
          </div>
        </aside>
      </div>
    </section>

    <section id="como-funciona" class="section how-section">
      <div class="section-heading centered">
        <p class="section-eyebrow">Así de simple</p>
        <h2>Reservar no debería ser un trámite.</h2>
      </div>
      <div class="how-grid">
        <article>
          <span class="how-number">01</span>
          <h3>Elige tu turno</h3>
          <p>Consulta los espacios y horarios reales. Sin llamadas ni mensajes para preguntar.</p>
        </article>
        <article>
          <span class="how-number">02</span>
          <h3>Verifica tu correo</h3>
          <p>
            Enviamos un código de 6 dígitos. Así confirmamos que la reserva viene de una persona
            real.
          </p>
        </article>
        <article>
          <span class="how-number">03</span>
          <h3>Llega y juega</h3>
          <p>Tu solicitud aparece en la agenda del negocio y el equipo la confirma.</p>
        </article>
      </div>
    </section>

    <section class="closing-banner">
      <div>
        <p class="section-eyebrow">Canchas, salones, consultorios y más</p>
        <h2>Tu próximo turno está a un clic.</h2>
      </div>
      <button class="cta cta-large cta-light" type="button" @click="scrollToSection('reservar')">
        Reservar ahora <span>→</span>
      </button>
    </section>

    <footer class="landing-footer">
      <div class="footer-brand">
        <span class="landing-brand-mark">r.</span>
        <span class="landing-brand-name">reservas<span>.</span></span>
      </div>
      <p>Disponibilidad en vivo y verificación por correo para reservar con confianza.</p>
      <div class="footer-links">
        <a href="#espacios" @click.prevent="scrollToSection('espacios')">Espacios</a>
        <a href="#reservar" @click.prevent="scrollToSection('reservar')">Reservar</a>
        <RouterLink :to="{ name: auth.isAuthenticated ? 'home' : 'login' }">
          {{ auth.isAuthenticated ? 'Mi panel' : 'Acceso al panel' }}
        </RouterLink>
      </div>
    </footer>
  </main>
</template>

<style scoped>
.landing {
  --ink: #172b4d;
  --muted: #66758a;
  --forest: #285ea8;
  --forest-deep: #1d477e;
  --grass: #28734f;
  --lime: #eaf1fa;
  --coral: #285ea8;
  --coral-dark: #204c88;
  --coral-pale: #eaf1fa;
  --cream: #f5f7fa;
  --paper: #fff;
  --line: #e2e8f0;
  --soft: #f8fafc;
  --success: #28734f;
  --success-pale: #eaf4ee;
  --warning: #876a32;
  --warning-pale: #f6f2e8;
  min-height: 100vh;
  overflow-x: hidden;
  color: var(--ink);
  background-color: var(--cream);
  background-image: none;
}
.landing ::selection {
  color: #fff;
  background: var(--coral);
}
.landing a:focus-visible,
.landing button:focus-visible,
.landing input:focus-visible {
  outline: 3px solid rgba(40, 94, 168, 0.3);
  outline-offset: 2px;
}
.landing a {
  color: inherit;
  text-decoration: none;
}
.landing button,
.landing input {
  font: inherit;
}
.landing button {
  cursor: pointer;
}
.landing button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}
.landing-brand {
  display: inline-flex;
  align-items: center;
  gap: 9px;
}
.landing-brand-mark {
  display: grid;
  width: 31px;
  height: 31px;
  place-items: center;
  border-radius: 9px 9px 9px 3px;
  color: #fff;
  background: var(--coral);
  font-family: Manrope, sans-serif;
  font-size: 22px;
  font-weight: 800;
  line-height: 1;
}
.landing-brand-name {
  font-family: Manrope, sans-serif;
  font-size: 20px;
  font-weight: 800;
  letter-spacing: -0.6px;
}
.landing-brand-name span {
  color: var(--coral);
}
.landing-nav {
  position: sticky;
  z-index: 20;
  top: 0;
  border-bottom: 1px solid rgba(23, 43, 77, 0.08);
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(14px);
}
.nav-inner {
  display: flex;
  width: min(1180px, calc(100% - 48px));
  min-height: 72px;
  align-items: center;
  gap: 28px;
  margin: 0 auto;
}
.nav-links {
  display: flex;
  align-items: center;
  gap: 26px;
  margin-left: 18px;
  color: #5a6b81;
  font-size: 12px;
  font-weight: 600;
}
.nav-links a {
  position: relative;
  padding: 6px 0;
}
.nav-links a::after {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  height: 1px;
  background: var(--forest);
  content: '';
  opacity: 0;
  transform: scaleX(0.4);
  transition:
    opacity 0.18s,
    transform 0.18s;
}
.nav-links a:hover::after {
  opacity: 1;
  transform: scaleX(1);
}
.nav-actions {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-left: auto;
}
.nav-panel {
  color: #344b68;
  font-size: 11px;
  font-weight: 700;
}
.nav-panel:hover {
  color: var(--forest);
}
.cta {
  display: inline-flex;
  min-height: 42px;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 0 18px;
  border: 1px solid transparent;
  border-radius: 6px;
  color: #fff;
  background: var(--coral);
  box-shadow: 0 3px 9px rgba(40, 94, 168, 0.16);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.1px;
  transition:
    transform 0.16s,
    background 0.16s,
    border-color 0.16s;
}
.cta:hover {
  background: var(--coral-dark);
  transform: translateY(-1px);
}
.cta-small {
  min-height: 36px;
  padding: 0 15px;
  border-radius: 5px;
  font-size: 11px;
}
.cta-large {
  min-height: 50px;
  padding: 0 24px;
  border-radius: 7px;
  font-size: 13px;
}
.cta-quiet {
  border-color: #cbd5e1;
  color: #344b68;
  background: #fff;
  box-shadow: none;
}
.cta-quiet:hover {
  border-color: #9bb2d0;
  color: #1e4d87;
  background: #f7faff;
}
.cta-outline {
  border-color: #cbd5e1;
  color: var(--forest);
  background: #fff;
  box-shadow: none;
}
.cta-outline:hover {
  border-color: var(--forest);
  background: #f7faff;
}
.cta-light {
  color: var(--coral);
  background: #fff;
  box-shadow: none;
}
.cta-light:hover {
  color: var(--coral-dark);
  background: #f7faff;
}
.full-width {
  width: 100%;
}
.hero {
  display: grid;
  width: min(1180px, calc(100% - 48px));
  grid-template-columns: minmax(0, 1.04fr) minmax(380px, 0.96fr);
  align-items: center;
  gap: clamp(38px, 6vw, 90px);
  margin: 0 auto;
  padding: clamp(58px, 8vw, 110px) 0 clamp(42px, 6vw, 80px);
}
.hero-eyebrow {
  display: flex;
  align-items: center;
  gap: 9px;
  margin: 0 0 22px;
  color: #5a6b81;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 1.15px;
  text-transform: uppercase;
}
.eyebrow-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--coral);
  box-shadow: 0 0 0 4px var(--coral-pale);
}
.hero h1 {
  margin: 0;
  color: var(--ink);
  font-family: Manrope, sans-serif;
  font-size: clamp(45px, 6.2vw, 78px);
  font-weight: 700;
  letter-spacing: -3.7px;
  line-height: 0.98;
}
.hero h1 em {
  color: var(--coral);
  font-style: normal;
}
.hero-lead {
  max-width: 520px;
  margin: 26px 0 0;
  color: #66758a;
  font-size: 15px;
  line-height: 1.7;
}
.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 30px;
}
.hero-trust {
  display: flex;
  flex-wrap: wrap;
  gap: 18px;
  margin-top: 30px;
  color: #718096;
  font-size: 10px;
  font-weight: 600;
}
.hero-trust b {
  margin-right: 4px;
  color: var(--grass);
}
.hero-visual {
  position: relative;
  display: grid;
  min-height: 430px;
  place-items: center;
}
.scene-glow {
  position: absolute;
  width: 96%;
  height: 88%;
  border-radius: 50%;
  background:
    radial-gradient(circle at 55% 45%, rgba(40, 94, 168, 0.12), transparent 54%),
    radial-gradient(circle at 20% 80%, rgba(23, 43, 77, 0.12), transparent 55%);
  filter: blur(8px);
}
.court-card {
  position: relative;
  z-index: 2;
  width: min(100%, 405px);
  padding: 16px;
  border: 1px solid rgba(255, 255, 255, 0.45);
  border-radius: 22px;
  color: #f4f7fb;
  background: linear-gradient(155deg, #203958, #172b4d 70%);
  box-shadow:
    0 30px 60px rgba(23, 43, 77, 0.22),
    inset 0 1px 0 rgba(255, 255, 255, 0.15);
  transform: rotate(-3.2deg);
  animation: court-in 0.8s cubic-bezier(0.2, 0.8, 0.2, 1) both;
}
.court-top,
.court-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 3px 5px;
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 1.4px;
}
.court-bottom {
  padding-top: 12px;
}
.court-status {
  padding: 4px 7px;
  border-radius: 3px;
  color: var(--forest);
  background: var(--coral-pale);
  font-size: 7px;
}
.court {
  position: relative;
  overflow: hidden;
  aspect-ratio: 1.62;
  margin: 12px 0 3px;
  border: 2px solid rgba(244, 247, 251, 0.84);
  border-radius: 5px;
  background:
    repeating-linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.035) 0,
      rgba(255, 255, 255, 0.035) 14px,
      transparent 14px,
      transparent 28px
    ),
    linear-gradient(150deg, #3972bc, #285ea8);
  box-shadow: inset 0 0 40px rgba(14, 36, 65, 0.22);
}
.court-half {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 50%;
  width: 2px;
  background: rgba(244, 247, 251, 0.72);
}
.court-circle {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 25%;
  aspect-ratio: 1;
  border: 2px solid rgba(244, 247, 251, 0.72);
  border-radius: 50%;
  transform: translate(-50%, -50%);
}
.court-goal {
  position: absolute;
  top: 50%;
  width: 9%;
  height: 35%;
  border: 2px solid rgba(244, 247, 251, 0.72);
  transform: translateY(-50%);
}
.court-goal-left {
  left: -2px;
  border-left: 0;
}
.court-goal-right {
  right: -2px;
  border-right: 0;
}
.court-ball {
  position: absolute;
  z-index: 1;
  top: 31%;
  left: 63%;
  width: 25px;
  height: 25px;
  border: 2px solid #cfe0f2;
  border-radius: 50%;
  background:
    radial-gradient(circle at 35% 30%, #fff 0 12%, transparent 13%),
    radial-gradient(circle at 63% 66%, #172b4d 0 16%, transparent 17%),
    var(--lime);
  box-shadow: 4px 12px 20px rgba(14, 36, 65, 0.35);
  animation: ball-float 4.5s ease-in-out infinite;
}
.float-card {
  position: absolute;
  z-index: 3;
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 11px 14px;
  border: 1px solid rgba(255, 255, 255, 0.8);
  border-radius: 11px;
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 16px 32px rgba(23, 43, 77, 0.14);
  backdrop-filter: blur(8px);
}
.float-card strong,
.float-card small {
  display: block;
}
.float-card strong {
  font-size: 10px;
}
.float-card small {
  margin-top: 3px;
  color: #718096;
  font-size: 8px;
}
.float-confirm {
  top: 11%;
  left: -2%;
}
.float-slot {
  right: -3%;
  bottom: 8%;
}
.float-icon {
  display: grid;
  width: 26px;
  height: 26px;
  place-items: center;
  border-radius: 50%;
  color: #fff;
  background: var(--grass);
  font-size: 13px;
}
.pulse-dot {
  width: 9px;
  height: 9px;
  flex: 0 0 auto;
  border-radius: 50%;
  background: var(--coral);
  box-shadow: 0 0 0 5px var(--coral-pale);
}
.hero-stats {
  display: grid;
  width: min(1180px, calc(100% - 48px));
  grid-template-columns: repeat(3, minmax(0, 1fr)) minmax(160px, 0.8fr);
  margin: 0 auto;
  border-top: 1px solid var(--line);
  border-bottom: 1px solid var(--line);
}
.hero-stats > div {
  display: grid;
  gap: 3px;
  padding: 20px 24px;
  border-right: 1px solid var(--line);
}
.hero-stats > div:first-child {
  padding-left: 0;
}
.hero-stats > div:last-child {
  border-right: 0;
}
.hero-stats strong {
  font-family: Manrope, sans-serif;
  font-size: 23px;
  font-weight: 700;
}
.hero-stats span,
.hero-stats small {
  color: #718096;
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.2px;
  text-transform: uppercase;
}
.stats-price {
  justify-content: center;
  align-content: center;
  justify-items: end;
}
.stats-price strong {
  color: var(--forest);
}
.section {
  width: min(1180px, calc(100% - 48px));
  margin: 0 auto;
  padding: clamp(68px, 9vw, 118px) 0 0;
  scroll-margin-top: 92px;
}
.section-heading {
  max-width: 650px;
  margin-bottom: 35px;
}
.section-heading.centered {
  margin-right: auto;
  margin-left: auto;
  text-align: center;
}
.section-eyebrow {
  margin: 0 0 11px;
  color: #718096;
  font-size: 9px;
  font-weight: 800;
  letter-spacing: 1.45px;
  text-transform: uppercase;
}
.section-heading h2 {
  margin: 0;
  color: var(--ink);
  font-family: Manrope, sans-serif;
  font-size: clamp(31px, 4vw, 46px);
  font-weight: 700;
  letter-spacing: -2px;
  line-height: 1.05;
}
.section-heading > p:last-child {
  max-width: 540px;
  margin: 17px 0 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.7;
}
.section-heading.centered > p:last-child {
  margin-right: auto;
  margin-left: auto;
}
.catalog-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
.catalog-card {
  display: flex;
  min-height: 252px;
  flex-direction: column;
  padding: 22px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--paper);
  transition:
    transform 0.2s,
    border-color 0.2s,
    box-shadow 0.2s;
}
.catalog-card:hover {
  border-color: #b9cbe0;
  box-shadow: 0 18px 40px rgba(23, 43, 77, 0.08);
  transform: translateY(-3px);
}
.catalog-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.catalog-category {
  color: #66758a;
  font-size: 8px;
  font-weight: 800;
  letter-spacing: 1.2px;
  text-transform: uppercase;
}
.catalog-icon {
  display: grid;
  width: 35px;
  height: 35px;
  place-items: center;
  border-radius: 6px;
  color: var(--forest);
  background: var(--lime);
  font-size: 17px;
}
.catalog-card h3 {
  margin: 20px 0 0;
  color: var(--ink);
  font-family: Manrope, sans-serif;
  font-size: 20px;
  font-weight: 700;
  letter-spacing: -0.8px;
}
.catalog-hours {
  margin: 6px 0 0;
  color: #66758a;
  font-size: 10px;
}
.catalog-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
  margin-top: 19px;
}
.catalog-tags span {
  padding: 5px 7px;
  border: 1px solid var(--line);
  border-radius: 4px;
  color: #5a6b81;
  background: var(--soft);
  font-size: 8px;
  font-weight: 600;
}
.catalog-card-footer {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 12px;
  margin-top: auto;
  padding-top: 22px;
}
.catalog-card-footer small,
.catalog-card-footer strong {
  display: block;
}
.catalog-card-footer small {
  color: #718096;
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
}
.catalog-card-footer strong {
  margin-top: 3px;
  font-family: Manrope, sans-serif;
  font-size: 17px;
}
.catalog-link {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 0 0 3px;
  border: 0;
  border-bottom: 1px solid #b9cbe0;
  color: var(--forest);
  background: transparent;
  font-size: 10px;
  font-weight: 800;
}
.catalog-link span {
  transition: transform 0.16s;
}
.catalog-link:hover span {
  transform: translateX(3px);
}
.catalog-skeleton {
  display: grid;
  min-height: 252px;
  align-content: start;
  gap: 13px;
  padding: 22px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--paper);
}
.catalog-skeleton span {
  height: 13px;
  border-radius: 3px;
  background: linear-gradient(90deg, #edf1f6, #f7faff, #edf1f6);
  background-size: 200% 100%;
  animation: shimmer 1.4s linear infinite;
}
.catalog-skeleton span:nth-child(1) {
  width: 35%;
}
.catalog-skeleton span:nth-child(2) {
  width: 70%;
  height: 25px;
}
.catalog-skeleton span:nth-child(3) {
  width: 52%;
}
.catalog-skeleton span:nth-child(4) {
  width: 100%;
  height: 70px;
}
.catalog-message {
  display: grid;
  justify-items: start;
  gap: 8px;
  padding: 28px;
  border: 1px dashed #d8e0ea;
  border-radius: 8px;
  background: var(--soft);
}
.catalog-message strong {
  font-family: Manrope, sans-serif;
  font-size: 18px;
}
.catalog-message p {
  margin: 0 0 4px;
  color: var(--muted);
  font-size: 12px;
}
.booking-section {
  padding-top: clamp(76px, 10vw, 132px);
}
.booking-shell {
  display: grid;
  grid-template-columns: minmax(0, 1.55fr) minmax(300px, 0.72fr);
  align-items: start;
  gap: 22px;
}
.booking-main {
  padding: clamp(20px, 3vw, 34px);
  border: 1px solid var(--line);
  border-radius: 10px;
  background: var(--paper);
  box-shadow: 0 22px 60px rgba(23, 43, 77, 0.06);
}
.steps {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 9px;
  margin-bottom: 29px;
  padding-bottom: 24px;
  border-bottom: 1px solid #edf1f5;
}
.step {
  display: flex;
  align-items: center;
  gap: 9px;
  color: #8494a8;
}
.step-number {
  display: grid;
  width: 25px;
  height: 25px;
  flex: 0 0 auto;
  place-items: center;
  border: 1px solid #d8e0ea;
  border-radius: 50%;
  color: #7b899b;
  font-size: 9px;
  font-weight: 800;
}
.step strong,
.step small {
  display: block;
}
.step strong {
  color: #66758a;
  font-size: 10px;
  font-weight: 700;
}
.step small {
  margin-top: 2px;
  font-size: 8px;
}
.step.active .step-number {
  border-color: var(--forest);
  color: #fff;
  background: var(--forest);
}
.step.active strong {
  color: var(--ink);
}
.step.done .step-number {
  border-color: #cfe3d8;
  color: var(--success);
  background: var(--success-pale);
}
.step.done strong {
  color: var(--success);
}
.booking-block + .booking-block {
  margin-top: 34px;
  padding-top: 31px;
  border-top: 1px solid #edf1f5;
}
.block-head {
  display: flex;
  align-items: flex-start;
  gap: 13px;
  margin-bottom: 20px;
}
.block-number {
  display: grid;
  width: 28px;
  height: 28px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 6px;
  color: var(--forest);
  background: var(--lime);
  font-family: Manrope, sans-serif;
  font-size: 9px;
  font-weight: 800;
}
.block-head h3 {
  margin: 1px 0 0;
  font-family: Manrope, sans-serif;
  font-size: 17px;
  font-weight: 700;
  letter-spacing: -0.4px;
}
.block-head p {
  margin: 5px 0 0;
  color: #718096;
  font-size: 10px;
  line-height: 1.5;
}
.space-picker {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}
.space-option {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 10px;
  padding: 11px 12px;
  border: 1px solid #d8e0ea;
  border-radius: 6px;
  color: var(--ink);
  background: #fff;
  text-align: left;
  transition:
    border-color 0.16s,
    background 0.16s;
}
.space-option:hover {
  border-color: #9bb2d0;
}
.space-option.selected {
  border-color: var(--forest);
  background: #f3f7fc;
  box-shadow: inset 0 0 0 1px var(--forest);
}
.space-option-icon {
  display: grid;
  width: 30px;
  height: 30px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 7px;
  background: var(--lime);
  font-size: 14px;
}
.space-option > span:nth-child(2) {
  min-width: 0;
}
.space-option strong,
.space-option small {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.space-option strong {
  font-size: 10px;
}
.space-option small {
  margin-top: 3px;
  color: #718096;
  font-size: 8px;
}
.space-option b {
  margin-left: auto;
  color: #66758a;
  font-size: 9px;
  font-weight: 700;
  white-space: nowrap;
}
.option-picker {
  display: grid;
  gap: 7px;
  margin-top: 17px;
}
.picker-label {
  margin: 0 0 2px;
  color: #718096;
  font-size: 8px;
  font-weight: 800;
  letter-spacing: 0.9px;
  text-transform: uppercase;
}
.option-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 13px;
  border: 1px solid var(--line);
  border-radius: 6px;
  color: var(--ink);
  background: #fff;
  text-align: left;
  transition:
    border-color 0.16s,
    background 0.16s;
}
.option-option:hover {
  border-color: #9bb2d0;
}
.option-option.selected {
  border-color: #9bb2d0;
  background: #f3f7fc;
}
.option-option strong,
.option-option small {
  display: block;
}
.option-option strong {
  font-size: 10px;
}
.option-option small {
  margin-top: 3px;
  color: #718096;
  font-size: 8px;
}
.option-option b {
  font-family: Manrope, sans-serif;
  font-size: 12px;
}
.date-row {
  display: flex;
  align-items: end;
  gap: 18px;
  margin-top: 19px;
  padding: 15px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--soft);
}
.landing-field {
  display: grid;
  min-width: 0;
  gap: 7px;
}
.landing-field > span {
  color: #435770;
  font-size: 9px;
  font-weight: 700;
}
.landing-field input {
  width: 100%;
  min-height: 41px;
  padding: 9px 11px;
  border: 1px solid #d8e0ea;
  border-radius: 5px;
  color: var(--ink);
  background: #fff;
  font-size: 11px;
  transition:
    border-color 0.16s,
    box-shadow 0.16s;
}
.landing-field input::placeholder {
  color: #8fa0b5;
}
.landing-field input:focus {
  border-color: #6d91bf;
  box-shadow: 0 0 0 2px #e6eef8;
  outline: 0;
}
.date-field {
  flex: 0 0 170px;
}
.date-copy {
  display: grid;
  gap: 4px;
  padding-bottom: 9px;
}
.date-copy small {
  color: #718096;
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
}
.date-copy strong {
  font-size: 11px;
  text-transform: capitalize;
}
.slots-heading {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  margin: 21px 0 10px;
}
.slots-heading strong {
  font-size: 10px;
}
.slots-heading small {
  color: #7b899b;
  font-size: 8px;
}
.slot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(98px, 1fr));
  gap: 7px;
}
.slot {
  display: grid;
  justify-items: start;
  gap: 2px;
  padding: 10px 11px;
  border: 1px solid #d8e0ea;
  border-radius: 6px;
  color: var(--ink);
  background: #fff;
  transition:
    border-color 0.16s,
    background 0.16s;
}
.slot:hover {
  border-color: #9bb2d0;
  background: #f7faff;
}
.slot.selected {
  border-color: var(--forest);
  color: #fff;
  background: var(--forest);
}
.slot strong {
  font-family: Manrope, sans-serif;
  font-size: 12px;
}
.slot small {
  color: #718096;
  font-size: 8px;
}
.slot.selected small {
  color: #cfe0f2;
}
.slots-message {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px;
  border: 1px dashed #d8e0ea;
  border-radius: 6px;
  color: #66758a;
  background: var(--soft);
  font-size: 10px;
}
.slots-message.empty {
  border-style: solid;
}
.loading-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--coral);
  animation: pulse 1s ease-in-out infinite;
}
.contact-form {
  display: grid;
  gap: 15px;
}
.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}
.verification-card {
  padding: 15px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--soft);
}
.verification-card.verified {
  border-color: #cfe3d8;
  background: var(--success-pale);
}
.verification-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
}
.verification-kicker {
  display: block;
  margin-bottom: 4px;
  color: var(--coral);
  font-size: 8px;
  font-weight: 800;
  letter-spacing: 1px;
  text-transform: uppercase;
}
.verification-head strong {
  display: block;
  font-size: 11px;
}
.verification-head p {
  max-width: 440px;
  margin: 4px 0 0;
  color: #7b899b;
  font-size: 9px;
  line-height: 1.5;
}
.verification-state {
  padding: 4px 7px;
  border-radius: 4px;
  color: var(--warning);
  background: var(--warning-pale);
  font-size: 7px;
  font-weight: 800;
  letter-spacing: 0.8px;
  text-transform: uppercase;
  white-space: nowrap;
}
.verification-state.ok {
  color: var(--success);
  background: var(--success-pale);
}
.verification-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 9px;
  margin-top: 12px;
}
.code-entry {
  display: flex;
  gap: 7px;
}
.code-entry input {
  width: 110px;
  min-height: 40px;
  padding: 8px 10px;
  border: 1px solid #d8e0ea;
  border-radius: 5px;
  background: #fff;
  font-family: Manrope, sans-serif;
  font-size: 17px;
  font-weight: 700;
  letter-spacing: 5px;
  text-align: center;
}
.verification-message {
  margin: 11px 0 0;
  color: var(--success);
  font-size: 9px;
  line-height: 1.5;
}
.dev-code {
  margin: 10px 0 0;
  padding: 7px 9px;
  border-radius: 5px;
  color: var(--warning);
  background: var(--warning-pale);
  font-size: 9px;
}
.dev-code strong {
  letter-spacing: 2px;
}
.submit-hint {
  display: flex;
  align-items: center;
  gap: 7px;
  margin: 0;
  padding: 9px 11px;
  border: 1px solid #d8e0ea;
  border-radius: 5px;
  color: #5a6b81;
  background: var(--soft);
  font-size: 9px;
  line-height: 1.5;
}
.submit-hint span {
  color: var(--coral);
  font-weight: 800;
}
.submit-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  padding-top: 4px;
}
.submit-total small,
.submit-total strong {
  display: block;
}
.submit-total small {
  color: #718096;
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
}
.submit-total strong {
  margin-top: 3px;
  font-family: Manrope, sans-serif;
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.8px;
}
.privacy-note {
  margin: 0;
  color: #7b899b;
  font-size: 8px;
  line-height: 1.55;
}
.inline-error {
  margin: 0;
  padding: 10px 11px;
  border: 1px solid #ecd5d5;
  border-radius: 5px;
  color: #884343;
  background: #fbf3f3;
  font-size: 9px;
  line-height: 1.5;
}
.booking-summary {
  position: sticky;
  top: 94px;
}
.summary-card,
.confirmation-card {
  padding: 24px;
  border: 1px solid var(--line);
  border-radius: 10px;
  background: var(--paper);
  box-shadow: 0 18px 50px rgba(23, 43, 77, 0.05);
}
.summary-card h3,
.confirmation-card h3 {
  margin: 4px 0 0;
  font-family: Manrope, sans-serif;
  font-size: 23px;
  font-weight: 700;
  letter-spacing: -1px;
  line-height: 1.15;
}
.summary-option {
  margin: 8px 0 0;
  color: #66758a;
  font-size: 10px;
  line-height: 1.5;
}
.summary-list {
  display: grid;
  gap: 0;
  margin: 24px 0 0;
  padding: 0;
  border-top: 1px solid #edf1f5;
}
.summary-list > div {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 14px;
  padding: 11px 0;
  border-bottom: 1px solid #f0f4f8;
}
.summary-list dt {
  color: #718096;
  font-size: 9px;
}
.summary-list dd {
  margin: 0;
  font-size: 10px;
  font-weight: 700;
  text-align: right;
}
.summary-total {
  padding-top: 14px !important;
}
.summary-total dt {
  color: #435770;
  font-weight: 700;
}
.summary-total dd {
  font-family: Manrope, sans-serif;
  font-size: 19px !important;
}
.summary-note {
  display: flex;
  gap: 9px;
  margin-top: 18px;
  padding: 12px;
  border-radius: 6px;
  background: #f3f7fc;
}
.summary-note span {
  color: var(--grass);
  font-size: 11px;
}
.summary-note p {
  margin: 0;
  color: #66758a;
  font-size: 9px;
  line-height: 1.5;
}
.confirmation-card {
  border-color: #cfe3d8;
  background: linear-gradient(165deg, #f7fbf8, #eef7f1);
}
.confirmation-check {
  display: grid;
  width: 34px;
  height: 34px;
  place-items: center;
  margin-bottom: 16px;
  border-radius: 50%;
  color: #fff;
  background: var(--grass);
  font-size: 16px;
}
.confirmation-copy {
  margin: 10px 0 0;
  color: #66758a;
  font-size: 10px;
  line-height: 1.6;
}
.confirmation-folio {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  margin-top: 18px;
  padding: 10px 12px;
  border: 1px dashed #cfe3d8;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.6);
}
.confirmation-folio small {
  color: #7b899b;
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
}
.confirmation-folio strong {
  font-family: Manrope, sans-serif;
  font-size: 13px;
  letter-spacing: 0.6px;
}
.how-section {
  padding-top: clamp(80px, 10vw, 128px);
}
.how-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
.how-grid article {
  padding: 24px;
  border-top: 1px solid #d8e0ea;
}
.how-number {
  color: var(--coral);
  font-family: Manrope, sans-serif;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 1px;
}
.how-grid h3 {
  margin: 22px 0 0;
  font-family: Manrope, sans-serif;
  font-size: 19px;
  font-weight: 700;
  letter-spacing: -0.7px;
}
.how-grid p {
  max-width: 285px;
  margin: 9px 0 0;
  color: #66758a;
  font-size: 11px;
  line-height: 1.65;
}
.closing-banner {
  display: flex;
  width: min(1180px, calc(100% - 48px));
  align-items: center;
  justify-content: space-between;
  gap: 28px;
  margin: clamp(70px, 9vw, 118px) auto 0;
  padding: clamp(28px, 4vw, 48px);
  border-radius: 10px;
  color: #f4f7fb;
  background:
    radial-gradient(circle at 84% 10%, rgba(57, 114, 188, 0.26), transparent 35%),
    linear-gradient(135deg, #172b4d, #203958);
}
.closing-banner .section-eyebrow {
  color: #a9bddb;
}
.closing-banner h2 {
  max-width: 560px;
  margin: 0;
  color: #f4f7fb;
  font-family: Manrope, sans-serif;
  font-size: clamp(28px, 4vw, 44px);
  font-weight: 700;
  letter-spacing: -2px;
  line-height: 1.05;
}
.landing-footer {
  display: flex;
  width: min(1180px, calc(100% - 48px));
  flex-wrap: wrap;
  align-items: center;
  gap: 24px;
  margin: 0 auto;
  padding: 44px 0 52px;
}
.footer-brand {
  display: flex;
  align-items: center;
  gap: 8px;
}
.landing-footer p {
  max-width: 430px;
  margin: 0;
  color: #7b899b;
  font-size: 9px;
  line-height: 1.5;
}
.footer-links {
  display: flex;
  gap: 18px;
  margin-left: auto;
  color: #5a6b81;
  font-size: 9px;
  font-weight: 700;
}
.footer-links a:hover {
  color: var(--forest);
}
@keyframes court-in {
  from {
    opacity: 0;
    transform: translateY(20px) rotate(-5deg);
  }
  to {
    opacity: 1;
    transform: translateY(0) rotate(-3.2deg);
  }
}
@keyframes ball-float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-7px);
  }
}
@keyframes shimmer {
  to {
    background-position: -200% 0;
  }
}
@keyframes pulse {
  0%,
  100% {
    opacity: 0.45;
    transform: scale(0.85);
  }
  50% {
    opacity: 1;
    transform: scale(1);
  }
}
@media (max-width: 980px) {
  .hero {
    grid-template-columns: 1fr;
    gap: 28px;
  }
  .hero-visual {
    min-height: 380px;
  }
  .catalog-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .booking-shell {
    grid-template-columns: 1fr;
  }
  .booking-summary {
    position: static;
  }
  .summary-card,
  .confirmation-card {
    max-width: none;
  }
}
@media (max-width: 760px) {
  .nav-inner {
    width: calc(100% - 32px);
    min-height: 64px;
    gap: 12px;
  }
  .nav-links,
  .nav-panel {
    display: none;
  }
  .nav-actions {
    margin-left: auto;
  }
  .hero,
  .hero-stats,
  .section,
  .closing-banner,
  .landing-footer {
    width: calc(100% - 32px);
  }
  .hero {
    padding-top: 50px;
  }
  .hero h1 {
    font-size: clamp(43px, 13vw, 62px);
    letter-spacing: -2.8px;
  }
  .hero-lead {
    font-size: 13px;
  }
  .hero-trust {
    gap: 11px 16px;
  }
  .hero-visual {
    min-height: 330px;
  }
  .court-card {
    width: min(100%, 350px);
  }
  .float-confirm {
    top: 7%;
    left: 0;
  }
  .float-slot {
    right: 0;
    bottom: 4%;
  }
  .hero-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .hero-stats > div {
    padding: 17px 14px;
    border-bottom: 1px solid var(--line);
  }
  .hero-stats > div:first-child {
    padding-left: 14px;
  }
  .hero-stats > div:nth-child(2n) {
    border-right: 0;
  }
  .stats-price {
    justify-items: start;
  }
  .catalog-grid,
  .how-grid {
    grid-template-columns: 1fr;
  }
  .steps {
    gap: 5px;
  }
  .step {
    display: grid;
    justify-items: start;
    gap: 6px;
  }
  .step small {
    display: none;
  }
  .space-picker,
  .form-grid {
    grid-template-columns: 1fr;
  }
  .date-row {
    align-items: stretch;
    flex-direction: column;
    gap: 10px;
  }
  .date-field {
    flex: auto;
  }
  .date-copy {
    padding-bottom: 0;
  }
  .submit-row {
    align-items: stretch;
    flex-direction: column;
  }
  .submit-row .cta {
    width: 100%;
  }
  .closing-banner {
    align-items: flex-start;
    flex-direction: column;
  }
  .footer-links {
    width: 100%;
    margin-left: 0;
  }
}
@media (max-width: 470px) {
  .hero h1 {
    font-size: 43px;
  }
  .hero-visual {
    min-height: 300px;
  }
  .float-card {
    padding: 8px 10px;
  }
  .float-card strong {
    font-size: 9px;
  }
  .float-card small {
    font-size: 7px;
  }
  .booking-main {
    padding: 18px;
  }
  .verification-actions {
    align-items: stretch;
    flex-direction: column;
  }
  .code-entry {
    width: 100%;
  }
  .code-entry input {
    flex: 1;
    width: auto;
  }
  .slot-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
@media (prefers-reduced-motion: reduce) {
  .court-card,
  .court-ball,
  .loading-dot,
  .catalog-skeleton span {
    animation: none;
  }
}
</style>
