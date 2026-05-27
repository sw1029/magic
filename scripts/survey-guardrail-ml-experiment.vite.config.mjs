import { resolve } from "node:path";
import { defineConfig } from "vite";

export default defineConfig({
  build: {
    ssr: resolve("scripts/survey-guardrail-ml-experiment.ts"),
    outDir: "tmp/survey-guardrail-ml-build",
    emptyOutDir: true,
    target: "node20",
    rollupOptions: {
      output: {
        entryFileNames: "survey-guardrail-ml-experiment.mjs"
      }
    }
  }
});
