<template>
  <el-container>
    <el-header>
      <div class="header-content">
        <h1>数据仪表盘</h1>
        <div class="header-buttons">
          <el-button @click="refreshData" :loading="refreshing">刷新数据</el-button>
          <el-button @click="exportData" type="primary">导出数据</el-button>
          <el-button @click="goBack">返回商品列表</el-button>
        </div>
      </div>
    </el-header>
    
    <el-main>
      <!-- 统计卡片 -->
      <el-row :gutter="20" class="stats-row">
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-content">
              <div class="stat-number">{{ stats.totalProducts }}</div>
              <div class="stat-label">商品总数</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-content">
              <div class="stat-number">{{ stats.todayNew }}</div>
              <div class="stat-label">今日新增</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-content">
              <div class="stat-number">{{ stats.monitoring }}</div>
              <div class="stat-label">监控中</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-content">
              <div class="stat-number">{{ stats.avgPrice }}</div>
              <div class="stat-label">平均价格</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
      
      <!-- 图表区域 -->
      <el-row :gutter="20">
        <el-col :span="12">
          <el-card class="chart-card">
            <template #header>
              <span>价格分布</span>
            </template>
            <div class="chart-placeholder">
              <div class="bar-chart">
                <div v-for="(item, index) in priceDistribution" :key="index" class="bar-item">
                  <div class="bar-label">{{ item.range }}</div>
                  <div class="bar-container">
                    <div class="bar-fill" :style="{ width: item.percentage + '%' }"></div>
                  </div>
                  <div class="bar-value">{{ item.count }} ({{ item.percentage.toFixed(1) }}%)</div>
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="12">
          <el-card class="chart-card">
            <template #header>
              <span>平台分布</span>
            </template>
            <div class="chart-placeholder">
              <div class="pie-chart">
                <div v-for="(item, index) in platformDistribution" :key="index" class="pie-item">
                  <div class="pie-color" :style="{ backgroundColor: item.color }"></div>
                  <div class="pie-label">{{ item.platform }}</div>
                  <div class="pie-value">{{ item.count }} ({{ item.percentage.toFixed(1) }}%)</div>
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
      
      <el-row :gutter="20">
        <el-col :span="12">
          <el-card class="chart-card">
            <template #header>
              <span>销量TOP10</span>
            </template>
            <div class="chart-placeholder">
              <div class="rank-list">
                <div v-for="(item, index) in topProducts" :key="index" class="rank-item">
                  <div class="rank-number" :class="'rank-' + (index + 1)">{{ index + 1 }}</div>
                  <div class="rank-info">
                    <div class="rank-name">{{ item.name }}</div>
                    <div class="rank-sales">销量: {{ item.salesCount.toLocaleString() }}</div>
                  </div>
                  <div class="rank-price">¥{{ item.price.toFixed(2) }}</div>
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="12">
          <el-card class="chart-card">
            <template #header>
              <span>价格趋势</span>
            </template>
            <div class="chart-placeholder">
              <div class="trend-chart">
                <div v-for="(item, index) in priceTrend" :key="index" class="trend-item">
                  <div class="trend-date">{{ item.date }}</div>
                  <div class="trend-bar">
                    <div class="trend-fill" :style="{ height: item.percentage + '%' }"></div>
                  </div>
                  <div class="trend-price">¥{{ item.price.toFixed(2) }}</div>
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
      
      <!-- 价格监控提醒 -->
      <el-row :gutter="20">
        <el-col :span="24">
          <el-card class="chart-card">
            <template #header>
              <span>价格监控提醒</span>
            </template>
            <el-table :data="alerts" style="width: 100%">
              <el-table-column label="商品" min-width="200">
                <template #default="{ row }">
                  <span>{{ row.product?.name || '未知商品' }}</span>
                </template>
              </el-table-column>
              <el-table-column label="当前价格" width="120">
                <template #default="{ row }">
                  <span class="price">¥{{ row.product?.price.toFixed(2) || '0.00' }}</span>
                </template>
              </el-table-column>
              <el-table-column label="目标价格" width="120">
                <template #default="{ row }">
                  <span v-if="row.targetPrice">¥{{ row.targetPrice.toFixed(2) }}</span>
                  <span v-else>-</span>
                </template>
              </el-table-column>
              <el-table-column label="触发次数" width="100">
                <template #default="{ row }">
                  {{ row.triggerCount }}
                </template>
              </el-table-column>
              <el-table-column label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.isActive ? 'success' : 'info'">
                    {{ row.isActive ? '监控中' : '已停止' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="150">
                <template #default="{ row }">
                  <el-button size="small" @click="viewAlert(row)">查看</el-button>
                  <el-button size="small" type="danger" @click="deleteAlert(row.id)">删除</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </el-col>
      </el-row>
    </el-main>
  </el-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import axios from 'axios'

const router = useRouter()
const refreshing = ref(false)

const stats = ref({
  totalProducts: 0,
  todayNew: 0,
  monitoring: 0,
  avgPrice: 0
})

const priceDistribution = ref([])
const platformDistribution = ref([])
const topProducts = ref([])
const priceTrend = ref([])
const alerts = ref([])

const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399']

const fetchDashboardData = async () => {
  try {
    // 获取商品数据
    const productsResponse = await axios.get('/api/products')
    const products = productsResponse.data
    
    // 计算统计数据
    stats.value = {
      totalProducts: products.length,
      todayNew: products.filter(p => {
        const today = new Date()
        const created = new Date(p.createdAt)
        return created.toDateString() === today.toDateString()
      }).length,
      monitoring: 0, // 需要从监控接口获取
      avgPrice: products.length > 0 ? products.reduce((sum, p) => sum + p.price, 0) / products.length : 0
    }
    
    // 计算价格分布
    const priceRanges = [
      { range: '0-50元', min: 0, max: 50 },
      { range: '50-100元', min: 50, max: 100 },
      { range: '100-200元', min: 100, max: 200 },
      { range: '200-500元', min: 200, max: 500 },
      { range: '500元以上', min: 500, max: Infinity }
    ]
    
    priceDistribution.value = priceRanges.map(range => ({
      range: range.range,
      count: products.filter(p => p.price >= range.min && p.price < range.max).length,
      percentage: 0
    }))
    
    const total = products.length || 1
    priceDistribution.value.forEach(item => {
      item.percentage = (item.count / total) * 100
    })
    
    // 计算平台分布
    const platforms = ['1688', 'Temu', 'TikTok', '其他']
    platformDistribution.value = platforms.map((platform, index) => ({
      platform,
      count: products.filter(p => p.platform === platform).length,
      percentage: 0,
      color: colors[index % colors.length]
    }))
    
    platformDistribution.value.forEach(item => {
      item.percentage = (item.count / total) * 100
    })
    
    // 获取销量TOP10
    topProducts.value = products
      .sort((a, b) => b.salesCount - a.salesCount)
      .slice(0, 10)
    
    // 模拟价格趋势数据
    const trendData = []
    for (let i = 6; i >= 0; i--) {
      const date = new Date()
      date.setDate(date.getDate() - i)
      trendData.push({
        date: date.toLocaleDateString('zh-CN'),
        price: 50 + Math.random() * 100,
        percentage: 30 + Math.random() * 70
      })
    }
    priceTrend.value = trendData
    
    // 获取监控提醒
    const alertsResponse = await axios.get('/api/pricemonitor/alerts/default-user')
    alerts.value = alertsResponse.data
    
  } catch (error) {
    console.error('获取仪表盘数据失败:', error)
    ElMessage.error('获取数据失败')
  }
}

const refreshData = async () => {
  refreshing.value = true
  await fetchDashboardData()
  refreshing.value = false
  ElMessage.success('数据已刷新')
}

const exportData = async () => {
  try {
    const response = await axios.get('/api/products')
    const products = response.data
    
    // 转换为CSV格式
    const headers = ['ID', '名称', '价格', '原价', '销量', '评分', '平台', '分类', '店铺']
    const rows = products.map(p => [
      p.id,
      `"${p.name}"`,
      p.price,
      p.originalPrice || '',
      p.salesCount,
      p.rating,
      p.platform || '',
      p.category || '',
      `"${p.shopName || ''}"`
    ])
    
    const csvContent = [headers.join(','), ...rows.map(r => r.join(','))].join('\n')
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `产品数据_${new Date().toISOString().slice(0, 10)}.csv`
    link.click()
    
    ElMessage.success('数据导出成功')
  } catch (error) {
    ElMessage.error('导出失败')
  }
}

const viewAlert = (alert) => {
  ElMessage.info(`查看监控提醒: ${alert.product?.name || '未知商品'}`)
}

const deleteAlert = async (alertId) => {
  try {
    await ElMessageBox.confirm('确定要删除这个监控提醒吗？', '确认删除', {
      type: 'warning'
    })
    
    await axios.delete(`/api/pricemonitor/alerts/${alertId}`)
    ElMessage.success('删除成功')
    await fetchDashboardData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const goBack = () => {
  router.push('/')
}

onMounted(() => {
  fetchDashboardData()
})
</script>

<style scoped>
.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
}

.header-buttons {
  display: flex;
  gap: 10px;
}

.stats-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
}

.stat-content {
  padding: 20px;
}

.stat-number {
  font-size: 32px;
  font-weight: bold;
  color: #409EFF;
  margin-bottom: 10px;
}

.stat-label {
  color: #909399;
  font-size: 14px;
}

.chart-card {
  margin-bottom: 20px;
}

.chart-placeholder {
  min-height: 300px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 柱状图样式 */
.bar-chart {
  width: 100%;
  padding: 20px;
}

.bar-item {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
}

.bar-label {
  width: 100px;
  text-align: right;
  padding-right: 10px;
  color: #606266;
}

.bar-container {
  flex: 1;
  height: 30px;
  background-color: #f5f7fa;
  border-radius: 4px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background-color: #409EFF;
  transition: width 0.3s;
}

.bar-value {
  width: 120px;
  padding-left: 10px;
  color: #606266;
  font-size: 14px;
}

/* 饼图样式 */
.pie-chart {
  width: 100%;
  padding: 20px;
}

.pie-item {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
}

.pie-color {
  width: 20px;
  height: 20px;
  border-radius: 4px;
  margin-right: 10px;
}

.pie-label {
  width: 100px;
  color: #606266;
}

.pie-value {
  color: #909399;
  font-size: 14px;
}

/* 排行榜样式 */
.rank-list {
  width: 100%;
  padding: 20px;
}

.rank-item {
  display: flex;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px solid #ebeef5;
}

.rank-number {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: bold;
  margin-right: 15px;
}

.rank-1 { background-color: #F56C6C; }
.rank-2 { background-color: #E6A23C; }
.rank-3 { background-color: #409EFF; }
.rank-4, .rank-5, .rank-6, .rank-7, .rank-8, .rank-9, .rank-10 { 
  background-color: #909399; 
}

.rank-info {
  flex: 1;
}

.rank-name {
  color: #303133;
  margin-bottom: 5px;
}

.rank-sales {
  color: #909399;
  font-size: 12px;
}

.rank-price {
  color: #F56C6C;
  font-weight: bold;
}

/* 趋势图样式 */
.trend-chart {
  width: 100%;
  height: 300px;
  display: flex;
  align-items: flex-end;
  justify-content: space-around;
  padding: 20px;
}

.trend-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
}

.trend-date {
  color: #909399;
  font-size: 12px;
  margin-bottom: 10px;
}

.trend-bar {
  width: 40px;
  height: 200px;
  background-color: #f5f7fa;
  border-radius: 4px;
  display: flex;
  align-items: flex-end;
}

.trend-fill {
  width: 100%;
  background-color: #67C23A;
  border-radius: 4px;
  transition: height 0.3s;
}

.trend-price {
  color: #606266;
  font-size: 12px;
  margin-top: 10px;
}

.price {
  color: #F56C6C;
  font-weight: bold;
}
</style>
