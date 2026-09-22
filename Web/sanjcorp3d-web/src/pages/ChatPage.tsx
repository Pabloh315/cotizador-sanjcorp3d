import { FormEvent, useEffect, useRef, useState } from 'react'
import { ImagePlus, MessageCircle, Send } from 'lucide-react'
import { api } from '../api'
import type { ChatMessage, Profile } from '../types'
import { ErrorMessage, PageHeader } from '../ui'

export function ChatPage({ profile, title = 'Chat con administración suprema' }: { profile: Profile; title?: string }) {
  const [items, setItems] = useState<ChatMessage[]>([])
  const [body, setBody] = useState('')
  const [photo, setPhoto] = useState<string>()
  const [error, setError] = useState('')
  const last = useRef(0)
  const load = async () => { try { const next = await api.chat(); setItems(next); last.current = next.at(-1)?.id ?? 0 } catch (e) { setError((e as Error).message) } }
  useEffect(() => { load(); const timer = window.setInterval(load, 5000); return () => window.clearInterval(timer) }, [])
  async function send(event: FormEvent) { event.preventDefault(); if (!body.trim() && !photo) return; try { const message = await api.sendChat(body, photo); setItems(current => [...current, message]); last.current = message.id; setBody(''); setPhoto(undefined) } catch (e) { setError((e as Error).message) } }
  function read(file?: File) { if (!file) return; if (file.size > 1_500_000) { setError('La foto no puede superar 1.5 MB.'); return } const reader = new FileReader(); reader.onload = () => setPhoto(String(reader.result)); reader.readAsDataURL(file) }
  return <><PageHeader eyebrow="COMUNICACIÓN" title={title} description="Mensajes privados del espacio de trabajo con administración." /><ErrorMessage error={error} /><section className="panel chat-panel"><div className="chat-messages">{items.length === 0 ? <p className="muted">Todavía no hay mensajes.</p> : items.map(item => <article className={`chat-message ${item.senderUserId === profile.id ? 'mine' : ''}`} key={item.id}><strong>{item.senderName}</strong><small>{new Date(item.createdAtUtc).toLocaleString()}</small>{item.body && <p>{item.body}</p>}{item.photoUrl && <img src={item.photoUrl} alt="Foto enviada en el chat" />}</article>)}</div><form className="chat-compose" onSubmit={send}><label className="icon ghost" title="Adjuntar foto"><ImagePlus size={19} /><input hidden type="file" accept="image/*" onChange={e => read(e.target.files?.[0])} /></label><input value={body} onChange={e => setBody(e.target.value)} placeholder="Escribe un mensaje..." /><button disabled={!body.trim() && !photo}><Send size={17} />Enviar</button></form>{photo && <p className="field-help"><MessageCircle size={14} /> Foto lista para enviar.</p>}</section></>
}


