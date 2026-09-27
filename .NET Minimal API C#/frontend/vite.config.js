import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// Vue dev server runs on http://localhost:5174 for the Minimal API edition
// (:5173 is kept by the controller-based app so both can run side-by-side).
// The frontend calls the API via the relative path /api/... which works both
// directly (:5174, proxied below) and through the NGINX entry point (:8081).
export default defineConfig({
  plugins: [vue()],
  server: {
    host: '127.0.0.1',
    port: 5174,

    allowedHosts: [
      'pedigree-silicon-levitate.ngrok-free.dev'
    ],

    proxy: {
      '/api': {
        target: 'http://localhost:5039',
        changeOrigin: true
      }
    }
  }
})