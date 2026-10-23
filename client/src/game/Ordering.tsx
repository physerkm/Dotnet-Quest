import { looksLikeCode } from '../components/CodeLine'
import type { AnswerFeedback } from '../api/types'

interface Props {
  items: string[]
  feedback: AnswerFeedback | null
  onChange: (items: string[]) => void
}

/** Öğeleri yukarı/aşağı butonlarıyla sıralama. Klavye ve ekran okuyucu dostu (sürükle-bırak gerektirmez). */
export function Ordering({ items, feedback, onChange }: Props) {
  const move = (from: number, to: number) => {
    if (to < 0 || to >= items.length) return
    const next = [...items]
    const [item] = next.splice(from, 1)
    next.splice(to, 0, item)
    onChange(next)
  }

  const correctOrder = feedback?.correctOrder ?? null

  return (
    <div className="ordering">
      <ol className="order-list">
        {items.map((item, index) => {
          const state = correctOrder ? (correctOrder[index] === item ? 'is-correct' : 'is-wrong') : ''
          return (
            <li key={item} className={`order-item ${state}`}>
              <span className="order-index">{index + 1}</span>
              <span className={looksLikeCode(item) ? 'order-text mono' : 'order-text'}>{item}</span>
              {!feedback && (
                <span className="order-buttons">
                  <button type="button" aria-label="Yukarı taşı" disabled={index === 0} onClick={() => move(index, index - 1)}>
                    ↑
                  </button>
                  <button
                    type="button"
                    aria-label="Aşağı taşı"
                    disabled={index === items.length - 1}
                    onClick={() => move(index, index + 1)}
                  >
                    ↓
                  </button>
                </span>
              )}
            </li>
          )
        })}
      </ol>

      {correctOrder && !feedback?.isCorrect && (
        <div className="correct-order">
          <h4>Doğru sıralama</h4>
          <ol>
            {correctOrder.map((item) => (
              <li key={item} className={looksLikeCode(item) ? 'mono' : undefined}>
                {item}
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  )
}
