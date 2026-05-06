/**
 * Parse a CSV/TSV text into field option rows.
 * Expects up to two columns: key, label.
 * If only one column is present, key and label are the same value.
 * Auto-skips a header row when the first cell looks like a column name.
 */
export function parseOptionsCsv(text) {
  const lines = text.replace(/\r/g, '').split('\n')
  const rows = []

  for (const line of lines) {
    if (!line.trim()) continue
    const sep  = line.includes('\t') ? '\t' : ','
    const cols = line.split(sep).map(c => c.trim().replace(/^"(.*)"$/, '$1').trim())
    const key  = cols[0]
    if (!key) continue
    rows.push({ key, label: cols[1] ?? key })
  }

  // Skip header row when first cell looks like a column name
  if (rows.length > 0) {
    const first = rows[0].key.toLowerCase().replace(/[\s_-]/g, '')
    if (['key', 'optionkey', 'value', 'code', 'id', 'abbr', 'abbreviation', 'label', 'name'].includes(first)) {
      rows.shift()
    }
  }

  return rows.map((r, i) => ({ optionKey: r.key, optionLabel: r.label, displayOrder: i + 1 }))
}
