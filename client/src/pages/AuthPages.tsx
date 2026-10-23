import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError, errorMessage } from '../api/client'
import { useAuth } from '../auth/useAuth'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email, password)
      navigate('/')
    } catch (e) {
      setError(e instanceof ApiError && e.status === 401 ? 'E-posta veya şifre hatalı.' : errorMessage(e))
      setBusy(false)
    }
  }

  return (
    <div className="auth-card">
      <h1>Giriş yap</h1>
      <form onSubmit={onSubmit} className="form">
        <label>
          E-posta
          <input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Şifre
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p className="alert alert-error">{error}</p>}
        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Giriş yapılıyor…' : 'Giriş yap'}
        </button>
      </form>
      <p className="muted">
        Hesabın yok mu? <Link to="/register">Kayıt ol</Link>
      </p>
    </div>
  )
}

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await register(email, password, displayName.trim())
      navigate('/')
    } catch (e) {
      setError(errorMessage(e))
      setBusy(false)
    }
  }

  return (
    <div className="auth-card">
      <h1>Kayıt ol</h1>
      <p className="muted">XP topla, seviye atla, liderlik tablosuna gir.</p>
      <form onSubmit={onSubmit} className="form">
        <label>
          Görünen ad
          <input
            required
            minLength={3}
            maxLength={24}
            pattern="[\p{L}\p{N} _.\-]+"
            title="Sadece harf, rakam, boşluk ve _ . - kullanılabilir."
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="ör. AsyncAvcisi"
          />
        </label>
        <label>
          E-posta
          <input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Şifre
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <span className="field-hint">En az 8 karakter, bir küçük harf ve bir rakam.</span>
        </label>
        {error && <p className="alert alert-error">{error}</p>}
        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Hesap oluşturuluyor…' : 'Kayıt ol'}
        </button>
      </form>
      <p className="muted">
        Zaten hesabın var mı? <Link to="/login">Giriş yap</Link>
      </p>
    </div>
  )
}
