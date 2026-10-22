import { FormEvent, useEffect, useRef, useState } from 'react'
import { FileText, ImagePlus, MessageCircle, Paperclip, Send } from 'lucide-react'
import { api } from '../api'
import type { ChatMessage, Profile, SupportConversation } from '../types'
import { ErrorMessage, Loading, PageHeader } from '../ui'

type Attachment = { url: string; name: string; contentType: string }
const allowedTypes = new Set(['image/png', 'image/jpeg', 'application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'])

export function ChatPage({ profile, isSuperAdmin }: { profile: Profile; isSuperAdmin: boolean }) {
  const [items, setItems] = useState<ChatMessage[]>([])
  const [conversations, setConversations] = useState<SupportConversation[]>([])
  const [selectedTenantId, setSelectedTenantId] = useState<string>()
  const [body, setBody] = useState('')
  const [attachment, setAttachment] = useState<Attachment>()
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(isSuperAdmin)
  const input = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (!isSuperAdmin) return
    api.supportConversations().then(next => { setConversations(next); setSelectedTenantId(current => current ?? next[0]?.id) }).catch(reason => setError((reason as Error).message)).finally(() => setLoading(false))
  }, [isSuperAdmin])

  useEffect(() => {
    if (isSuperAdmin && !selectedTenantId) { setItems([]); return }
    let active = true
    const load = () => api.chat(isSuperAdmin ? selectedTenantId : undefined).then(next => { if (active) setItems(next) }).catch(reason => { if (active) setError((reason as Error).message) })
    load()
    const timer = window.setInterval(load, 8000)
    return () => { active = false; window.clearInterval(timer) }
  }, [isSuperAdmin, selectedTenantId])

  async function send(event: FormEvent) {
    event.preventDefault()
    if ((!body.trim() && !attachment) || (isSuperAdmin && !selectedTenantId)) return
    try {
      const message = await api.sendChat(body, attachment, isSuperAdmin ? selectedTenantId : undefined)
      setItems(current => [...current, message]); setBody(''); setAttachment(undefined)
      if (input.current) input.current.value = ''
    } catch (reason) { setError((reason as Error).message) }
  }

  function read(file?: File) {
    if (!file) return
    const extension = file.name.split('.').pop()?.toLowerCase()
    const contentType = file.type || ({ png: 'image/png', jpg: 'image/jpeg', jpeg: 'image/jpeg', pdf: 'application/pdf', doc: 'application/msword', docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' }[extension ?? ''] ?? '')
    if (!allowedTypes.has(contentType)) { setError('Solo puedes adjuntar PNG, JPG, PDF, DOC o DOCX.'); return }
    if (file.size > 5_000_000) { setError('El archivo no puede superar 5 MB.'); return }
    const reader = new FileReader()
    reader.onload = () => { setAttachment({ url: String(reader.result), name: file.name, contentType }); setError('') }
    reader.onerror = () => setError('No se pudo leer el archivo.')
    reader.readAsDataURL(file)
  }

  const selectedName = conversations.find(item => item.id === selectedTenantId)?.name
  return <>
    <PageHeader eyebrow="SOPORTE INTERNO" title={isSuperAdmin ? 'Conversaciones con empresas' : 'Soporte Atlas Technologies'} description={isSuperAdmin ? 'Atiende reportes e incidencias de los administradores de cada empresa.' : 'Reporta incidentes, errores o dudas directamente a la administracion suprema.'} />
    <ErrorMessage error={error} />
    {loading ? <Loading /> : <section className={`support-chat ${isSuperAdmin ? 'with-conversations' : ''}`}>
      {isSuperAdmin && <aside className="support-conversations"><p className="eyebrow">EMPRESAS</p>{conversations.length === 0 ? <p className="muted">No hay empresas activas.</p> : conversations.map(conversation => <button type="button" key={conversation.id} className={conversation.id === selectedTenantId ? 'active' : ''} onClick={() => setSelectedTenantId(conversation.id)}>{conversation.logoUrl ? <img src={conversation.logoUrl} alt="" /> : <span>{conversation.name.slice(0, 2).toUpperCase()}</span>}<strong>{conversation.name}</strong>{conversation.lastMessageAtUtc && <small>{new Date(conversation.lastMessageAtUtc).toLocaleDateString()}</small>}</button>)}</aside>}
      <section className="panel chat-panel">{isSuperAdmin && <div className="chat-context"><MessageCircle size={18} /><strong>{selectedName ?? 'Selecciona una empresa'}</strong></div>}<div className="chat-messages">{items.length === 0 ? <p className="muted">Todavia no hay mensajes en esta conversacion.</p> : items.map(item => <article className={`chat-message ${item.senderUserId === profile.id ? 'mine' : ''}`} key={item.id}><strong>{item.senderName}</strong><small>{new Date(item.createdAtUtc).toLocaleString()}</small>{item.body && <p>{item.body}</p>}<AttachmentView item={item} /></article>)}</div><form className="chat-compose" onSubmit={send}><label className="icon ghost" title="Adjuntar PNG, JPG, PDF, DOC o DOCX"><Paperclip size={19} /><input ref={input} hidden type="file" accept="image/png,image/jpeg,application/pdf,.doc,.docx" onChange={event => read(event.target.files?.[0])} /></label><input value={body} onChange={event => setBody(event.target.value)} placeholder="Describe el incidente o responde..." disabled={isSuperAdmin && !selectedTenantId} /><button disabled={(!body.trim() && !attachment) || (isSuperAdmin && !selectedTenantId)}><Send size={17} />Enviar</button></form>{attachment && <p className="field-help chat-attachment-ready"><FileText size={14} /> {attachment.name} listo para enviar.</p>}</section>
    </section>}
  </>
}

function AttachmentView({ item }: { item: ChatMessage }) {
  if (!item.photoUrl) return null
  if (item.attachmentContentType?.startsWith('image/')) return <img src={item.photoUrl} alt={item.attachmentName ?? 'Imagen adjunta'} />
  return <a className="chat-file" href={item.photoUrl} download={item.attachmentName ?? 'adjunto'}><FileText size={18} /><span><strong>{item.attachmentName ?? 'Archivo adjunto'}</strong><small>{item.attachmentContentType === 'application/pdf' ? 'Documento PDF' : 'Documento adjunto'}</small></span></a>
}