require("esbuild").build({
  entryPoints: ["./src/index.ts"],
  outdir: "lib",
  bundle: true,
  sourcemap: true,
  platform: "node",
  logLevel: "error",
});
