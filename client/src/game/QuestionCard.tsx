import { useEffect, useState } from 'react'
import { api, errorMessage } from '../api/client'
import type { AnswerResponse, SessionQuestion, SubmitAnswer } from '../api/types'
import { BugHunt } from './BugHunt'
import { MultipleChoice } from './MultipleChoice'
import { Ordering } from './Ordering'

interface Props {
  sessionId: string
  question: SessionQuestion
  isLast: boolean
  onAnswered: (questionId: number, result: AnswerResponse) => void
  onNext: () => void
}

const typeLabels = {
  MultipleChoice: { label: 'Çoktan seçmeli', hint: 'Doğru şıkkı seç.' },
  BugHunt: { label: 'Bug Avı', hint: 'Hatalı satıra tıkla.' },
  Ordering: { label: 'Sıralama', hint: 'Oklarla doğru sıraya diz.' },
} as const

export function QuestionCard({ sessionId, question, isLast, onAnswered, onNext }: Props) {
  const [selectedOption, setSelectedOption] = useState<number | null>(null)
  const [selectedLine, setSelectedLine] = useState<number | null>(null)
  const [ordering, setOrdering] = useState<string[]>(question.options)
  const [orderTouched, setOrderTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const feedback = question.feedback
  const canSubmit =
    !feedback &&
    !submitting &&
    (question.type === 'MultipleChoice'
      ? selectedOption !== null
      : question.type === 'BugHunt'
        ? selectedLine !== null
        : orderTouched)

  const submit = async () => {
    if (!canSubmit) return

    const answer: SubmitAnswer = { questionId: question.id }
    if (question.type === 'MultipleChoice') answer.selectedOption = selectedOption!
    if (question.type === 'BugHunt') answer.selectedLine = selectedLine!
    if (question.type === 'Ordering') answer.ordering = ordering

    setSubmitting(true)
    setError(null)
    try {
      const result = await api.submitAnswer(sessionId, answer)
      onAnswered(question.id, result)
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setSubmitting(false)
    }
  }

  // Klavye kısayolları: A-D / 1-4 şık seçer, Enter cevaplar veya sonraki soruya geçer.
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null
      if (event.metaKey || event.ctrlKey || event.altKey) return
      // Form alanlarında, linklerde ve sıralama oklarında tarayıcının normal davranışı korunur.
      if (target?.closest('input, textarea, a, .order-buttons')) return

      if (event.key === 'Enter') {
        // preventDefault: odaktaki butonun ayrıca "click" üretip işlemi iki kez yapmasını engeller.
        event.preventDefault()
        if (feedback) onNext()
        else if (canSubmit) void submit()
        return
      }

      if (!feedback && question.type === 'MultipleChoice') {
        const index = '1234'.indexOf(event.key) >= 0 ? '1234'.indexOf(event.key) : 'abcd'.indexOf(event.key.toLowerCase())
        if (index >= 0 && index < question.options.length) setSelectedOption(index)
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  })

  const type = typeLabels[question.type]

  return (
    <article className="question-card">
      <header className="question-meta">
        <span className={`type-pill type-${question.type}`}>{type.label}</span>
        <span className="muted">{type.hint}</span>
      </header>

      <h2 className="question-prompt">{question.prompt}</h2>

      {question.type === 'MultipleChoice' && (
        <MultipleChoice
          options={question.options}
          selected={selectedOption}
          feedback={feedback}
          onSelect={setSelectedOption}
        />
      )}

      {question.type === 'BugHunt' && question.code && (
        <BugHunt code={question.code} selected={selectedLine} feedback={feedback} onSelect={setSelectedLine} />
      )}

      {question.type === 'Ordering' && (
        <Ordering
          items={ordering}
          feedback={feedback}
          onChange={(items) => {
            setOrdering(items)
            setOrderTouched(true)
          }}
        />
      )}

      {error && <p className="alert alert-error">{error}</p>}

      {feedback ? (
        <div className={`feedback ${feedback.isCorrect ? 'feedback-correct' : 'feedback-wrong'}`} role="status">
          <div className="feedback-title">
            {feedback.isCorrect ? `✅ Doğru! +${feedback.pointsEarned} puan` : '❌ Yanlış'}
          </div>
          <p className="feedback-text">{feedback.explanation}</p>
          <button type="button" className="btn btn-primary" onClick={onNext} autoFocus>
            {isLast ? 'Sonuçları gör →' : 'Sonraki soru →'}
          </button>
        </div>
      ) : (
        <div className="question-actions">
          {question.type === 'Ordering' && !orderTouched && (
            <span className="muted">Öğeleri en az bir kez taşı, sonra cevapla.</span>
          )}
          <button type="button" className="btn btn-primary" disabled={!canSubmit} onClick={() => void submit()}>
            {submitting ? 'Kontrol ediliyor…' : 'Cevapla'}
          </button>
        </div>
      )}
    </article>
  )
}
