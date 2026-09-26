import { defineConfig } from 'vite' // Imports Vite's typed configuration helper.
export default defineConfig({ server: { proxy: { '/api': { target: 'https://localhost:7294', secure: false } } }, build: { outDir: '../WebApplication7/wwwroot', emptyOutDir: true } }) // Routes local API calls to ASP.NET and copies production assets into its static directory.
