import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Test configuration lives in vitest.config.ts rather than here: vitest ships its own nested
// copy of vite, and mixing the two `defineConfig` types in one file makes `tsc -b` fail on the
// plugin array. vitest.config.ts re-exports this config and adds the `test` block.
// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
})
