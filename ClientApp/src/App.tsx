import React from 'react'
import { Routes, Route, Link } from 'react-router-dom'
import Home from './pages/Home'
import About from './pages/About'

export default function App() {
  return (
    <div className="min-h-screen">
      <nav className="p-4 bg-white shadow">
        <div className="container mx-auto flex gap-4">
          <Link to="/" className="text-blue-600">Home</Link>
          <Link to="/about" className="text-blue-600">About</Link>
        </div>
      </nav>

      <main className="container mx-auto p-4">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/about" element={<About />} />
        </Routes>
      </main>
    </div>
  )
}
