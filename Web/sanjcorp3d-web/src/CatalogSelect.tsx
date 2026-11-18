type Props = {
  label: string
  value: string
  options: string[]
  onChange: (value: string) => void
  customLabel?: string
  placeholder?: string
  required?: boolean
}

export function CatalogSelect({ label, value, options, onChange, customLabel = 'Otro / personalizado', placeholder = 'Escribe el valor', required = true }: Props) {
  const known = options.includes(value)
  return <label>{label}
    <select value={known ? value : '__custom'} onChange={event => onChange(event.target.value === '__custom' ? '' : event.target.value)}>
      {options.map(option => <option key={option} value={option}>{option}</option>)}
      <option value="__custom">{options.length === 0 ? 'Sin opciones: registrar manualmente' : customLabel}</option>
    </select>
    {!known && <input required={required} value={value} onChange={event => onChange(event.target.value)} placeholder={placeholder} />}
  </label>
}
