import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { api, errorMessage } from '../api/client'
import type { AnswerResponse, Session } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { difficultyLabel } from '../game/difficulty'
import { QuestionCard } from '../game/QuestionCard'
import { ResultScreen } from '../game/ResultScreen'

/** URL'deki tur değişince (ör. "Tekrar oyna") tüm state'in sıfırlanması için key kullanılır. */
export default function GameRoute() {
  const { sessionId } = useParams()
  return <GamePage key={sessionId} sessionId={sessionId!} />
}

function GamePage({ sessionId }: { sessionId: string }) {
  const { player, refresh } = useAuth()
  const [session, setSession] = useState<Session | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [index, setIndex] = useState(0)
  const [showResults, setShowResults] = useState(false)
  const [lastResult, setLastResult] = useState<AnswerResponse | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    api
      .getSession(sessionId, controller.signal)
      .then((loaded) => {
        setSession(loaded)
        const firstUnanswered = loaded.questions.findIndex((q) => q.feedback === null)
        if (firstUnanswered === -1) setShowResults(true)
        else setIndex(firstUnanswered)
      })
      .catch((e) => {
        if (!controller.signal.aborted) setError(errorMessage(e))
      })

    return () => controller.abort()
  }, [sessionId])

  if (error) {
    return (
      <div className="page-center">
        <p className="alert alert-error">{error}</p>
        <Link to="/" className="btn">
          Ana sayfaya dön
        </Link>
      </div>
    )
  }

  if (!session) return <div className="page-center muted">Yükleniyor…</div>

  const handleAnswered = (questionId: number, result: AnswerResponse) => {
    setLastResult(result)
    setSession((current) =>
      current && {
        ...current,
        score: result.sessionScore,
        isCompleted: result.sessionCompleted,
        questions: current.questions.map((q) => (q.id === questionId ? { ...q, feedback: result.feedback } : q)),
      },
    )

    // Navbar'daki XP ve seviye güncellensin.
    if (result.sessionCompleted && player) void refresh()
  }

  const handleNext = () => {
    if (session.isCompleted) setShowResults(true)
    else setIndex((i) => Math.min(i + 1, session.questions.length - 1))
  }

  const question = session.questions[index]

  return (
    <div className="game">
      <div className="game-header">
        <div className="game-title">
          <span className="game-icon" aria-hidden="true">
            {session.categoryIcon}
          </span>
          <div>
            <h1>{session.categoryTitle}</h1>
            <span className={`badge badge-${session.difficulty}`}>{difficultyLabel(session.difficulty)}</span>
          </div>
        </div>
        <div className="game-score" aria-label="Puan">
          <span className="score-value">{session.score}</span>
          <span className="muted"> / {session.maxScore} puan</span>
        </div>
      </div>

      <div className="progress-dots" aria-label="İlerleme">
        {session.questions.map((q, i) => (
          <span
            key={q.id}
            className={[
              'dot',
              q.feedback ? (q.feedback.isCorrect ? 'dot-correct' : 'dot-wrong') : '',
              !showResults && i === index ? 'dot-current' : '',
            ].join(' ')}
            title={`Soru ${i + 1}`}
          />
        ))}
      </div>

      {showResults ? (
        <ResultScreen session={session} lastResult={lastResult} />
      ) : (
        <>
          <p className="muted question-counter">
            Soru {index + 1} / {session.questions.length}
          </p>
          <QuestionCard
            key={question.id}
            sessionId={session.id}
            question={question}
            isLast={session.isCompleted || index === session.questions.length - 1}
            onAnswered={handleAnswered}
            onNext={handleNext}
          />
        </>
      )}
    </div>
  )
}
