import { useEffect, useState } from 'react'
import './App.css'

interface PingResponse {
  message: string
  utcTime: string
}

function App() {
  const [ping, setPing] = useState<PingResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/ping')
      .then((res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json()
      })
      .then(setPing)
      .catch((err: Error) => setError(err.message))
  }, [])

  return (
    <main className="app">
      <h1>NewsReader</h1>
      <p>USENET (NNTP) reader</p>
      <div className="status">
        {error ? (
          <p className="error">Backend unreachable: {error}</p>
        ) : ping ? (
          <p className="ok">
            Backend says: <strong>{ping.message}</strong> ({new Date(ping.utcTime).toLocaleTimeString()})
          </p>
        ) : (
          <p>Pinging backend…</p>
        )}
      </div>
    </main>
  )
}

export default App