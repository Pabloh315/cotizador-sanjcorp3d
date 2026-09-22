import { useEffect, useState } from 'react'
import { ArrowRight, Building2, Clock, GraduationCap, Gift, ImagePlus, MapPin, Moon, Phone, Sparkles, Sun, Wrench } from 'lucide-react'

const brandName = 'Atlas Impresiones 3D'
const lightLogo = '/logo.png'
const darkLogo = '/logoblanco.png'
const galleryKey = 'atlas.public.gallery'

type PublicTheme = 'light' | 'dark'
type PublicHomeProps = { onLoginClick: () => void; theme: PublicTheme; onToggleTheme: () => void }
type GalleryImage = { id: string; src: string; name: string }

const categories = [
  { title: 'Atlas 3D Estilo', description: 'Souvenirs, figuras de acción, regalos, decoración y piezas con personalidad.', icon: Sparkles },
  { title: 'Atlas 3D Recuerdo', description: 'Detalles para empresas, llaveros, estatuillas, recuerdos corporativos y presentes especiales.', icon: Gift },
  { title: 'Atlas 3D Educación', description: 'Recursos para universidades, colegios, maquetas, modelos didácticos y material académico.', icon: GraduationCap },
  { title: 'Atlas 3D Empresarial', description: 'Piezas personalizadas para maquinaria, prototipos, repuestos y soluciones técnicas.', icon: Wrench },
]

export function PublicHome({ onLoginClick, theme, onToggleTheme }: PublicHomeProps) {
  const [images, setImages] = useState<GalleryImage[]>([])
  const logo = theme === 'dark' ? darkLogo : lightLogo

  useEffect(() => {
    try {
      const stored = localStorage.getItem(galleryKey)
      if (stored) setImages(JSON.parse(stored) as GalleryImage[])
    } catch { setImages([]) }
  }, [])

  function saveImages(next: GalleryImage[]) {
    setImages(next)
    localStorage.setItem(galleryKey, JSON.stringify(next))
  }

  function uploadImages(files: FileList | null) {
    const selected = Array.from(files ?? []).filter(file => file.type === 'image/png' || file.type === 'image/jpeg')
    if (selected.length === 0) return
    Promise.all(selected.map(file => new Promise<GalleryImage>(resolve => {
      const reader = new FileReader()
      reader.onload = () => resolve({ id: `${file.name}-${Date.now()}-${Math.random()}`, src: String(reader.result), name: file.name })
      reader.readAsDataURL(file)
    }))).then(next => saveImages([...images, ...next].slice(-12)))
  }

  return <main className={`public-site ${theme === 'dark' ? 'public-dark' : 'public-light'}`}>
    <nav className="public-nav">
      <a className="public-brand" href="#inicio"><img src={logo} alt={brandName} /><span>{brandName}</span></a>
      <div><a href="#productos">Productos</a><a href="#servicios">Servicios</a><a href="#contacto">Contacto</a><button type="button" className="public-theme-toggle" onClick={onToggleTheme}>{theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}{theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}</button></div>
    </nav>

    <section id="inicio" className="public-hero">
      <div>
        <p className="eyebrow">IMPRESIÓN 3D EN SANTA CRUZ, BOLIVIA</p>
        <h1>Productos, recuerdos y soluciones 3D hechas a medida.</h1>
        <p className="lead">Creamos piezas personalizadas, productos de tienda, detalles empresariales y soluciones técnicas con envío a nivel nacional.</p>
        <div className="public-hero-actions"><a className="button-link" href="#productos">Ver categorías <ArrowRight size={18} /></a><a className="button-link secondary-link" href="#contacto">Contactar</a></div>
      </div>
      <div className="public-hero-visual"><img src={logo} alt="Logo Atlas Impresiones 3D" /><span>Atlas Impresiones 3D</span></div>
    </section>

    <section id="productos" className="public-section">
      <div className="public-section-head"><p className="eyebrow">CATÁLOGO</p><h2>Cuatro líneas para diferentes necesidades</h2></div>
      <div className="public-category-grid">{categories.map(({ title, description, icon: Icon }) => <article key={title} className="public-category-card"><Icon size={26} /><h3>{title}</h3><p>{description}</p></article>)}</div>
    </section>

    <section id="servicios" className="public-section public-split">
      <div><p className="eyebrow">ESPECIALIZACIÓN</p><h2>Desde regalos personalizados hasta piezas funcionales.</h2><p>Atendemos trabajos únicos, producción por cantidad, piezas para instituciones educativas y proyectos empresariales. Las cotizaciones se manejan internamente por nuestro equipo para cuidar costos, tiempos y materiales.</p></div>
      <div className="public-info-list"><span><Clock size={20} />Horario: 8:00 a. m. a 6:00 p. m.</span><span><MapPin size={20} />Santa Cruz, Bolivia</span><span><Building2 size={20} />Envíos a nivel nacional</span></div>
    </section>

    <section className="public-section">
      <div className="public-section-head"><p className="eyebrow">GALERÍA</p><h2>Fotos de productos y trabajos</h2></div>
      <label className="public-upload"><ImagePlus size={24} /><span><strong>Subir imágenes PNG o JPG</strong><small>Vista previa local para preparar la vitrina. Puedes cargar fotos de productos, regalos o piezas terminadas.</small></span><input type="file" accept="image/png,image/jpeg" multiple onChange={event => uploadImages(event.target.files)} /></label>
      {images.length === 0 ? <div className="public-empty-gallery">Sube tus primeras fotos para ver cómo quedará la galería.</div> : <div className="public-gallery">{images.map(image => <figure key={image.id}><img src={image.src} alt={image.name} /><figcaption>{image.name}</figcaption></figure>)}</div>}
    </section>

    <section id="contacto" className="public-contact">
      <div><p className="eyebrow">CONTACTO</p><h2>Cuéntanos qué pieza necesitas imprimir.</h2><p>Atendemos desde Santa Cruz y enviamos a toda Bolivia. Puedes agregar aquí WhatsApp, teléfono y redes cuando los tengas listos.</p></div>
      <div className="public-contact-card"><Phone size={22} /><strong>Datos pendientes</strong><span>WhatsApp, teléfono y redes sociales se pueden completar luego.</span></div>
    </section>

    <footer className="public-footer"><span>Atlas Impresiones 3D - Santa Cruz, Bolivia</span><button type="button" className="ghost" onClick={onLoginClick}>Acceso empresa</button></footer>
  </main>
}
