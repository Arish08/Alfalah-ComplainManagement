import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { api, DEV_USERS, getDevUserId, setDevUserId } from '../api/client.js'

const DevUserContext = createContext(null)

export function DevUserProvider({ children }) {
  const [userId, setUserId] = useState(getDevUserId())
  const [me, setMe] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const refreshMe = async () => {
    setLoading(true)
    setError('')
    try {
      setMe(await api('/me'))
    } catch (err) {
      setMe(null)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    refreshMe()
  }, [userId])

  const switchUser = (nextUserId) => {
    setDevUserId(nextUserId)
    setUserId(nextUserId)
  }

  const roles = useMemo(
    () => new Set((me?.memberships || []).map((x) => x.roleCode)),
    [me],
  )

  return (
    <DevUserContext.Provider value={{
      userId,
      me,
      roles,
      loading,
      error,
      devUsers: DEV_USERS,
      switchUser,
      refreshMe,
    }}>
      {children}
    </DevUserContext.Provider>
  )
}

export function useDevUser() {
  const value = useContext(DevUserContext)
  if (!value) throw new Error('useDevUser must be used inside DevUserProvider')
  return value
}
