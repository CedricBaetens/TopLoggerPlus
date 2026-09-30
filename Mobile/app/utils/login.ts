import { Capacitor, registerPlugin } from '@capacitor/core'
const login = registerPlugin<{ open(): Promise<{ refreshToken: string }> }>('TopLoggerLogin')
export async function loginToken(): Promise<string> {
  if (!Capacitor.isNativePlatform()) throw new Error('Sign in using the Android app.')
  const { refreshToken } = await login.open()
  if (typeof refreshToken !== 'string') throw new Error('TopLogger did not return a connection. Please sign in again.')
  return refreshToken
}
