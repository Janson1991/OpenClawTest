<template>
  <div class="search-bar">
    <el-input
      v-model="store.query"
      :placeholder="placeholder"
      size="large"
      clearable
      @keyup.enter="store.doSearch()"
      @clear="store.reset()"
    >
      <template #prefix>
        <el-icon><Search /></el-icon>
      </template>

      <template #append>
        <!-- 搜索按钮 -->
        <el-button type="primary" :loading="store.loading" @click="store.doSearch()">
          搜索
        </el-button>

        <!-- 语音输入按钮 -->
        <el-button
          :type="isRecording ? 'danger' : 'default'"
          :title="isRecording ? '录音中，松开结束' : '点击语音输入'"
          @mousedown="startVoice"
          @mouseup="stopVoice"
          @touchstart.prevent="startVoice"
          @touchend.prevent="stopVoice"
        >
          <el-icon>
            <component :is="isRecording ? 'VideoPause' : 'Microphone'" />
          </el-icon>
          {{ isRecording ? '松开' : '语音' }}
        </el-button>
      </template>
    </el-input>

    <!-- AI 意图解析结果 -->
    <Transition name="fade">
      <div v-if="store.parsedQuery && hasParsedInfo" class="parsed-info">
        <span class="label">AI 理解：</span>
        <el-tag
          v-for="kw in displayTags"
          :key="kw"
          size="small"
          type="info"
          class="tag"
        >{{ kw }}</el-tag>
        <span v-if="store.elapsedMs" class="elapsed">
          {{ store.elapsedMs }}ms
        </span>
      </div>
    </Transition>

    <!-- 语音状态提示 -->
    <Transition name="fade">
      <div v-if="isRecording" class="voice-hint">
        <el-icon class="pulse"><Microphone /></el-icon>
        正在录音…请说出搜索内容
      </div>
    </Transition>

    <!-- 错误提示 -->
    <el-alert
      v-if="store.error"
      :title="store.error"
      type="error"
      show-icon
      closable
      @close="store.error = ''"
      class="error-alert"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSearchStore } from '@/stores/search'
import { ElMessage } from 'element-plus'

const props = defineProps<{ placeholder?: string }>()
const store = useSearchStore()

const isRecording = ref(false)
let recognition: any = null

const hasParsedInfo = computed(() =>
  (store.parsedQuery?.keywords?.length ?? 0) > 0 ||
  (store.parsedQuery?.synonyms?.length ?? 0) > 0
)

const displayTags = computed(() => {
  const kws = store.parsedQuery?.keywords  ?? []
  const syn = store.parsedQuery?.synonyms  ?? []
  return [...kws, ...syn].slice(0, 10)
})

function startVoice() {
  const SpeechRecognition =
    (window as any).SpeechRecognition ||
    (window as any).webkitSpeechRecognition

  if (!SpeechRecognition) {
    ElMessage.warning('当前浏览器不支持语音识别，请使用 Chrome')
    return
  }

  recognition       = new SpeechRecognition()
  recognition.lang  = 'zh-CN'
  recognition.continuous    = false
  recognition.interimResults= false

  recognition.onresult = (e: any) => {
    const text = e.results[0][0].transcript
    store.query = text
    store.doSearch(text)
  }

  recognition.onerror = (e: any) => {
    ElMessage.error(`语音识别失败: ${e.error}`)
    isRecording.value = false
  }

  recognition.onend = () => { isRecording.value = false }

  recognition.start()
  isRecording.value = true
}

function stopVoice() {
  recognition?.stop()
}
</script>

<style scoped>
.search-bar { display: flex; flex-direction: column; gap: 8px; }

.parsed-info {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  font-size: 13px;
  color: #606266;
  padding: 4px 2px;
}
.parsed-info .label { color: #909399; font-size: 12px; }
.parsed-info .elapsed { margin-left: auto; color: #c0c4cc; font-size: 11px; }

.voice-hint {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #f56c6c;
  font-size: 13px;
  padding: 4px 2px;
}

.pulse { animation: pulse 1s infinite; }
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50%       { opacity: 0.3; }
}

.error-alert { margin-top: 4px; }

.fade-enter-active, .fade-leave-active { transition: opacity 0.3s; }
.fade-enter-from, .fade-leave-to       { opacity: 0; }
</style>
