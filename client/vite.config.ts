import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // /api isteklerini .NET API'ye yönlendir. Aynı origin olduğu için CORS ayarı gerekmez
    // ve auth cookie'si sorunsuz çalışır.
    proxy: {
      '/api': 'http://localhost:5080',
      '/scalar': 'http://localhost:5080',
      '/openapi': 'http://localhost:5080',
    },
  },
  build: {
    // Production build'i doğrudan API'nin wwwroot klasörüne yazılır; tek uygulama olarak deploy edilir.
    outDir: '../src/DotnetQuest.Api/wwwroot',
    emptyOutDir: true,
  },
})
