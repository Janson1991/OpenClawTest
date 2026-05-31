import axios from 'axios'

export interface SearchRequest {
  query:    string
  topK?:    number
  shopId?:  number
  uCatId1?: number
  minScore?: number
}

export interface SkuSearchItem {
  recordId:    number
  goodsId:     string
  skuId:       string
  shopId:      number
  name:        string | null
  spuItemName: string | null
  brandName:   string | null
  priceSale:   number | null
  priceMarket: number | null
  goodsType:   string | null
  checkStatus: string | null
  state:       number
  autoState:   number
  score:       number
  source:      string
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
