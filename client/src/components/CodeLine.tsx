import type { ReactNode } from 'react'

// Küçük bir C# sözdizimi renklendirici. Kütüphane eklemeden kod parçalarını okunur kılar.

const keywords = new Set([
  'abstract', 'as', 'async', 'await', 'base', 'bool', 'break', 'case', 'catch', 'class', 'const', 'continue',
  'decimal', 'default', 'do', 'double', 'else', 'enum', 'false', 'finally', 'for', 'foreach', 'get', 'if',
  'in', 'init', 'int', 'interface', 'internal', 'is', 'lock', 'namespace', 'new', 'not', 'null', 'object',
  'out', 'override', 'private', 'protected', 'public', 'readonly', 'record', 'required', 'return', 'sealed',
  'set', 'static', 'string', 'switch', 'this', 'throw', 'true', 'try', 'using', 'var', 'virtual', 'void',
  'while', 'with',
])

const tokenPattern = /(\/\/.*$)|(\$?@?"(?:[^"\\]|\\.)*")|(\b\d[\d_]*(?:\.\d+)?[mMfFdDlL]?\b)|(\b[A-Za-z_]\w*\b)|(\s+)|(.)/g

export function highlightCSharp(line: string): ReactNode[] {
  const nodes: ReactNode[] = []
  let match: RegExpExecArray | null
  let key = 0

  tokenPattern.lastIndex = 0
  while ((match = tokenPattern.exec(line)) !== null) {
    const [token, comment, str, num, word] = match
    if (comment) nodes.push(<span key={key++} className="tok-comment">{token}</span>)
    else if (str) nodes.push(<span key={key++} className="tok-string">{token}</span>)
    else if (num) nodes.push(<span key={key++} className="tok-number">{token}</span>)
    else if (word && keywords.has(word)) nodes.push(<span key={key++} className="tok-keyword">{token}</span>)
    else if (word && /^[A-Z]/.test(word)) nodes.push(<span key={key++} className="tok-type">{token}</span>)
    else nodes.push(token)
  }

  return nodes
}

/** Metnin kod gibi görünüp görünmediğini tahmin eder (şıkları monospace göstermek için). */
export function looksLikeCode(text: string): boolean {
  return (
    /^(app|builder|services|db|var|await|\(await|public)\b/.test(text) ||
    /^(GET|POST|PUT|PATCH|DELETE) \//.test(text) ||
    /=>|\);$/.test(text)
  )
}
