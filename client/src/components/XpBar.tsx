interface Props {
  xp: number
  start: number
  next: number | null
  compact?: boolean
}

export function XpBar({ xp, start, next, compact = false }: Props) {
  const percent = next === null ? 100 : Math.min(100, Math.round(((xp - start) / (next - start)) * 100))

  return (
    <div className={compact ? 'xp-bar compact' : 'xp-bar'}>
      <div
        className="xp-track"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        aria-label="Sonraki seviyeye ilerleme"
      >
        <div className="xp-fill" style={{ width: `${percent}%` }} />
      </div>
      {!compact && (
        <span className="xp-text muted">
          {next === null ? 'Maksimum seviye!' : `${xp} / ${next} XP · sonraki seviyeye ${next - xp} XP`}
        </span>
      )}
    </div>
  )
}
