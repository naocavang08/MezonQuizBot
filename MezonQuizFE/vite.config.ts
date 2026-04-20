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
        target: 'https://0.0.0.0:7086',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'https://0.0.0.0:7086',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      '/uploads': {
        target: 'https://0.0.0.0:7086',
        changeOrigin: true,
        secure: false,
      },
    },
  }
})
