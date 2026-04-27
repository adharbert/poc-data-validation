import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig(({ mode }) => {
  // Load env files for the current mode — prefix '' loads ALL variables, not just VITE_*
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],

    resolve: {
      alias: { '@': path.resolve(__dirname, './src') },
    },

    css: {
      preprocessorOptions: {
        scss: {
          // Bootstrap 5.3 uses deprecated Sass APIs — silence until BS6 ships
          silenceDeprecations: ['import', 'global-builtin', 'color-functions', 'if-function'],
        },
      },
    },

    // Dev server: proxy /api to the .NET API.
    // DEV_API_TARGET is a server-side-only env var (no VITE_ prefix so it is NOT baked into the bundle).
    // Defaults to the local .NET HTTPS port.
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target:       env.DEV_API_TARGET || 'https://localhost:7017',
          changeOrigin: true,
          secure:       false, // allow self-signed dev cert
        },
      },
    },

    define: {
      // Make current mode available as a plain string constant
      __APP_ENV__: JSON.stringify(mode),
    },

    build: {
      sourcemap: mode !== 'production',
    },
  }
})
