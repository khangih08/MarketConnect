import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Optional: add interceptors to attach auth tokens or handle errors
api.interceptors.response.use(
  response => response,
  error => {
    // central error handling
    return Promise.reject(error)
  }
)

export default api
