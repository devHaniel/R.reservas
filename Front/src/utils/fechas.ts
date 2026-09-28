// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

// Utilidades de fecha/hora para la API de reservas.
//
// La API trabaja con hora local del negocio como un string "de reloj":
//
//   yyyy-MM-ddTHH:mm   ->   "2026-10-10T16:00"
//
// Esa hora NO es UTC y NO lleva zona horaria. Por eso todo se manipula como
// texto y nunca se convierte con `new Date(...)` / `toISOString()` (eso
// desplazaría la hora). Ver FRONTEND_GUIDE.md.

const pad = (valor: number) => String(valor).padStart(2, '0')

/** Convierte un Date local a "yyyy-MM-ddTHH:mm" SIN pasar por UTC. */
export function aFechaApi(date: Date): string {
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  )
}

/** Instante actual en formato "yyyy-MM-ddTHH:mm" (hora local del navegador). */
export function ahoraApi(now = new Date()): string {
  return aFechaApi(now)
}

/**
 * Normaliza un valor de `<input type="datetime-local">` al formato canónico
 * "yyyy-MM-ddTHH:mm". Acepta "2026-10-10T16:00", "2026-10-10 16:00" o con
 * segundos, y recorta cualquier sufijo (`Z`, offset, milisegundos).
 */
export function limpiarFechaApi(valor: string): string {
  if (!valor) return ''
  return valor.trim().replace(' ', 'T').slice(0, 16)
}

/** Parte de fecha ("yyyy-MM-dd") de un string de reloj. */
export function claveFecha(isoLocal: string): string {
  return limpiarFechaApi(isoLocal).slice(0, 10)
}

/** Parte de hora ("HH:mm") de un string de reloj. */
export function horaDe(isoLocal: string): string {
  return limpiarFechaApi(isoLocal).slice(11, 16)
}

/**
 * Formatea "2026-10-10T16:00" como "10/10/2026 16:00" sin reinterpretar zonas
 * horarias (solo manipulación de string).
 */
export function formatearFecha(isoLocal: string): string {
  const valor = limpiarFechaApi(isoLocal)
  if (valor.length < 16) return valor
  const [fecha = '', hora = ''] = valor.split('T')
  const [anio = '', mes = '', dia = ''] = fecha.split('-')
  return `${dia}/${mes}/${anio} ${hora}`
}

/** Formatea solo la hora "HH:mm" de un string de reloj. */
export function formatearHora(isoLocal: string): string {
  return horaDe(isoLocal)
}

/** Construye un Date local (no UTC) a partir de "yyyy-MM-ddTHH:mm". */
export function aDateLocal(isoLocal: string): Date {
  const valor = limpiarFechaApi(isoLocal)
  const [fecha = '', hora = ''] = valor.split('T')
  const [anio = '1970', mes = '1', dia = '1'] = fecha.split('-')
  const [horas = '0', minutos = '0'] = hora.split(':')
  return new Date(Number(anio), Number(mes) - 1, Number(dia), Number(horas), Number(minutos))
}

/** Fecha de hoy como clave "yyyy-MM-dd" (hora local del navegador). */
export function fechaHoy(now = new Date()): string {
  return claveFecha(aFechaApi(now))
}

/** Suma minutos a un string de reloj y devuelve otro string de reloj. */
export function sumarMinutos(isoLocal: string, minutos: number): string {
  const base = aDateLocal(isoLocal)
  base.setMinutes(base.getMinutes() + minutos)
  return aFechaApi(base)
}

/**
 * Etiqueta larga legible ("domingo, 27 de septiembre") a partir de una clave
 * de fecha o de un string de reloj.
 */
export function formatearFechaLarga(
  valor: string,
  options: Intl.DateTimeFormatOptions = { weekday: 'long', day: 'numeric', month: 'long' },
): string {
  const [anio = '1970', mes = '1', dia = '1'] = claveFecha(valor).split('-')
  const fecha = new Date(Number(anio), Number(mes) - 1, Number(dia), 12)
  return new Intl.DateTimeFormat('es-MX', options).format(fecha)
}