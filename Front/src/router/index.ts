// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { createRouter, createWebHistory } from 'vue-router'
import ApiWorkspace from '../views/ApiWorkspace.vue'
import LandingView from '../views/LandingView.vue'
import LoginView from '../views/LoginView.vue'

const workspaceRoutes = [
  { path: '/panel', name: 'home', meta: { section: 'overview' } },
  { path: '/reservas', name: 'reservations', meta: { section: 'reservations' } },
  { path: '/calendario', name: 'calendar', meta: { section: 'calendar' } },
  { path: '/clientes', name: 'clients', meta: { section: 'clients' } },
  { path: '/espacios', name: 'resources', meta: { section: 'resources' } },
  { path: '/opciones', name: 'services', meta: { section: 'services' } },
  { path: '/usuarios', name: 'users', meta: { section: 'users', admin: true } },
].map((route) => ({
  ...route,
  component: ApiWorkspace,
  meta: { ...route.meta, requiresAuth: true },
}))

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'landing', component: LandingView },
    { path: '/login', name: 'login', component: LoginView },
    ...workspaceRoutes,
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

export default router
