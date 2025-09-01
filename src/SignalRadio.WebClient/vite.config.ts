import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Simple dev server config with an /api proxy to the backend
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    // Ensure the dev server sends no-cache headers so browsers don't hold
    // onto stale copies while developing.
    headers: {
      'Cache-Control': 'no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0',
      'Pragma': 'no-cache',
      'Expires': '0',
    },
    proxy: {
      // Mirror production nginx routes:
      // - /api/ -> backend /api/
      // Hubs are exposed at /hubs/ in production; dev server should proxy
      // websocket upgrades from /hubs (no trailing slash used by the dev client)
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },

      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
        secure: false,
      },
    },
  },
})
