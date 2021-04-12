const { build } = require("esbuild");
const { copyFile } = require("fs");
const outdir = "dist";

build({
  entryPoints: ["./src/index.js"],
  outdir,
  bundle: true,
  minify: true,
  platform: "node",
  logLevel: "error",
}).then(() => {
  try {
    copyFile("web.config", `${outdir}/web.config`, () => {});
    copyFile(".deployment", `${outdir}/.deployment`, () => {});
    copyFile("deploy.cmd", `${outdir}/deploy.cmd`, () => {});
  } catch (error) {
    console.log(error);
  }
});
