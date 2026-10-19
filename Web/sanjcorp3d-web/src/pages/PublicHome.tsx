import { ArrowRight, Calculator, ClipboardList, Moon, PackageSearch, ShieldCheck, Sun, Users } from 'lucide-react'

const brandName = 'Atlas Technologies'

type PublicTheme = 'light' | 'dark'
type PublicHomeProps = { onLoginClick: () => void; theme: PublicTheme; onToggleTheme: () => void }

const features = [
  { icon: Calculator, title: 'Cotizaciones precisas', text: 'Calcula trabajos de impresion 3D considerando materiales, produccion, adicionales y transporte.' },
  { icon: PackageSearch, title: 'Inventario organizado', text: 'Controla filamentos, resinas, materiales y existencias para trabajar con informacion real.' },
  { icon: ClipboardList, title: 'Pedidos y ventas', text: 'Guarda cotizaciones, convierte ventas y organiza la produccion de cada empresa.' },
  { icon: Users, title: 'Equipo con permisos', text: 'Administra accesos para administradores, ventas e inventario dentro de cada empresa.' },
]

export function PublicHome({ onLoginClick, theme, onToggleTheme }: PublicHomeProps) {
  return <main className={`public-site ${theme === 'dark' ? 'public-dark' : 'public-light'}`}>
    <nav className="public-nav">
      <a className="public-brand" href="#inicio"><span>{brandName}</span></a>
      <div><a href="#solucion">Solucion</a><a href="#empresas">Empresas</a><button type="button" className="public-theme-toggle" onClick={onToggleTheme}>{theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}{theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}</button><button type="button" className="ghost" onClick={onLoginClick}>Ingresar</button></div>
    </nav>
    <section id="inicio" className="public-hero">
      <div><p className="eyebrow">GESTION PARA IMPRESION 3D</p><h1>Atlas Technologies</h1><p className="lead">Una plataforma para empresas de impresion 3D que necesitan tasar, cotizar, organizar inventario, controlar pedidos y trabajar con su equipo desde un solo lugar.</p><div className="public-hero-actions"><button className="button-link" onClick={onLoginClick}>Acceso empresarial <ArrowRight size={18} /></button></div></div>
      <div className="public-hero-visual"><ShieldCheck size={54} /><span>Control claro para cada empresa.</span></div>
    </section>
    <section id="solucion" className="public-section">
      <div className="public-section-head"><p className="eyebrow">LA PLATAFORMA</p><h2>Todo lo necesario para operar una empresa de impresion 3D</h2></div>
      <div className="public-category-grid">{features.map(({ icon: Icon, title, text }) => <article key={title} className="public-category-card"><Icon size={26} /><h3>{title}</h3><p>{text}</p></article>)}</div>
    </section>
    <section id="empresas" className="public-section public-split">
      <div><p className="eyebrow">ESPACIOS INDEPENDIENTES</p><h2>Cada empresa conserva su propia informacion.</h2><p>Atlas Technologies permite crear espacios separados para cada negocio. Cada administrador maneja sus usuarios, su identidad visual, sus impresoras, sus precios y sus operaciones sin mezclar informacion con otras empresas.</p></div>
      <div className="public-info-list"><span><ShieldCheck size={20} />Datos separados por empresa</span><span><Users size={20} />Permisos segun el rol</span><span><Calculator size={20} />Cotizaciones listas para cliente</span></div>
    </section>
    <footer className="public-footer"><span>Atlas Technologies</span><button type="button" className="ghost" onClick={onLoginClick}>Acceso empresa</button></footer>
  </main>
}
