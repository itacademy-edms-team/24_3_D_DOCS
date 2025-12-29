<template>
  <span
    :role="ariaRole"
    :aria-label="ariaLabel"
    class="icon"
    v-html="svgContent"
    :style="{ width: size + 'px', height: size + 'px', color }"
  />
</template>

<script setup>
import { computed } from 'vue'
import send from '../assets/icons/chat/send.svg?raw'
import microphone from '../assets/icons/chat/microphone.svg?raw'
import stop from '../assets/icons/chat/stop.svg?raw'
import retry from '../assets/icons/chat/retry.svg?raw'
import assistant from '../assets/icons/chat/assistant.svg?raw'
import user from '../assets/icons/chat/user.svg?raw'
import attachment from '../assets/icons/chat/attachment.svg?raw'
import settings from '../assets/icons/chat/settings.svg?raw'
import more_vert from '../assets/icons/chat/more_vert.svg?raw'
import close from '../assets/icons/chat/close.svg?raw'
import plus from '../assets/icons/chat/plus.svg?raw'
import copy from '../assets/icons/chat/copy.svg?raw'
import trash from '../assets/icons/chat/trash.svg?raw'
import arrow_back from '../assets/icons/toolbar/arrow_back.svg?raw'
import cursor from '../assets/icons/toolbar/cursor.svg?raw'
import text_fields from '../assets/icons/toolbar/text_fields.svg?raw'
import code from '../assets/icons/toolbar/code.svg?raw'
import horizontal_rule from '../assets/icons/toolbar/horizontal_rule.svg?raw'
import grid_on from '../assets/icons/toolbar/grid_on.svg?raw'
import undo from '../assets/icons/toolbar/undo.svg?raw'
import redo from '../assets/icons/toolbar/redo.svg?raw'
import content_copy from '../assets/icons/toolbar/content_copy.svg?raw'
import select_all from '../assets/icons/toolbar/select_all.svg?raw'
import save from '../assets/icons/toolbar/save.svg?raw'
import paste from '../assets/icons/toolbar/paste.svg?raw'
import cut from '../assets/icons/toolbar/cut.svg?raw'
import palette from '../assets/icons/toolbar/palette.svg?raw'
import description from '../assets/icons/toolbar/description.svg?raw'
import assignment from '../assets/icons/toolbar/assignment.svg?raw'
import archive from '../assets/icons/toolbar/archive.svg?raw'
import search from '../assets/icons/toolbar/search.svg?raw'
import folder_open from '../assets/icons/toolbar/folder_open.svg?raw'
import info from '../assets/icons/toolbar/info.svg?raw'
import arrow_upward from '../assets/icons/toolbar/arrow_upward.svg?raw'
import arrow_downward from '../assets/icons/toolbar/arrow_downward.svg?raw'
import sun from '../assets/icons/toolbar/sun.svg?raw'
import moon from '../assets/icons/toolbar/moon.svg?raw'
import logout from '../assets/icons/toolbar/logout.svg?raw'

const props = defineProps({
  name: { type: String, required: true },
  size: { type: Number, default: 20 },
  color: { type: String, default: 'currentColor' },
  ariaLabel: { type: String, default: '' },
  decorative: { type: Boolean, default: false }
})

const icons = {
  send,
  microphone,
  stop,
  retry,
  assistant,
  user,
  attachment,
  settings,
  more_vert,
  close,
  plus,
  copy,
  trash,
  arrow_back,
  cursor,
  text_fields,
  code,
  horizontal_rule,
  grid_on,
  undo,
  redo,
  content_copy,
  select_all,
  save,
  paste,
  cut,
  palette,
  description,
  assignment,
  archive,
  search,
  folder_open,
  info,
  arrow_upward,
  arrow_downward,
  sun,
  moon,
  logout
}

const svgContent = computed(() => icons[props.name] || '')
const ariaRole = computed(() => (props.decorative ? 'img' : 'img'))
</script>

<style scoped>
.icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.icon svg {
  width: 100%;
  height: 100%;
  display: block;
  fill: currentColor;
  stroke: currentColor;
}
</style>

