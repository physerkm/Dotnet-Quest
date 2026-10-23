// Backend DTO'larının TypeScript karşılıkları.

export type Difficulty = 'Easy' | 'Medium' | 'Hard'
export type QuestionType = 'MultipleChoice' | 'BugHunt' | 'Ordering'

export interface Category {
  id: string
  title: string
  description: string
  icon: string
  questionCounts: { easy: number; medium: number; hard: number }
}

export interface AnswerFeedback {
  isCorrect: boolean
  pointsEarned: number
  explanation: string
  correctOption: number | null
  correctLine: number | null
  correctOrder: string[] | null
}

export interface SessionQuestion {
  id: number
  order: number
  type: QuestionType
  prompt: string
  code: string | null
  options: string[]
  feedback: AnswerFeedback | null
}

export interface Session {
  id: string
  categoryId: string
  categoryTitle: string
  categoryIcon: string
  difficulty: Difficulty
  score: number
  maxScore: number
  isCompleted: boolean
  questions: SessionQuestion[]
}

export interface SubmitAnswer {
  questionId: number
  selectedOption?: number
  selectedLine?: number
  ordering?: string[]
}

export interface AnswerResponse {
  feedback: AnswerFeedback
  sessionScore: number
  maxScore: number
  sessionCompleted: boolean
  isPerfect: boolean
  xpEarned: number | null
}

export interface LeaderboardEntry {
  rank: number
  displayName: string
  totalXp: number
  level: number
  title: string
}

export interface PlayerProfile {
  email: string
  displayName: string | null
  totalXp: number
  level: number
  title: string
  levelStartXp: number
  nextLevelXp: number | null
  rank: number
  sessionsCompleted: number
  answersGiven: number
  correctAnswers: number
}

/** RFC 9457 Problem Details */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
