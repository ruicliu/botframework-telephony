const childProcess = require("child_process");

require("esbuild")
  .build({
    entryPoints: ["./src/index.ts"],
    outdir: "lib",
    bundle: true,
    sourcemap: true,
    platform: "node",
    logLevel: "error",
    watch: {
      onRebuild(error, result) {
        if (error) console.error("watch build failed:", error);
        else childProcess.fork("./lib/index.js");
      },
    },
  })
  .then((result) => {
    result.stop();
  });
