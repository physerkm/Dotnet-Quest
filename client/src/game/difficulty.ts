import type { Category, Difficulty } from '../api/types'

export const difficulties: { value: Difficulty; label: string; points: number; hint: string }[] = [
  { value: 'Easy', label: 'Kolay', points: 10, hint: 'Temeller' },
  { value: 'Medium', label: 'Orta', points: 20, hint: 'Gerçek dünya tuzakları' },
  { value: 'Hard', label: 'Zor', points: 30, hint: 'Kıdemli seviye' },
]

export const difficultyLabel = (difficulty: Difficulty) =>
  difficulties.find((d) => d.value === difficulty)?.label ?? difficulty

export const questionCount = (category: Category, difficulty: Difficulty) =>
  difficulty === 'Easy'
    ? category.questionCounts.easy
    : difficulty === 'Medium'
      ? category.questionCounts.medium
      : category.questionCounts.hard

export const nextDifficulty = (difficulty: Difficulty): Difficulty | null =>
  difficulty === 'Easy' ? 'Medium' : difficulty === 'Medium' ? 'Hard' : null

const storageKey = 'dotnetquest.difficulty'

/** Son seçilen zorluk tarayıcıda hatırlanır (sadece kolaylık; kritik veri değil). */
export function loadDifficulty(): Difficulty {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved === 'Easy' || saved === 'Medium' || saved === 'Hard') return saved
  } catch {
    // Gizli sekme vb. durumlarda localStorage erişilemeyebilir.
  }
  return 'Easy'
}

export function saveDifficulty(difficulty: Difficulty) {
  try {
    localStorage.setItem(storageKey, difficulty)
  } catch {
    // yok say
  }
}
