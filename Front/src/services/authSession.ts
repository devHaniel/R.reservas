import type { AuthTokens, AuthUser } from '../types/api'

const tokenStorageKey = 'reservas.auth.tokens.v1'

export function readTokens(): AuthTokens | null {
  try {
    const raw = sessionStorage.getItem(tokenStorageKey)
    if (!raw) return null
    const value: unknown = JSON.parse(raw)
    if (
      !value ||
      typeof value !== 'object' ||
      typeof (value as AuthTokens).accessToken !== 'string' ||
      typeof (value as AuthTokens).refreshToken !== 'string'
    ) {
      return null
    }
    return value as AuthTokens
  } catch {
    return null
  }
}

export function saveTokens(tokens: AuthTokens) {
  sessionStorage.setItem(tokenStorageKey, JSON.stringify(tokens))
}

export function clearTokens() {
  sessionStorage.removeItem(tokenStorageKey)
}

export function userFromAccessToken(token: string): AuthUser | null {
  try {
    const payloadSegment = token.split('.')[1]
    if (!payloadSegment) return null
    const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
    const bytes = Uint8Array.from(atob(base64), (character) => character.charCodeAt(0))
    const payload: Record<string, unknown> = JSON.parse(new TextDecoder().decode(bytes))
    const roleClaim =
      payload.role ??
      payload.roles ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    const roleValues = (Array.isArray(roleClaim) ? roleClaim : [roleClaim]).filter(
      (role): role is string => typeof role === 'string',
    )
    const roles = [...new Set(roleValues)].filter(
      (role): role is AuthUser['roles'][number] => role === 'Admin' || role === 'Vendedor',
    )
    const id =
      payload.sub ??
      payload.nameid ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
    const email =
      payload.email ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
    const name =
      payload.name ??
      payload.unique_name ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']

    if (id === undefined || roles.length === 0) return null
    return {
      id: String(id),
      email: typeof email === 'string' ? email : typeof name === 'string' ? name : String(id),
      name: typeof name === 'string' ? name : typeof email === 'string' ? email : String(id),
      roles,
    }
  } catch {
    return null
  }
}

export function currentUser(): AuthUser | null {
  const tokens = readTokens()
  return tokens ? userFromAccessToken(tokens.accessToken) : null
}
