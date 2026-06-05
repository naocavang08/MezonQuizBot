import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    allowedHosts: ['.ngrok-free.app'],
    proxy: {
      '/api': {
        target: 'https://mezonquiz-api-latest.onrender.com',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'https://mezonquiz-api-latest.onrender.com',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      '/uploads': {
        target: 'https://mezonquiz-api-latest.onrender.com',
        changeOrigin: true,
        secure: false,
      },
    },
  }
})
