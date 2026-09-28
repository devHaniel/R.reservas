// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { resourcesService } from '../services/resourcesService'
import { serviceTypesService } from '../services/serviceTypesService'
import type { ReservableResource, ResourceWrite, ServiceType, ServiceTypeWrite } from '../types/api'

export const useCatalogStore = defineStore('catalog', () => {
  const resources = ref<ReservableResource[]>([])
  const serviceTypes = ref<ServiceType[]>([])
  const serviceTypesForResource = ref<ServiceType[]>([])
  const resourcesLoading = ref(false)
  const serviceTypesLoading = ref(false)
  const loading = computed(() => resourcesLoading.value || serviceTypesLoading.value)
  const error = ref('')

  async function loadResources() {
    resourcesLoading.value = true
    error.value = ''
    try {
      resources.value = await resourcesService.list()
      return resources.value
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudieron cargar los recursos.'
      throw cause
    } finally {
      resourcesLoading.value = false
    }
  }

  async function loadServiceTypes(resourceId?: number) {
    serviceTypesLoading.value = true
    error.value = ''
    try {
      const result =
        resourceId === undefined
          ? await serviceTypesService.list()
          : await serviceTypesService.byResource(resourceId)
      if (resourceId === undefined) serviceTypes.value = result
      else serviceTypesForResource.value = result
      return result
    } catch (cause) {
      error.value =
        cause instanceof Error ? cause.message : 'No se pudieron cargar los tipos de servicio.'
      throw cause
    } finally {
      serviceTypesLoading.value = false
    }
  }

  async function saveResource(id: number | null, resource: ResourceWrite) {
    try {
      error.value = ''
      const result =
        id === null
          ? await resourcesService.create(resource)
          : await resourcesService.update(id, resource)
      await loadResources()
      return result
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo guardar el recurso.'
      throw cause
    }
  }

  async function removeResource(id: number) {
    try {
      error.value = ''
      await resourcesService.remove(id)
      await loadResources()
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo eliminar el recurso.'
      throw cause
    }
  }

  async function saveServiceType(id: number | null, serviceType: ServiceTypeWrite) {
    try {
      error.value = ''
      const result =
        id === null
          ? await serviceTypesService.create(serviceType)
          : await serviceTypesService.update(id, serviceType)
      await loadServiceTypes()
      return result
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo guardar el servicio.'
      throw cause
    }
  }

  async function removeServiceType(id: number) {
    try {
      error.value = ''
      await serviceTypesService.remove(id)
      await loadServiceTypes()
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'No se pudo eliminar el servicio.'
      throw cause
    }
  }

  return {
    resources,
    serviceTypes,
    serviceTypesForResource,
    loading,
    error,
    loadResources,
    loadServiceTypes,
    saveResource,
    removeResource,
    saveServiceType,
    removeServiceType,
  }
})
