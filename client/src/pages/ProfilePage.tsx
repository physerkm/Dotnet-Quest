import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router'
import { api, errorMessage } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { XpBar } from '../components/XpBar'

export default function ProfilePage() {
  const { player, loading, refresh, logout } = useAuth()
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [editing, setEditing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (loading) return <div className="page-center muted">Yükleniyor…</div>
  if (!player) return <Navigate to="/login" replace />

  const accuracy = player.answersGiven === 0 ? 0 : Math.round((player.correctAnswers / player.answersGiven) * 100)

  const saveName = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      await api.updateMe(name.trim())
      await refresh()
      setEditing(false)
    } catch (e) {
      setError(errorMessage(e))
    }
  }

  return (
    <div className="narrow">
      <div className="profile-head">
        <div className="avatar" aria-hidden="true">
          {(player.displayName ?? player.email).charAt(0).toUpperCase()}
        </div>
        <div>
          {editing ? (
            <form onSubmit={saveName} className="inline-form">
              <input
                autoFocus
                required
                minLength={3}
                maxLength={24}
                value={name}
                onChange={(e) => setName(e.target.value)}
                aria-label="Görünen ad"
              />
              <button type="submit" className="btn btn-primary btn-sm">
                Kaydet
              </button>
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setEditing(false)}>
                Vazgeç
              </button>
            </form>
          ) : (
            <h1>
              {player.displayName ?? 'İsimsiz Geliştirici'}{' '}
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                onClick={() => {
                  setName(player.displayName ?? '')
                  setEditing(true)
                }}
              >
                ✏️ Düzenle
              </button>
            </h1>
          )}
          <p className="muted">{player.email}</p>
          {error && <p className="alert alert-error">{error}</p>}
        </div>
      </div>

      <div className="level-card">
        <div className="level-card-top">
          <span className="level-chip big">Lv {player.level}</span>
          <span className="level-title">{player.title}</span>
          <span className="muted">#{player.rank} sırada</span>
        </div>
        <XpBar xp={player.totalXp} start={player.levelStartXp} next={player.nextLevelXp} />
      </div>

      <div className="stats">
        <div className="stat">
          <span className="stat-value">{player.totalXp}</span>
          <span className="stat-label">Toplam XP</span>
        </div>
        <div className="stat">
          <span className="stat-value">{player.sessionsCompleted}</span>
          <span className="stat-label">Tamamlanan tur</span>
        </div>
        <div className="stat">
          <span className="stat-value">
            {player.correctAnswers}/{player.answersGiven}
          </span>
          <span className="stat-label">Doğru cevap</span>
        </div>
        <div className="stat">
          <span className="stat-value">%{accuracy}</span>
          <span className="stat-label">İsabet</span>
        </div>
      </div>

      <button
        type="button"
        className="btn btn-ghost"
        onClick={async () => {
          await logout()
          navigate('/')
        }}
      >
        Çıkış yap
      </button>
    </div>
  )
}
