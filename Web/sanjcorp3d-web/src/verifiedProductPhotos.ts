const photos: Record<string, string> = {
  'printer|bambu lab|h2d': 'https://tse2.mm.bing.net/th?id=OIP.KkSKmF2AiMyKD4H0DH5iqQHaG4&w=640&h=480&c=7&rs=1',
  'printer|creality|k2 plus': 'https://tse2.mm.bing.net/th?id=OIP.uw0JjbeFBYPzKcl75JXzzQHaHa&w=640&h=480&c=7&rs=1',
  'printer|original prusa|mk4s': 'https://www.prusa3d.com/content/images/product/7576e91e-08c1-4fc3-8b66-88a06868deb0.jpg',
  'printer|elegoo|saturn 4 ultra 16k': 'https://tse2.mm.bing.net/th?id=OIP.H06wssMTM5i76c8sC-Y7MQHaHa&w=640&h=480&c=7&rs=1',
  'printer|anycubic|kobra s1 combo': 'https://tse2.mm.bing.net/th?id=OIP.wWun9VN4kb1jNHd41R1bKAHaHa&w=640&h=480&c=7&rs=1',
  'filament|polymaker|panchroma basic pla|verde oliva': 'https://tse2.mm.bing.net/th?id=OIP.IZY90PhZVz1k58G21vHQjAHaHa&w=640&h=480&c=7&rs=1',
  'filament|esun|epla+|verde': 'https://tse2.mm.bing.net/th?id=OIP.ny0f-iTTOSayUjqxV9X_cQHaHa&w=640&h=480&c=7&rs=1',
  'filament|sunlu|pla+|negro': 'https://tse2.mm.bing.net/th?id=OIP.w9t0yqR6lX4YUIK6wFrJtAHaHa&w=640&h=480&c=7&rs=1',
}

const normalize = (value = '') => value.trim().toLocaleLowerCase().replaceAll('™', '').replaceAll('®', '').replace(/\s+/g, ' ')

export function verifiedProductPhoto(kind: 'printer' | 'filament' | 'resin', brand: string, modelOrMaterial: string, colorName = '') {
  const key = kind === 'printer'
    ? `printer|${normalize(brand)}|${normalize(modelOrMaterial)}`
    : `${kind}|${normalize(brand)}|${normalize(modelOrMaterial)}|${normalize(colorName)}`
  return photos[key]
}
