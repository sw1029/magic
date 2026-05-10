import { resolve } from "node:path";
import { defineConfig } from "vitest/config";

const surveyCsp =
  "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' http://localhost:* http://127.0.0.1:* ws://localhost:* ws://127.0.0.1:*; img-src 'self' data:; media-src 'none'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";

export default defineConfig({
  server: {
    headers: {
      "Content-Security-Policy": surveyCsp
    }
  },
  preview: {
    headers: {
      "Content-Security-Policy": surveyCsp
    }
  },
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, "index.html"),
        survey: resolve(__dirname, "survey/magic-symbol-tutorial/index.html")
      }
    }
  },
  test: {
    environment: "node",
    include: ["tests/**/*.test.ts"]
  }
});
