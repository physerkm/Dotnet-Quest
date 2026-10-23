import type {
  AnswerResponse,
  Category,
  Difficulty,
  LeaderboardEntry,
  PlayerProfile,
  ProblemDetails,
  Session,
  SubmitAnswer,
} from './types'

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `HTTP ${status}`)
    this.status = status
    this.problem = problem
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    credentials: 'same-origin', // auth cookie'si otomatik gönderilir
    headers: { 'Content-Type': 'application/json', ...init.headers },
  })

  const text = await response.text()
  let body: unknown
  try {
    body = text ? JSON.parse(text) : undefined
  } catch {
    body = undefined // ör. API kapalıyken proxy'nin döndüğü düz metin
  }

  if (!response.ok) {
    throw new ApiError(response.status, (body as ProblemDetails | undefined) ?? null)
  }

  return body as T
}

const json = (value: unknown) => JSON.stringify(value)

export const api = {
  getCategories: (signal?: AbortSignal) => request<Category[]>('/categories', { signal }),

  createSession: (categoryId: string, difficulty: Difficulty) =>
    request<Session>('/sessions', { method: 'POST', body: json({ categoryId, difficulty }) }),

  getSession: (id: string, signal?: AbortSignal) => request<Session>(`/sessions/${id}`, { signal }),

  submitAnswer: (sessionId: string, answer: SubmitAnswer) =>
    request<AnswerResponse>(`/sessions/${sessionId}/answers`, { method: 'POST', body: json(answer) }),

  getLeaderboard: (top = 20, signal?: AbortSignal) =>
    request<LeaderboardEntry[]>(`/leaderboard?top=${top}`, { signal }),

  getMe: (signal?: AbortSignal) => request<PlayerProfile>('/players/me', { signal }),

  updateMe: (displayName: string) =>
    request<void>('/players/me', { method: 'PUT', body: json({ displayName }) }),

  register: (email: string, password: string) =>
    request<void>('/auth/register', { method: 'POST', body: json({ email, password }) }),

  login: (email: string, password: string) =>
    request<void>('/auth/login?useCookies=true', { method: 'POST', body: json({ email, password }) }),

  logout: () => request<void>('/auth/logout', { method: 'POST' }),
}

// ASP.NET Core Identity hata kodlarının Türkçe karşılıkları.
const identityMessages: Record<string, string> = {
  DuplicateUserName: 'Bu e-posta ile zaten bir hesap var.',
  DuplicateEmail: 'Bu e-posta ile zaten bir hesap var.',
  InvalidEmail: 'Geçerli bir e-posta adresi gir.',
  InvalidUserName: 'Geçerli bir e-posta adresi gir.',
  PasswordTooShort: 'Şifre en az 8 karakter olmalı.',
  PasswordRequiresDigit: 'Şifre en az bir rakam içermeli.',
  PasswordRequiresLower: 'Şifre en az bir küçük harf içermeli.',
  PasswordRequiresUpper: 'Şifre en az bir büyük harf içermeli.',
  PasswordRequiresNonAlphanumeric: 'Şifre en az bir özel karakter içermeli.',
}

/** Herhangi bir hatayı kullanıcıya gösterilebilecek Türkçe bir mesaja çevirir. */
export function errorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Sunucuya ulaşılamadı. API çalışıyor mu?'
  }

  const errors = error.problem?.errors
  if (errors) {
    const messages = Object.entries(errors).map(([key, values]) => identityMessages[key] ?? values[0])
    return [...new Set(messages)].join(' ')
  }

  if (error.status === 429) return 'Çok hızlısın! Biraz bekleyip tekrar dene.'
  if (error.status === 404) return 'Aradığın şey bulunamadı.'
  if (error.status >= 500) return 'Sunucuda beklenmeyen bir hata oluştu.'

  return error.problem?.detail ?? error.problem?.title ?? 'Bir şeyler ters gitti.'
}
