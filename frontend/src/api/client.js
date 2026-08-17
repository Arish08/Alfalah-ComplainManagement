const API_BASE = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '')
const USER_STORAGE_KEY = 'banking-platform-dev-user-id'

export const DEV_USERS = [
  { id: '11111111-1111-1111-1111-111111111111', name: 'Unit Head / Dept Admin', short: 'UH' },
  { id: '22222222-2222-2222-2222-222222222222', name: 'Team Lead', short: 'TL' },
  { id: '33333333-3333-3333-3333-333333333333', name: 'Officer One', short: 'O1' },
  { id: '44444444-4444-4444-4444-444444444444', name: 'Officer Two', short: 'O2' },
]

export function getDevUserId() {
  return localStorage.getItem(USER_STORAGE_KEY) || DEV_USERS[0].id
}

export function setDevUserId(userId) {
  localStorage.setItem(USER_STORAGE_KEY, userId)
}

export async function api(path, options = {}) {
  const headers = new Headers(options.headers || {})
  headers.set('X-User-Id', getDevUserId())

  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
    body: options.body && !(options.body instanceof FormData)
      ? JSON.stringify(options.body)
      : options.body,
  })

  if (!response.ok) {
    let message = `Request failed (${response.status})`
    try {
      const problem = await response.json()
      message = problem.detail || problem.title || message
    } catch {
      // Keep fallback message.
    }
    throw new Error(message)
  }

  if (response.status === 204) return null
  const contentType = response.headers.get('content-type') || ''
  return contentType.includes('application/json') ? response.json() : response.text()
}
