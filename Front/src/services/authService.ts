import { apiRequest } from './apiClient'
import { clearTokens, saveTokens, userFromAccessToken } from './authSession'
import type {
  AuthTokens,
  AuthUser,
  LoginInput,
  RegisterVendorInput,
  RegisterVendorResult,
} from '../types/api'

export const authService = {
  async login(input: LoginInput): Promise<AuthUser> {
    const tokens = await apiRequest<AuthTokens>('/api/auth/login', {
      method: 'POST',
      body: input,
      auth: false,
    })
    if (!tokens?.accessToken || !tokens.refreshToken)
      throw new Error('La API devolvió una respuesta de inicio de sesión inválida.')
    saveTokens(tokens)
    const user = userFromAccessToken(tokens.accessToken)
    if (!user) {
      clearTokens()
      throw new Error('No se pudo leer el usuario y sus roles desde el token.')
    }
    return user
  },

  logout() {
    clearTokens()
  },

  async registerVendor(input: RegisterVendorInput) {
    return apiRequest<RegisterVendorResult>('/api/auth/register', {
      method: 'POST',
      body: input,
    })
  },
}
