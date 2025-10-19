import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");

  return {
    plugins: [react()],
    server: {
      port: Number(env.VITE_PORT ?? 5174),
      host: true,
      proxy: {
        "/api": env.VITE_API_PROXY
          ? { target: env.VITE_API_PROXY, changeOrigin: true }
          : undefined,
      },
    },
    build: {
      sourcemap: true,
    },
  };
});
