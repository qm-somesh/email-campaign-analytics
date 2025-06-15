const path = require('path');

module.exports = function override(config, env) {
  // Add fallbacks for Node.js modules that aren't available in browser
  config.resolve.fallback = {
    ...config.resolve.fallback,
    "http": require.resolve("stream-http"),
    "https": require.resolve("https-browserify"),
    "stream": require.resolve("stream-browserify"),
    "util": require.resolve("util"),
    "url": require.resolve("url"),
    "buffer": require.resolve("buffer"),
    "crypto": require.resolve("crypto-browserify"),
    "zlib": require.resolve("browserify-zlib"),
    "querystring": require.resolve("querystring-es3"),
    "path": require.resolve("path-browserify"),
    "process": require.resolve("process/browser.js"),
    "fs": false,
    "net": false,
    "tls": false
  };

  // Add plugins for global polyfills
  const webpack = require('webpack');
  config.plugins = [
    ...config.plugins,
    new webpack.ProvidePlugin({
      Buffer: ['buffer', 'Buffer'],
      process: 'process/browser.js',
    }),
  ];

  return config;
};
