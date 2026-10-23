import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { api, errorMessage } from '../api/client'
import type { Category, Difficulty } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { difficulties, loadDifficulty, questionCount, saveDifficulty } from '../game/difficulty'

export default function HomePage() {
  const { player } = useAuth()
  const navigate = useNavigate()
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [difficulty, setDifficulty] = useState<Difficulty>(loadDifficulty)
  const [starting, setStarting] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    api
      .getCategories(controller.signal)
      .then(setCategories)
      .catch((e) => {
        if (!controller.signal.aborted) setError(errorMessage(e))
      })
    return () => controller.abort()
  }, [])

  const chooseDifficulty = (value: Difficulty) => {
    setDifficulty(value)
    saveDifficulty(value)
  }

  const start = async (categoryId: string) => {
    setStarting(categoryId)
    setError(null)
    try {
      const session = await api.createSession(categoryId, difficulty)
      navigate(`/play/${session.id}`)
    } catch (e) {
      setError(errorMessage(e))
      setStarting(null)
    }
  }

  return (
    <div className="home">
      <section className="hero">
        <h1>
          Oynayarak öğren: <span className="accent">.NET Web API</span>
        </h1>
        <p className="hero-sub">
          HTTP, REST, Dependency Injection, async/await, middleware ve EF Core. Her turda 5 soru, her yanlışta
          yeni bir ders.
        </p>
        {!player && (
          <p className="hero-note">
            Misafir olarak oynayabilirsin. <Link to="/register">Kayıt olursan</Link> XP kazanır, seviye atlar ve
            liderlik tablosuna girersin.
          </p>
        )}
      </section>

      <section aria-labelledby="difficulty-title">
        <h2 id="difficulty-title" className="section-title">
          1. Zorluk seç
        </h2>
        <div className="difficulty-picker" role="radiogroup" aria-label="Zorluk">
          {difficulties.map((d) => (
            <button
              key={d.value}
              type="button"
              role="radio"
              aria-checked={difficulty === d.value}
              className={`difficulty-option difficulty-${d.value} ${difficulty === d.value ? 'is-active' : ''}`}
              onClick={() => chooseDifficulty(d.value)}
            >
              <span className="difficulty-label">{d.label}</span>
              <span className="difficulty-hint">
                {d.hint} · {d.points} puan/soru
              </span>
            </button>
          ))}
        </div>
      </section>

      <section aria-labelledby="category-title">
        <h2 id="category-title" className="section-title">
          2. Kategori seç ve başla
        </h2>

        {error && <p className="alert alert-error">{error}</p>}
        {!categories && !error && <p className="muted">Kategoriler yükleniyor…</p>}

        <div className="category-grid">
          {categories?.map((category) => {
            const count = questionCount(category, difficulty)
            return (
              <button
                key={category.id}
                type="button"
                className="category-card"
                disabled={starting !== null || count === 0}
                onClick={() => start(category.id)}
              >
                <span className="category-icon" aria-hidden="true">
                  {category.icon}
                </span>
                <span className="category-title">{category.title}</span>
                <span className="category-desc">{category.description}</span>
                <span className="category-footer">
                  <span className="muted">{count} soru</span>
                  <span className="category-cta">{starting === category.id ? 'Başlıyor…' : 'Başla →'}</span>
                </span>
              </button>
            )
          })}
        </div>
      </section>
    </div>
  )
}
