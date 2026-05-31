import { defineStore } from 'pinia'
import { ref } from 'vue'
import { searchSkus, type SearchResponse, type ParsedQuery } from '@/api/search'

export const useSearchStore = defineStore('search', () => {
  const query       = ref('')
  const shopId      = ref<number | undefined>(undefined)
  const results     = ref<SearchResponse['items']>([])
  const parsedQuery = ref<ParsedQuery | null>(null)
  const loading     = ref(false)
  const elapsedMs   = ref(0)
  const error       = ref('')

  async function doSearch(q?: string) {
    const searchQuery = q ?? query.value
    if (!searchQuery.trim()) return

    query.value = searchQuery
    loading.value = true
    error.value   = ''

    try {
      const res = await searchSkus({
        query:   searchQuery,
        topK:    24,
        shopId:  shopId.value,
      })
      results.value     = res.items
      parsedQuery.value = res.parsedQuery
      elapsedMs.value   = res.elapsedMs
    } catch (e: any) {
      error.value = e?.response?.data?.error ?? '搜索失败，请稍后重试'
    } finally {
      loading.value = false
    }
  }

  function reset() {
    query.value       = ''
    results.value     = []
    parsedQuery.value = null
    error.value       = ''
  }

  return { query, shopId, results, parsedQuery, loading, elapsedMs, error, doSearch, reset }
})
