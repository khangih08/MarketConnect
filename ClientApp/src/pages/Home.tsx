import React, { useEffect, useState } from 'react'
import api from '../services/api'

export default function Home() {
  const [message, setMessage] = useState<string>('')

  useEffect(() => {
    api.get('/health')
      .then(r => setMessage(r.data?.message ?? 'OK'))
      .catch(() => setMessage('API unavailable'))
  }, [])

  return (
    <div>
      <h1 className="text-2xl font-semibold">Home</h1>
      <p className="mt-4">API status: {message}</p>
    </div>
  )
}
