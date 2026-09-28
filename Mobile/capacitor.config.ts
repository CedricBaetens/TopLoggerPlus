import type { CapacitorConfig } from '@capacitor/cli'
const config: CapacitorConfig = {
  appId: 'com.toploggerplus.app', appName: 'TopLogger Plus', webDir: '.output/public',
  loggingBehavior: 'none',
  android: { backgroundColor: '#f5f6f3', allowMixedContent: false },
}
export default config
