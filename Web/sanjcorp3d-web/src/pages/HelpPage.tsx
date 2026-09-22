import { BookOpen, Boxes, Calculator, DatabaseBackup, KeyRound, PackageSearch, ShoppingBag } from 'lucide-react'
import { PageHeader } from '../ui'

const topics = [
  { icon: Calculator, title: 'Crear una cotización', text: 'Completa cliente, proyecto, impresora y duración. Agrega cada filamento o resina con sus gramos, añade materiales extra y pulsa Calcular. Cuando el resultado sea correcto, guárdalo.' },
  { icon: ShoppingBag, title: 'Confirmar una venta', text: 'Después de guardar una cotización pulsa Confirmar venta. También puedes buscarla en Historial y confirmarla desde su detalle. Solo se registra una venta por cotización.' },
  { icon: PackageSearch, title: 'Controlar existencias', text: 'En Filamentos y resinas abre Editar para indicar kilos y gramos, además del umbral de alarma. Una venta descuenta automáticamente los gramos usados; un consumible agotado no puede usarse en una cotización nueva.' },
  { icon: Boxes, title: 'Conservar el historial', text: 'Al archivar impresoras, consumibles o materiales dejan de aparecer en nuevas cotizaciones, pero las cotizaciones anteriores mantienen sus nombres, precios y costos.' },
  { icon: KeyRound, title: 'Proteger una cuenta', text: 'Cada persona debe usar su propio acceso. En Configuración → Seguridad puede activar un segundo factor y guardar sus códigos de recuperación.' },
  { icon: DatabaseBackup, title: 'Respaldar los datos', text: 'Un administrador puede descargar un respaldo JSON desde Configuración. El archivo contiene los datos operativos, pero nunca contraseñas ni secretos de autenticación.' },
]

export function HelpPage() {
  return <><PageHeader eyebrow="GUÍA RÁPIDA" title="Ayuda" description="Flujos principales del cotizador web de Atlas Impresiones 3D." /><div className="help-grid">{topics.map(({ icon: Icon, title, text }, index) => <article className="panel help-card" key={title}><div className="help-number">{String(index + 1).padStart(2, '0')}</div><Icon size={24} /><h2>{title}</h2><p>{text}</p></article>)}</div><section className="panel formula-help"><BookOpen size={24} /><div><h2>Fórmula utilizada</h2><p>El sistema conserva el cálculo del programa original: costo de consumibles + electricidad + mantenimiento + adicionales; después aplica el multiplicador de ganancia, el impuesto y el redondeo configurado.</p></div></section></>
}


