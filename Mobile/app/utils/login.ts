import { Capacitor, registerPlugin } from '@capacitor/core'
const login = registerPlugin<{ open(): Promise<{ refreshToken: string }> }>('TopLoggerLogin')
export async function loginToken(): Promise<string> {
  if (!Capacitor.isNativePlatform()) throw new Error('Automatic sign-in is available in the Android app. Use token entry for browser development.')
  const { refreshToken } = await login.open()
  if (typeof refreshToken !== 'string') throw new Error('TopLogger did not return a connection. Try token entry instead.')
  return refreshToken
}
