import { Link, NavLink, Route, Routes } from 'react-router'
import { useAuth } from './auth/useAuth'
import { XpBar } from './components/XpBar'
import { LoginPage, RegisterPage } from './pages/AuthPages'
import GameRoute from './pages/GamePage'
import HomePage from './pages/HomePage'
import LeaderboardPage from './pages/LeaderboardPage'
import ProfilePage from './pages/ProfilePage'

function Navbar() {
  const { player, loading } = useAuth()

  return (
    <header className="navbar">
      <div className="navbar-inner">
        <Link to="/" className="brand" aria-label="DotNet Quest ana sayfa">
          <span className="brand-mark">&lt;/&gt;</span> <span className="brand-text">DotNet Quest</span>
        </Link>
        <nav className="nav-links">
          <NavLink to="/" end>
            Oyna
          </NavLink>
          <NavLink to="/leaderboard">Liderlik</NavLink>
        </nav>
        <div className="nav-user">
          {loading ? null : player ? (
            <Link to="/profile" className="player-chip" title={`${player.totalXp} XP`}>
              <span className="level-chip">Lv {player.level}</span>
              <span className="player-name">{player.displayName ?? 'Profil'}</span>
              <XpBar xp={player.totalXp} start={player.levelStartXp} next={player.nextLevelXp} compact />
            </Link>
          ) : (
            <>
              <Link to="/login" className="btn btn-ghost btn-sm">
                Giriş
              </Link>
              <Link to="/register" className="btn btn-primary btn-sm">
                Kayıt ol
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}

function NotFound() {
  return (
    <div className="page-center">
      <h1>404 Not Found</h1>
      <p className="muted">Bu sayfa yok. Tam olarak doğru status kodu, değil mi? 😉</p>
      <Link to="/" className="btn btn-primary">
        Ana sayfaya dön
      </Link>
    </div>
  )
}

export default function App() {
  return (
    <>
      <Navbar />
      <main className="container">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/play/:sessionId" element={<GameRoute />} />
          <Route path="/leaderboard" element={<LeaderboardPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <footer className="footer muted">
        .NET 10 Web API + React ile yapıldı · API dokümantasyonu: <a href="/scalar">/scalar</a> (development)
      </footer>
    </>
  )
}
