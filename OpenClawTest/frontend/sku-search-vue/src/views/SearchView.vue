<template>
  <div class="search-view">
    <!-- 顶部搜索区 -->
    <header class="search-header">
      <h1 class="title">🔍 SKU 智能搜索</h1>
      <p class="subtitle">支持语义理解、模糊搜索、同义词扩展 · skudetail2 商品库</p>

      <div class="search-wrap">
        <SearchBar placeholder="输入商品名称，例如：户外用品、帐篷、牧高笛…" />
      </div>

      <!-- 热门搜索 -->
      <div class="hot-searches" v-if="!store.results.length">
        <span class="label">热门：</span>
        <el-button
          v-for="kw in hotKeywords"
          :key="kw"
          size="small"
          text
          @click="store.doSearch(kw)"
        >{{ kw }}</el-button>
      </div>
    </header>

    <!-- 结果区 -->
    <main class="result-area" v-if="store.results.length || store.loading">
      <!-- 统计信息栏 -->
      <div class="result-stats" v-if="!store.loading">
        <span class="stats-text">
          找到 <strong>{{ store.results.length }}</strong> 个商品
          <template v-if="store.query">（"{{ store.query }}"）</template>
        </span>

        <!-- 店铺快速过滤 -->
        <div class="shop-filters" v-if="availableShops.length > 0">
          <span class="filter-label">店铺：</span>
          <el-check-tag
            :checked="!store.shopId"
            @change="filterByShop(undefined)"
          >全部</el-check-tag>
          <el-check-tag
            v-for="shop in availableShops"
            :key="shop.id"
            :checked="store.shopId === shop.id"
            @change="filterByShop(shop.id)"
          >{{ shop.name || '店铺' + shop.id }} ({{ shop.count }})</el-check-tag>
        </div>
      </div>

      <!-- 商品网格 -->
      <div v-loading="store.loading" class="sku-grid">
        <SkuCard v-for="item in store.results" :key="item.recordId" :item="item" />
      </div>

      <!-- 空结果 -->
      <el-empty
        v-if="!store.loading && store.results.length === 0"
        description="没有找到匹配的商品，换个关键词试试"
      />
    </main>

    <!-- 首页空状态 -->
    <div class="empty-state" v-else>
      <el-icon size="80" color="#dcdfe6"><Search /></el-icon>
      <p>输入关键词开始搜索</p>
      <p class="hint">支持自然语言，例如"适合家庭露营的装备"</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import SearchBar from '@/components/SearchBar.vue'
import SkuCard   from '@/components/SkuCard.vue'
import { useSearchStore } from '@/stores/search'

const store = useSearchStore()

const hotKeywords = ['户外用品', '帐篷', '牧高笛', '露营装备', '礼品卡', '景区门票']

const availableShops = computed(() => {
  const shopMap = new Map<number, { id: number; name: string; count: number }>()
  for (const item of store.results) {
    if (!shopMap.has(item.shopId)) {
      shopMap.set(item.shopId, { id: item.shopId, name: `店铺${item.shopId}`, count: 0 })
    }
    shopMap.get(item.shopId)!.count++
  }
  return [...shopMap.values()]
})

function filterByShop(shopId?: number) {
  store.shopId = shopId
  store.doSearch()
}
</script>

<style scoped>
.search-view {
  min-height: 100vh;
  background: #f5f7fa;
}

.search-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 40px 24px 32px;
  text-align: center;
  color: #fff;
}
.title    { margin: 0 0 8px; font-size: 28px; font-weight: 700; }
.subtitle { margin: 0 0 24px; opacity: 0.85; font-size: 14px; }

.search-wrap {
  max-width: 680px;
  margin: 0 auto;
}

.hot-searches {
  margin-top: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 4px;
  font-size: 13px;
  opacity: 0.9;
}
.hot-searches .label { opacity: 0.7; }
.hot-searches :deep(.el-button) { color: #fff !important; opacity: 0.85; }
.hot-searches :deep(.el-button:hover) { opacity: 1; }

.result-area { max-width: 1200px; margin: 0 auto; padding: 20px 16px; }

.result-stats {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 16px;
  font-size: 14px;
  color: #606266;
}
.shop-filters {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  align-items: center;
}
.filter-label {
  font-size: 13px;
  color: #909399;
}

.sku-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  color: #c0c4cc;
  gap: 12px;
  font-size: 15px;
}
.hint { font-size: 13px; opacity: 0.7; }
</style>
