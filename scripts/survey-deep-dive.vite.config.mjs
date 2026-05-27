import { resolve } from "node:path";
import { defineConfig } from "vite";

export default defineConfig({
  build: {
    ssr: resolve("scripts/survey-deep-dive-extract.ts"),
    outDir: "tmp/survey-deep-dive-build",
    emptyOutDir: true,
    target: "node20",
    rollupOptions: {
      output: {
        entryFileNames: "survey-deep-dive-extract.mjs"
      }
    }
  }
});
