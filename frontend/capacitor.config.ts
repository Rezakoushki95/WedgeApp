const LIVERELOAD_URL = process.env["LIVERELOAD_URL"];

const config = {
  appId: 'com.example.app',
  appName: 'WedgeApp',
  webDir: 'www',
  server: process.env["LIVERELOAD"] === 'true' && LIVERELOAD_URL
    ? {
      url: LIVERELOAD_URL,
      cleartext: true,
    }
    : undefined,
};

export default config;