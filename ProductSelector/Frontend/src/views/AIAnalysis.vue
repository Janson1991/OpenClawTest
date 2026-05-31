<template>
  <el-container>
    <el-header>
      <div class="header-content">
        <h1>AI 选品分析</h1>
        <el-button @click="goBack">返回商品列表</el-button>
      </div>
    </el-header>
    
    <el-main>
      <el-row :gutter="20">
        <!-- 左侧分析面板 -->
        <el-col :span="12">
          <el-card class="analysis-card">
            <template #header>
              <span>商品分析</span>
            </template>
            
            <el-form label-position="top">
              <el-form-item label="选择商品">
                <el-select v-model="selectedProductId" placeholder="选择要分析的商品" style="width: 100%">
                  <el-option
                    v-for="product in products"
                    :key="product.id"
                    :label="product.name"
                    :value="product.id"
                  />
                </el-select>
              </el-form-item>
              
              <el-form-item>
                <el-button type="primary" @click="analyzeProduct" :loading="analyzing">
                  开始分析
                </el-button>
              </el-form-item>
            </el-form>
            
            <div v-if="analysisResult" class="analysis-result">
              <pre>{{ analysisResult }}</pre>
            </div>
          </el-card>
          
          <el-card class="analysis-card">
            <template #header>
              <span>商品对比</span>
            </template>
            
            <el-form label-position="top">
              <el-form-item label="商品1">
                <el-select v-model="compareProduct1Id" placeholder="选择商品1" style="width: 100%">
                  <el-option
                    v-for="product in products"
                    :key="product.id"
                    :label="product.name"
                    :value="product.id"
                  />
                </el-select>
              </el-form-item>
              
              <el-form-item label="商品2">
                <el-select v-model="compareProduct2Id" placeholder="选择商品2" style="width: 100%">
                  <el-option
                    v-for="product in products"
                    :key="product.id"
                    :label="product.name"
                    :value="product.id"
                  />
                </el-select>
              </el-form-item>
              
              <el-form-item>
                <el-button type="primary" @click="compareProducts" :loading="comparing">
                  开始对比
                </el-button>
              </el-form-item>
            </el-form>
            
            <div v-if="comparisonResult" class="comparison-result">
              <h3>{{ comparisonResult.product1Name }} vs {{ comparisonResult.product2Name }}</h3>
              <p><strong>价格差异：</strong>¥{{ comparisonResult.priceDifference.toFixed(2) }}</p>
              <p><strong>销量差异：</strong>{{ comparisonResult.salesDifference.toLocaleString() }}</p>
              <p><strong>评分差异：</strong>{{ comparisonResult.ratingDifference.toFixed(2) }}</p>
              <p><strong>推荐商品：</strong>{{ comparisonResult.winner }}</p>
              <pre>{{ comparisonResult.analysis }}</pre>
            </div>
          </el-card>
        </el-col>
        
        <!-- 右侧推荐面板 -->
        <el-col :span="12">
          <el-card class="analysis-card">
            <template #header>
              <span>商品推荐</span>
            </template>
            
            <el-button @click="loadRecommendations" :loading="loadingRecommendations" style="margin-bottom: 20px">
              刷新推荐
            </el-button>
            
            <div v-if="recommendations.length > 0" class="recommendations-list">
              <div v-for="rec in recommendations" :key="rec.productId" class="recommendation-item">
                <div class="rec-header">
                  <span class="rec-name">{{ rec.productName }}</span>
                  <el-tag :type="getScoreType(rec.score)">{{ rec.score.toFixed(1) }}</el-tag>
                </div>
                <div class="rec-action">
                  <el-tag :type="getActionType(rec.action)">{{ rec.action }}</el-tag>
                </div>
                <div class="rec-reasons">
                  <span v-for="(reason, index) in rec.reasons" :key="index" class="reason-tag">
                    {{ reason }}
                  </span>
                </div>
              </div>
            </div>
            
            <el-empty v-else description="暂无推荐数据" />
          </el-card>
          
          <el-card class="analysis-card">
            <template #header>
              <span>市场报告</span>
            </template>
            
            <el-button @click="generateReport" :loading="generatingReport" style="margin-bottom: 20px">
              生成报告
            </el-button>
            
            <div v-if="marketReport" class="market-report">
              <pre>{{ marketReport }}</pre>
            </div>
          </el-card>
          
          <el-card class="analysis-card">
            <template #header>
              <span>分类分析</span>
            </template>
            
            <el-button @click="loadCategoryAnalysis" :loading="loadingCategoryAnalysis" style="margin-bottom: 20px">
              刷新分析
            </el-button>
            
            <div v-if="categoryAnalysis.length > 0" class="category-analysis">
              <div v-for="category in categoryAnalysis" :key="category.category" class="category-item">
                <h4>{{ category.category }}</h4>
                <p>商品数: {{ category.count }}</p>
                <p>平均价格: ¥{{ category.averagePrice.toFixed(2) }}</p>
                <p>平均销量: {{ category.averageSales.toLocaleString() }}</p>
                <p>平均评分: {{ category.averageRating.toFixed(2) }}</p>
                <p>热销商品: {{ category.topProduct }}</p>
              </div>
            </div>
            
            <el-empty v-else description="暂无分类数据" />
          </el-card>
        </el-col>
      </el-row>
    </el-main>
  </el-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const router = useRouter()

