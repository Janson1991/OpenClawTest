import { createApp } from 'vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import AIAnalysis from './views/AIAnalysis.vue'

const app = createApp(AIAnalysis)
app.use(ElementPlus)
app.mount('#app')
