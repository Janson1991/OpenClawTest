<template>
  <el-card class="sku-card" shadow="hover" :body-style="{ padding: '0' }">
    <!-- 商品图片 -->
    <div class="img-wrap">
      <el-image
        :src="item.imageUrl || defaultImg"
        :alt="item.name"
        fit="cover"
        lazy
        class="sku-img"
      >
        <template #error>
          <div class="img-placeholder">
            <el-icon size="32" color="#dcdfe6"><Picture /></el-icon>
          </div>
        </template>
      </el-image>

      <!-- 相似度得分徽标 -->
      <el-tag
        class="score-badge"
        size="small"
        :type="scoreType"
        effect="dark"
      >
        {{ (item.score * 100).toFixed(0) }}%
      </el-tag>
    </div>

    <!-- 商品信息 -->
    <div class="sku-info">
      <p class="sku-name" :title="item.name">{{ item.name }}</p>

      <div class="sku-meta">
        <el-tag v-if="item.category" size="small" type="info">
          {{ item.category }}
        </el-tag>
        <span v-if="item.brand" class="brand">{{ item.brand }}</span>
      </div>

      <div class="sku-footer">
        <span class="price">¥{{ item.price.toFixed(2) }}</span>
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

const defaultImg = 'https://via.placeholder.com/200x200?text=No+Image'

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
.sku-img {
  position: absolute;
  top: 0; left: 0;
  width: 100%; height: 100%;
}
.img-placeholder {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
}

.score-badge {
  position: absolute;
  top: 8px; right: 8px;
  opacity: 0.9;
}

.sku-info {
  padding: 10px 12px 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
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
.sku-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}
.brand { font-size: 11px; color: #909399; }

.sku-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 2px;
}
.price {
  font-size: 16px;
  font-weight: 700;
  color: #f56c6c;
}
.source-tag { font-size: 10px; }
</style>
