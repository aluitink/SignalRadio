import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Simple dev server config with an /api proxy to the backend
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // forward /api to the SignalRadio API running on localhost:5000
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
