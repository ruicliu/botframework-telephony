const path = require("path");

module.exports = {
  mode: "production",
  target: "node",
  module: {
    rules: [
      {
        test: /\.ts?$/,
        use: [
          {
            loader: "ts-loader",
            options: {
              transpileOnly: true,
            },
          },
        ],
      },
    ],
  },
  output: {
    path: path.resolve(__dirname, "lib"),
    filename: "index.js",
  },
  resolve: {
    extensions: [".ts", ".js"],
  },
  devtool: "source-map",
  stats: { warnings: false },
};
