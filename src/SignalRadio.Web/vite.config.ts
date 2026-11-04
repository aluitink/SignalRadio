import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies /api and /hubs to the backend (mirrors production nginx).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    headers: {
      'Cache-Control': 'no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0',
      'Pragma': 'no-cache',
      'Expires': '0',
    },
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        ws: true,
        secure: false,
      },
    },
  },
})
