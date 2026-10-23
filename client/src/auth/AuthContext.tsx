import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, ApiError } from '../api/client'
import type { PlayerProfile } from '../api/types'
import { AuthContext } from './useAuth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [player, setPlayer] = useState<PlayerProfile | null>(null)
  const [loading, setLoading] = useState(true)

  // Açılışta cookie ile giriş yapılmış mı kontrol et (401 = misafir).
  useEffect(() => {
    const controller = new AbortController()
    api
      .getMe(controller.signal)
      .then(setPlayer, () => {
        if (!controller.signal.aborted) setPlayer(null)
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [])

  const refresh = useCallback(async () => {
    try {
      setPlayer(await api.getMe())
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) setPlayer(null)
      else throw error
    }
  }, [])

  const login = useCallback(
    async (email: string, password: string) => {
      await api.login(email, password)
      await refresh()
    },
    [refresh],
  )

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      await api.register(email, password)
      await api.login(email, password)
      await api.updateMe(displayName)
      await refresh()
    },
    [refresh],
  )

  const logout = useCallback(async () => {
    await api.logout()
    setPlayer(null)
  }, [])

  const value = useMemo(
    () => ({ player, loading, refresh, login, register, logout }),
    [player, loading, refresh, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
