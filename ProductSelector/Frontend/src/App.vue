<template>
  <el-container>
    <el-header>
      <div class="header-content">
        <h1>选品监控工具</h1>
        <el-input
          v-model="searchKeyword"
          placeholder="输入关键词搜索商品"
          class="search-input"
          @keyup.enter="handleSearch"
        >
          <template #append>
            <el-button @click="handleSearch" :loading="searching">
              搜索
            </el-button>
          </template>
        </el-input>
      </div>
    </el-header>
    
    <el-main>
      <el-row :gutter="20">
        <el-col :span="6">
          <el-card class="filter-card">
            <template #header>
              <span>筛选条件</span>
            </template>
            
            <el-form label-position="top">
              <el-form-item label="平台">
                <el-select v-model="filters.platform" placeholder="选择平台" clearable>
                  <el-option label="1688" value="1688" />
                  <el-option label="Temu" value="Temu" />
                  <el-option label="TikTok" value="TikTok" />
                </el-select>
              </el-form-item>
              
              <el-form-item label="分类">
                <el-input v-model="filters.category" placeholder="输入分类" />
              </el-form-item>
              
              <el-form-item>
                <el-button type="primary" @click="applyFilters">
                  应用筛选
                </el-button>
                <el-button @click="resetFilters">
                  重置
                </el-button>
              </el-form-item>
            </el-form>
          </el-card>
          
          <el-card class="stats-card">
            <template #header>
              <span>数据统计</span>
            </template>
            
            <div class="stats-item">
              <span>商品总数：</span>
              <span>{{ stats.totalProducts }}</span>
            </div>
            <div class="stats-item">
              <span>今日新增：</span>
              <span>{{ stats.todayNew }}</span>
            </div>
            <div class="stats-item">
              <span>监控中：</span>
              <span>{{ stats.monitoring }}</span>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="18">
          <el-card>
            <template #header>
              <div class="card-header">
                <span>商品列表</span>
                <el-button type="success" @click="scrapeProducts" :loading="scraping">
                  抓取数据
                </el-button>
              </div>
            </template>
            
            <el-table
              :data="products"
              style="width: 100%"
              v-loading="loading"
            >
              <el-table-column label="商品信息" min-width="300">
                <template #default="{ row }">
                  <div class="product-info">
                    <img v-if="row.imageUrl" :src="row.imageUrl" class="product-image" />
                    <div class="product-details">
                      <div class="product-name">{{ row.name }}</div>
                      <div class="product-shop">{{ row.shopName }}</div>
                    </div>
                  </div>
                </template>
              </el-table-column>
              
              <el-table-column label="价格" width="120">
                <template #default="{ row }">
                  <span class="price">¥{{ row.price.toFixed(2) }}</span>
                  <span v-if="row.originalPrice" class="original-price">
                    ¥{{ row.originalPrice.toFixed(2) }}
                  </span>
                </template>
              </el-table-column>
              
              <el-table-column label="销量" width="100">
                <template #default="{ row }">
                  {{ formatNumber(row.salesCount) }}
                </template>
              </el-table-column>
              
              <el-table-column label="评分" width="80">
                <template #default="{ row }">
                  <el-rate
                    v-model="row.rating"
                    disabled
                    show-score
                    text-color="#ff9900"
                    score-template="{value}"
                  />
                </template>
              </el-table-column>
              
              <el-table-column label="平台" width="100">
                <template #default="{ row }">
                  <el-tag :type="getPlatformType(row.platform)">
                    {{ row.platform }}
                  </el-tag>
                </template>
              </el-table-column>
              
              <el-table-column label="操作" width="150">
                <template #default="{ row }">
                  <el-button
                    size="small"
                    @click="viewProduct(row)"
                  >
                    查看
                  </el-button>
                  <el-button
                    size="small"
                    type="primary"
                    @click="addToMonitor(row)"
                  >
                    监控
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
            
            <el-pagination
              v-model:current-page="currentPage"
              :page-size="pageSize"
              :total="total"
              layout="total, prev, pager, next"
              @current-change="handlePageChange"
            />
          </el-card>
        </el-col>
      </el-row>
    </el-main>
  </el-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const searchKeyword = ref('')
