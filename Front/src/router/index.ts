import { createRouter, createWebHistory } from 'vue-router'
import ApiWorkspace from '../views/ApiWorkspace.vue'
import LoginView from '../views/LoginView.vue'
import UsersSection from '../components/UsersSection.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    {
      path: '/',
      name: 'home',
      component: ApiWorkspace,
      meta: { requiresAuth: true },
    },
    {
      path: '/usuarios',
      name: 'users',
      component: UsersSection,
      meta: { requiresAuth: true },
    },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

export default router
