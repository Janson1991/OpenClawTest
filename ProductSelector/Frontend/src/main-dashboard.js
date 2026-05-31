import { createApp } from 'vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import Dashboard from './views/Dashboard.vue'

const app = createApp(Dashboard)
app.use(ElementPlus)
app.mount('#app')
