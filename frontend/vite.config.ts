import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  // strictPort: if 5173 is busy, stop with an error instead of silently using another port
  // (the backend allows requests from http://localhost:5173, see backend/appsettings.json -> Frontend:Url).
  server: { port: 5173, strictPort: true },
  // `npm test` runs the *.test.ts(x) files in a simulated browser (jsdom).
  test: { environment: "jsdom" },
});