// 商品列表
const products = ref([])
const selectedProductId = ref(null)
const analyzing = ref(false)
const analysisResult = ref('')

// 商品对比
const compareProduct1Id = ref(null)
const compareProduct2Id = ref(null)
const comparing = ref(false)
const comparisonResult = ref(null)

// 推荐
const loadingRecommendations = ref(false)
const recommendations = ref([])

// 市场报告
const generatingReport = ref(false)
const marketReport = ref('')

// 分类分析
const loadingCategoryAnalysis = ref(false)
const categoryAnalysis = ref([])

const fetchProducts = async () => {
  try {
    const response = await axios.get('/api/products')
    products.value = response.data
  } catch (error) {
    ElMessage.error('获取商品列表失败')
  }
}

const analyzeProduct = async () => {
  if (!selectedProductId.value) {
    ElMessage.warning('请先选择商品')
    return
  }
  
  analyzing.value = true
  try {
    const response = await axios.post(`/api/aianalysis/analyze/${selectedProductId.value}`)
    analysisResult.value = response.data
  } catch (error) {
    ElMessage.error('分析失败')
  } finally {
    analyzing.value = false
  }
}

const compareProducts = async () => {
  if (!compareProduct1Id.value || !compareProduct2Id.value) {
    ElMessage.warning('请选择两个商品')
    return
  }
  
  comparing.value = true
  try {
    const response = await axios.post('/api/aianalysis/compare', null, {
      params: {
        product1Id: compareProduct1Id.value,
        product2Id: compareProduct2Id.value
      }
    })
    comparisonResult.value = response.data
  } catch (error) {
    ElMessage.error('对比失败')
  } finally {
    comparing.value = false
  }
}

const loadRecommendations = async () => {
  loadingRecommendations.value = true
  try {
    const response = await axios.get('/api/aianalysis/recommendations')
    recommendations.value = response.data
  } catch (error) {
    ElMessage.error('获取推荐失败')
  } finally {
    loadingRecommendations.value = false
  }
}

const generateReport = async () => {
  generatingReport.value = true
  try {
    const response = await axios.get('/api/aianalysis/market-report')
    marketReport.value = response.data
  } catch (error) {
    ElMessage.error('生成报告失败')
  } finally {
    generatingReport.value = false
  }
}

const loadCategoryAnalysis = async () => {
  loadingCategoryAnalysis.value = true
  try {
    const response = await axios.get('/api/aianalysis/category-analysis')
    categoryAnalysis.value = response.data
  } catch (error) {
    ElMessage.error('获取分类分析失败')
  } finally {
    loadingCategoryAnalysis.value = false
  }
}

const goBack = () => {
  router.push('/')
}

const getScoreType = (score) => {
  if (score >= 80) return 'success'
  if (score >= 60) return 'warning'
  return 'danger'
}

const getActionType = (action) => {
  if (action === '立即上架') return 'success'
  if (action === '建议上架') return 'warning'
  return 'info'
}

onMounted(() => {
  fetchProducts()
  loadRecommendations()
  loadCategoryAnalysis()
})
</script>

<style scoped>
.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
}

.analysis-card {
  margin-bottom: 20px;
}

.analysis-result,
.comparison-result,
.market-report {
  margin-top: 20px;
  padding: 15px;
  background-color: #f5f7fa;
  border-radius: 4px;
  max-height: 400px;
  overflow-y: auto;
}

pre {
  white-space: pre-wrap;
  word-wrap: break-word;
  font-family: 'Courier New', Courier, monospace;
  font-size: 13px;
  line-height: 1.5;
}

.recommendations-list {
  max-height: 400px;
  overflow-y: auto;
}

.recommendation-item {
  padding: 15px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  margin-bottom: 10px;
}

.rec-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.rec-name {
  font-weight: bold;
}

.rec-action {
  margin-bottom: 10px;
}

.rec-reasons {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
}

.reason-tag {
  background-color: #f0f9ff;
  color: #409eff;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.category-analysis {
  max-height: 400px;
  overflow-y: auto;
}

.category-item {
  padding: 10px;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  margin-bottom: 10px;
}

.category-item h4 {
  margin: 0 0 10px 0;
  color: #409eff;
}

.category-item p {
  margin: 5px 0;
  color: #606266;
  font-size: 14px;
}
</style>