const searching = ref(false)
const scraping = ref(false)
const loading = ref(false)
const products = ref([])
const currentPage = ref(1)
const pageSize = ref(20)
const total = ref(0)

const filters = ref({
  platform: '',
  category: ''
})

const stats = ref({
  totalProducts: 0,
  todayNew: 0,
  monitoring: 0
})

const fetchProducts = async () => {
  loading.value = true
  try {
    const response = await axios.get('/api/products', {
      params: {
        platform: filters.value.platform || undefined,
        category: filters.value.category || undefined,
        page: currentPage.value,
        pageSize: pageSize.value
      }
    })
    products.value = response.data
    total.value = response.data.length
  } catch (error) {
    ElMessage.error('获取商品列表失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = async () => {
  if (!searchKeyword.value.trim()) {
    ElMessage.warning('请输入搜索关键词')
    return
  }
  
  searching.value = true
  try {
    const response = await axios.get('/api/products/search', {
      params: { keyword: searchKeyword.value }
    })
    products.value = response.data
    total.value = response.data.length
  } catch (error) {
    ElMessage.error('搜索失败')
  } finally {
    searching.value = false
  }
}

const scrapeProducts = async () => {
  if (!searchKeyword.value.trim()) {
    ElMessage.warning('请先输入搜索关键词')
    return
  }
  
  scraping.value = true
  try {
    const response = await axios.post('/api/products/scrape', null, {
      params: {
        keyword: searchKeyword.value,
        page: currentPage.value
      }
    })
    products.value = response.data
    total.value = response.data.length
    ElMessage.success(`成功抓取 ${response.data.length} 个商品`)
  } catch (error) {
    ElMessage.error('抓取数据失败')
  } finally {
    scraping.value = false
  }
}

const applyFilters = () => {
  currentPage.value = 1
  fetchProducts()
}

const resetFilters = () => {
  filters.value = {
    platform: '',
    category: ''
  }
  currentPage.value = 1
  fetchProducts()
}

const handlePageChange = (page) => {
  currentPage.value = page
  fetchProducts()
}

const viewProduct = (product) => {
  if (product.sourceUrl) {
    window.open(product.sourceUrl, '_blank')
  }
}

const addToMonitor = (product) => {
  ElMessage.success('已添加到监控列表')
}

const formatNumber = (num) => {
  if (num >= 10000) {
    return (num / 10000).toFixed(1) + '万'
  }
  return num.toString()
}

const getPlatformType = (platform) => {
  const types = {
    '1688': 'danger',
    'Temu': 'warning',
    'TikTok': 'success'
  }
  return types[platform] || 'info'
}

onMounted(() => {
  fetchProducts()
})
</script>

<style>
.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
}

.search-input {
  width: 400px;
}

.filter-card,
.stats-card {
  margin-bottom: 20px;
}

.stats-item {
  display: flex;
  justify-content: space-between;
  margin-bottom: 10px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.product-info {
  display: flex;
  align-items: center;
}

.product-image {
  width: 50px;
  height: 50px;
  object-fit: cover;
  margin-right: 10px;
  border-radius: 4px;
}

.product-details {
  flex: 1;
}

.product-name {
  font-weight: bold;
  margin-bottom: 5px;
}

.product-shop {
  color: #666;
  font-size: 12px;
}

.price {
  color: #f56c6c;
  font-weight: bold;
}

.original-price {
  color: #999;
  text-decoration: line-through;
  font-size: 12px;
  margin-left: 5px;
}

.el-pagination {
  margin-top: 20px;
  justify-content: center;
}
</style>
