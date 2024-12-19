import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.wedge.apple',
  appName: 'Wedge',
  webDir: 'www',
  server: {
    url: 'http://192.168.1.11:8100',
    cleartext: true
  }
};

export default config;
