import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { api, errorMessage } from '../api/client'
import type { AnswerResponse, Difficulty, Session } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { difficultyLabel, nextDifficulty } from './difficulty'

interface Props {
  session: Session
  lastResult: AnswerResponse | null
}

function verdict(ratio: number) {
  if (ratio === 1) return { emoji: '🏆', text: 'Kusursuz! Tam bir .NET ustası.' }
  if (ratio >= 0.6) return { emoji: '🎉', text: 'Harika iş!' }
  if (ratio >= 0.4) return { emoji: '💪', text: 'Fena değil, biraz daha pratik!' }
  return { emoji: '📚', text: 'Açıklamaları oku ve tekrar dene. Her yanlış bir derstir.' }
}

export function ResultScreen({ session, lastResult }: Props) {
  const { player } = useAuth()
  const navigate = useNavigate()
  const [starting, setStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const correct = session.questions.filter((q) => q.feedback?.isCorrect).length
  const total = session.questions.length
  const { emoji, text } = verdict(total === 0 ? 0 : correct / total)
  const harder = nextDifficulty(session.difficulty)

  const playAgain = async (difficulty: Difficulty) => {
    setStarting(true)
    setError(null)
    try {
      const next = await api.createSession(session.categoryId, difficulty)
      navigate(`/play/${next.id}`)
    } catch (e) {
      setError(errorMessage(e))
      setStarting(false)
    }
  }

  return (
    <section className="result">
      <div className="result-emoji" aria-hidden="true">
        {emoji}
      </div>
      <h1>{text}</h1>
      <p className="result-score">
        <strong>{correct}</strong> / {total} doğru · <strong>{session.score}</strong> / {session.maxScore} puan
      </p>

      {lastResult?.xpEarned != null ? (
        <p className="xp-gain">
          +{lastResult.xpEarned} XP
          {lastResult.isPerfect && <span className="bonus"> (hatasız tur bonusu dahil!)</span>}
        </p>
      ) : !player ? (
        <p className="alert alert-info">
          Bu turda kazandığın puanlar kaydedilmedi. <Link to="/register">Kayıt ol</Link>, XP topla ve liderlik
          tablosuna gir!
        </p>
      ) : null}

      {error && <p className="alert alert-error">{error}</p>}

      <div className="result-actions">
        <button type="button" className="btn btn-primary" disabled={starting} onClick={() => playAgain(session.difficulty)}>
          🔁 Tekrar oyna ({difficultyLabel(session.difficulty)})
        </button>
        {harder && (
          <button type="button" className="btn" disabled={starting} onClick={() => playAgain(harder)}>
            ⬆️ {difficultyLabel(harder)} seviyeyi dene
          </button>
        )}
        <Link to="/" className="btn btn-ghost">
          Kategorilere dön
        </Link>
      </div>

      <ol className="result-summary">
        {session.questions.map((q) => (
          <li key={q.id} className={q.feedback?.isCorrect ? 'is-correct' : 'is-wrong'}>
            <span aria-hidden="true">{q.feedback?.isCorrect ? '✓' : '✗'}</span> {q.prompt}
          </li>
        ))}
      </ol>
    </section>
  )
}
