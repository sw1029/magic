import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

const surveyCsp =
  "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' http://localhost:* http://127.0.0.1:* ws://localhost:* ws://127.0.0.1:*; img-src 'self' data:; media-src 'none'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
const webUiCsp =
  "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' http://localhost:* http://127.0.0.1:* ws://localhost:* ws://127.0.0.1:*; img-src 'self' data:; media-src 'none'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";

function cspForRequestUrl(url = "/"): string {
  return url.startsWith("/survey/") ? surveyCsp : webUiCsp;
}

const __dirname = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  server: {
    headers: {
      "Content-Security-Policy": webUiCsp
    }
  },
  preview: {
    headers: {
      "Content-Security-Policy": webUiCsp
    }
  },
  plugins: [
    {
      name: "magic-csp-by-entry",
      configureServer(server) {
        server.middlewares.use((request, response, next) => {
          response.setHeader("Content-Security-Policy", cspForRequestUrl(request.url));
          next();
        });
      },
      configurePreviewServer(server) {
        server.middlewares.use((request, response, next) => {
          response.setHeader("Content-Security-Policy", cspForRequestUrl(request.url));
          next();
        });
      }
    }
  ],
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
