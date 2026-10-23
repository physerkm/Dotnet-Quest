import { looksLikeCode } from '../components/CodeLine'
import type { AnswerFeedback } from '../api/types'

interface Props {
  options: string[]
  selected: number | null
  feedback: AnswerFeedback | null
  onSelect: (index: number) => void
}

const letters = ['A', 'B', 'C', 'D', 'E', 'F']

export function MultipleChoice({ options, selected, feedback, onSelect }: Props) {
  return (
    <div className="choices" role="radiogroup" aria-label="Şıklar">
      {options.map((option, index) => {
        let state = ''
        if (feedback) {
          if (index === feedback.correctOption) state = 'is-correct'
          else if (index === selected) state = 'is-wrong'
          else state = 'is-dimmed'
        } else if (index === selected) {
          state = 'is-selected'
        }

        return (
          <button
            key={option}
            type="button"
            role="radio"
            aria-checked={index === selected}
            className={`choice ${state}`}
            disabled={feedback !== null}
            onClick={() => onSelect(index)}
          >
            <span className="choice-letter">{letters[index]}</span>
            <span className={looksLikeCode(option) ? 'choice-text mono' : 'choice-text'}>{option}</span>
          </button>
        )
      })}
    </div>
  )
}
