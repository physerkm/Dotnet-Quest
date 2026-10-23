import { highlightCSharp } from '../components/CodeLine'
import type { AnswerFeedback } from '../api/types'

interface Props {
  code: string
  selected: number | null
  feedback: AnswerFeedback | null
  onSelect: (line: number) => void
}

export function BugHunt({ code, selected, feedback, onSelect }: Props) {
  const lines = code.split('\n')

  return (
    <div className="code-block" role="radiogroup" aria-label="Kod satırları">
      {lines.map((text, index) => {
        const lineNumber = index + 1
        const isBlank = text.trim() === ''

        let state = ''
        if (feedback) {
          if (lineNumber === feedback.correctLine) state = 'is-correct'
          else if (lineNumber === selected) state = 'is-wrong'
        } else if (lineNumber === selected) {
          state = 'is-selected'
        }

        return (
          <button
            key={lineNumber}
            type="button"
            role="radio"
            aria-checked={lineNumber === selected}
            aria-label={`Satır ${lineNumber}`}
            className={`code-line ${state}`}
            disabled={feedback !== null || isBlank}
            onClick={() => onSelect(lineNumber)}
          >
            <span className="line-number">{lineNumber}</span>
            <code className="line-text">{highlightCSharp(text)}</code>
            {state === 'is-correct' && <span className="line-badge">🐞 hata burada</span>}
          </button>
        )
      })}
    </div>
  )
}
