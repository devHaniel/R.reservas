// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { clearTokens, readTokens, saveTokens } from './authSession'
import type { AuthTokens } from '../types/api'

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5020').replace(
  /\/+$/,
  '',
)

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly detail?: unknown,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
  auth?: boolean
  retried?: boolean
}

let refreshInFlight: Promise<string | null> | null = null

function notifySessionExpired() {
  clearTokens()
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('reservas:session-expired'))
  }
}

function beginRefresh(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight
  refreshInFlight = (async () => {
    const tokens = readTokens()
    if (!tokens) {
      notifySessionExpired()
      return null
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(tokens),
      })
      if (!response.ok) {
        notifySessionExpired()
        return null
      }
      const rotated = (await response.json()) as AuthTokens
      if (!rotated.accessToken || !rotated.refreshToken) {
        notifySessionExpired()
        return null
      }
      saveTokens(rotated)
      return rotated.accessToken
    } catch {
      notifySessionExpired()
      return null
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

async function readResponse(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined
  const text = await response.text()
  if (!text) return undefined
  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('json') || /^[\s]*[\[{]/.test(text)) {
    try {
      return JSON.parse(text) as unknown
    } catch {
      return text
    }
  }
  return text
}

function errorMessage(status: number, body: unknown): string {
  if (body && typeof body === 'object') {
    const problem = body as Record<string, unknown>
    for (const key of ['mensaje', 'detail', 'title', 'message']) {
      if (typeof problem[key] === 'string') return problem[key]
    }
    if (problem.errors && typeof problem.errors === 'object') {
      const messages = Object.values(problem.errors as Record<string, unknown>)
        .flatMap((value) => (Array.isArray(value) ? value : [value]))
        .filter((value): value is string => typeof value === 'string')
      if (messages.length) return messages.join(' ')
    }
  }
  if (status === 401) return 'Tu sesión venció. Inicia sesión nuevamente.'
  if (status === 403) return 'Tu usuario no tiene permiso para realizar esta acción.'
  if (status === 404) return 'No se encontró el registro solicitado.'
  if (status === 429)
    return 'Se alcanzó el límite de solicitudes. Espera unos segundos e inténtalo de nuevo.'
  if (status >= 500) return 'El servidor encontró un error. Inténtalo de nuevo más tarde.'
  return `La solicitud falló (HTTP ${status}).`
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { auth = true, retried = false, body, headers: inputHeaders, ...requestOptions } = options
  const headers = new Headers(inputHeaders)
  headers.set('Accept', 'application/json')
  if (body !== undefined && !(body instanceof FormData))
    headers.set('Content-Type', 'application/json')
  const tokens = auth ? readTokens() : null
  if (tokens?.accessToken) headers.set('Authorization', `Bearer ${tokens.accessToken}`)

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...requestOptions,
      headers,
      body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(`No se pudo conectar con la API en ${API_BASE_URL}.`, 0)
  }

  if (response.status === 401 && auth && !retried && path !== '/api/auth/refresh') {
    const refreshedAccessToken = await beginRefresh()
    if (refreshedAccessToken) return apiRequest<T>(path, { ...options, retried: true })
  }

  const result = await readResponse(response)
  if (!response.ok)
    throw new ApiError(errorMessage(response.status, result), response.status, result)
  return result as T
}
