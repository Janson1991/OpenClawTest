import axios from 'axios'

export interface SearchRequest {
  query:    string
  topK?:    number
  category?: string
  minScore?: number
}

export interface SkuSearchItem {
  id:       number
  skuCode:  string
  name:     string
  category: string | null
  brand:    string | null
  price:    number
  imageUrl: string | null
  score:    number
  source:   string
}

export interface ParsedQuery {
  keywords:   string[]
  synonyms:   string[]
  categories: string[]
  attributes: Record<string, string>
}

export interface SearchResponse {
  items:       SkuSearchItem[]
  parsedQuery: ParsedQuery
  total:       number
  elapsedMs:   number
}

const api = axios.create({ baseURL: '/api' })

export const searchSkus = (req: SearchRequest) =>
  api.post<SearchResponse>('/search', req).then(r => r.data)

export const getIndexStatus = () =>
  api.get('/admin/index/status').then(r => r.data)

export const triggerRebuildIndex = () =>
  api.post('/admin/index/rebuild').then(r => r.data)
