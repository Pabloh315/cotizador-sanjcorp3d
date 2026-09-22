import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import './styles.css'

document.addEventListener('focusin', event => {
  const target = event.target
  if (target instanceof HTMLInputElement && target.type === 'number' && target.value === '0') {
    window.setTimeout(() => target.select(), 0)
  }
})

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>)
