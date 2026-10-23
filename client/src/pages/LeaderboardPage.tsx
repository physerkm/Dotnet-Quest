import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, errorMessage } from '../api/client'
import type { LeaderboardEntry } from '../api/types'

const medals = ['🥇', '🥈', '🥉']

export default function LeaderboardPage() {
  const [entries, setEntries] = useState<LeaderboardEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    api
      .getLeaderboard(20, controller.signal)
      .then(setEntries)
      .catch((e) => {
        if (!controller.signal.aborted) setError(errorMessage(e))
      })
    return () => controller.abort()
  }, [])

  return (
    <div className="narrow">
      <h1>🏆 Liderlik Tablosu</h1>
      <p className="muted">En çok XP toplayan geliştiriciler. Sadece kayıtlı oyuncuların XP'si sayılır.</p>

      {error && <p className="alert alert-error">{error}</p>}
      {!entries && !error && <p className="muted">Yükleniyor…</p>}

      {entries?.length === 0 && (
        <div className="empty">
          <p>Henüz kimse XP kazanmadı. İlk sen ol!</p>
          <Link to="/" className="btn btn-primary">
            Oynamaya başla
          </Link>
        </div>
      )}

      {entries && entries.length > 0 && (
        <table className="leaderboard">
          <thead>
            <tr>
              <th scope="col">#</th>
              <th scope="col">Oyuncu</th>
              <th scope="col">Seviye</th>
              <th scope="col" className="num">
                XP
              </th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.rank}>
                <td className="rank">{medals[entry.rank - 1] ?? entry.rank}</td>
                <td>{entry.displayName}</td>
                <td>
                  <span className="level-chip">Lv {entry.level}</span> <span className="muted">{entry.title}</span>
                </td>
                <td className="num strong">{entry.totalXp}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
