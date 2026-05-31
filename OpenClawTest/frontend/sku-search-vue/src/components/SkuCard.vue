<template>
  <el-card class="sku-card" shadow="hover" :body-style="{ padding: '0' }">
    <!-- 商品图片（skudetail2 无图片字段，展示默认占位） -->
    <div class="img-wrap">
      <div class="img-placeholder">
        <el-icon size="40" color="#c0c4cc"><Goods /></el-icon>
        <span class="goods-type" v-if="item.goodsType">{{ item.goodsType }}</span>
      </div>

      <!-- 相似度得分徽标 -->
      <el-tag class="score-badge" size="small" :type="scoreType" effect="dark">
        {{ (item.score * 100).toFixed(0) }}%
      </el-tag>

      <!-- 上架状态 -->
      <el-tag
        class="status-badge"
        size="small"
        :type="item.state === 1 && item.autoState === 1 ? 'success' : 'info'"
        effect="plain"
      >
        {{ item.state === 1 && item.autoState === 1 ? '上架' : '下架' }}
      </el-tag>
    </div>

    <!-- 商品信息 -->
    <div class="sku-info">
      <p class="sku-name" :title="item.name">{{ item.name || '暂无名称' }}</p>

      <p class="sku-spec" v-if="item.spuItemName" :title="item.spuItemName">
        {{ item.spuItemName }}
      </p>

      <div class="sku-meta">
        <el-tag v-if="item.brandName" size="small" type="info" effect="plain">
          {{ item.brandName }}
        </el-tag>
        <span class="sku-id" :title="'SKU: ' + item.skuId">
          SKU: {{ item.skuId?.slice(0, 8) }}...
        </span>
      </div>

      <div class="sku-footer">
        <div class="price-group">
          <span class="price-sale" v-if="item.priceSale">¥{{ item.priceSale.toFixed(2) }}</span>
          <span class="price-market" v-if="item.priceMarket && item.priceMarket > (item.priceSale || 0)">
            ¥{{ item.priceMarket.toFixed(2) }}
          </span>
        </div>
        <el-tag size="small" :type="sourceType" effect="plain" class="source-tag">
          {{ sourceLabel }}
        </el-tag>
      </div>
    </div>
  </el-card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { SkuSearchItem } from '@/api/search'

const props = defineProps<{ item: SkuSearchItem }>()

const scoreType = computed(() => {
  if (props.item.score >= 0.8) return 'success'
  if (props.item.score >= 0.6) return 'warning'
  return 'info'
})

const sourceType = computed(() => {
  const s = props.item.source
  if (s === 'vector')  return 'primary'
  if (s === 'keyword') return 'success'
  return 'info'
})

const sourceLabel = computed(() => {
  const s = props.item.source
  if (s === 'vector')  return '语义'
  if (s === 'keyword') return '关键词'
  return '融合'
})
</script>

<style scoped>
.sku-card {
  cursor: pointer;
  transition: transform 0.2s;
  border-radius: 8px;
  overflow: hidden;
}
.sku-card:hover { transform: translateY(-4px); }

.img-wrap {
  position: relative;
  width: 100%;
  padding-top: 100%;
  background: #f5f7fa;
}
.img-placeholder {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  background: linear-gradient(135deg, #f5f7fa 0%, #e4e7ed 100%);
}
.goods-type {
  font-size: 11px;
  color: #909399;
  max-width: 80%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-badge {
  position: absolute;
  top: 8px; right: 8px;
  opacity: 0.9;
}
.status-badge {
  position: absolute;
  top: 8px; left: 8px;
}

.sku-info {
  padding: 10px 12px 12px;
  display: flex;
  flex-direction: column;
  gap: 5px;
}
.sku-name {
  margin: 0;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  color: #303133;
}
.sku-spec {
  margin: 0;
  font-size: 11px;
  color: #909399;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sku-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}
.sku-id {
  font-size: 10px;
  color: #c0c4cc;
}

.sku-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 2px;
}
.price-group {
  display: flex;
  align-items: baseline;
  gap: 6px;
}
.price-sale {
  font-size: 16px;
  font-weight: 700;
  color: #f56c6c;
}
.price-market {
  font-size: 11px;
  color: #c0c4cc;
  text-decoration: line-through;
}
.source-tag { font-size: 10px; }
</style>
