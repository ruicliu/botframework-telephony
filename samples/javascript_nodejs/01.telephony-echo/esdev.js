require("esbuild").build({
  entryPoints: ["./src/index.js"],
  outdir: "lib",
  bundle: true,
  sourcemap: true,
  platform: "node",
  logLevel: "error",
});
