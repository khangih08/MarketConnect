import React from 'react'
import { Routes, Route, Link } from 'react-router-dom'
import Home from './pages/Home'
import About from './pages/About'
import ModeratorDashboard from './pages/ModeratorDashboard'

export default function App() {
  return (
    <div className="min-h-screen">
      <nav className="p-4 bg-white shadow">
        <div className="container mx-auto flex gap-4 text-xs font-bold">
          <Link to="/" className="text-blue-600">Trang chủ</Link>
          <Link to="/about" className="text-blue-600">Giới thiệu</Link>
          <Link to="/moderation" className="text-emerald-700">Cổng Kiểm Duyệt (Moderation)</Link>
        </div>
      </nav>

      <main className="container mx-auto p-4">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/about" element={<About />} />
          <Route path="/moderation" element={<ModeratorDashboard />} />
        </Routes>
      </main>
    </div>
  )
}
