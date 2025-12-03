/**
 * Parse frontmatter from markdown document
 * Extracts YAML-like properties between --- markers
 * 
 * Example:
 * ---
 * title: ОТЧЕТ О ПРАКТИЧЕСКОЙ РАБОТЕ
 * student_id: "032319377"
 * author: Буданов К.Д.
 * ---
 * 
 * Content here...
 */
export function parseFrontmatter(markdown: string): {
  variables: Record<string, string>;
  content: string;
} {
  const trimmed = markdown.trim();
  
  // Check if document starts with frontmatter delimiter
  if (!trimmed.startsWith('---')) {
    return {
      variables: {},
      content: markdown,
    };
  }

  // Find the end of frontmatter (second ---)
  const firstDelimiterEnd = trimmed.indexOf('\n', 3);
  if (firstDelimiterEnd === -1) {
    // No newline after first delimiter, no frontmatter
    return {
      variables: {},
      content: markdown,
    };
  }

  const secondDelimiterStart = trimmed.indexOf('\n---', firstDelimiterEnd);
  if (secondDelimiterStart === -1) {
    // No second delimiter found
    return {
      variables: {},
      content: markdown,
    };
  }

  // Extract frontmatter content (between first --- and second ---)
  const frontmatterText = trimmed.substring(
    firstDelimiterEnd + 1,
    secondDelimiterStart
  ).trim();

  // Extract content (after second ---)
  const content = trimmed.substring(secondDelimiterStart + 5).trim();

  // Parse frontmatter properties
  const variables: Record<string, string> = {};
  const lines = frontmatterText.split('\n');

  for (const line of lines) {
    const trimmedLine = line.trim();
    if (!trimmedLine || trimmedLine.startsWith('#')) {
      // Skip empty lines and comments
      continue;
    }

    // Match key: value pattern
    const match = trimmedLine.match(/^([^:]+):\s*(.+)$/);
    if (match) {
      const key = match[1].trim();
      let value = match[2].trim();

      // Remove quotes if present (both single and double)
      if (
        (value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'"))
      ) {
        value = value.slice(1, -1);
      }

      variables[key] = value;
    }
  }

  return {
    variables,
    content,
  };
}

/**
 * Generate frontmatter YAML from variables and merge with existing content
 */
export function generateFrontmatter(
  variables: Record<string, string>,
  existingContent: string
): string {
  const { content } = parseFrontmatter(existingContent);
  
  if (Object.keys(variables).length === 0) {
    return content;
  }
  
  const frontmatterLines = Object.entries(variables)
    .filter(([key]) => key.trim())
    .map(([key, value]) => {
      // Escape value if it contains special characters or needs quotes
      const needsQuotes = 
        value.includes(':') || 
        value.includes('\n') || 
        value.trim() !== value ||
        value.includes('#') ||
        value.includes('|');
      
      if (needsQuotes) {
        // Escape quotes in value
        const escapedValue = value.replace(/"/g, '\\"');
        return `${key}: "${escapedValue}"`;
      }
      
      return `${key}: ${value}`;
    });
  
  return `---\n${frontmatterLines.join('\n')}\n---\n\n${content}`;
}

/**
 * Extract variable keys from title page elements
 */
export function getTitlePageVariables(titlePage: { elements: Array<{ type: string; variableKey?: string }> }): string[] {
  if (!titlePage || !titlePage.elements) {
    return [];
  }
  
  const variableKeys = new Set<string>();
  
  for (const element of titlePage.elements) {
    if (element.type === 'variable' && element.variableKey) {
      variableKeys.add(element.variableKey);
    }
  }
  
  return Array.from(variableKeys);
}

/**
 * Validate frontmatter YAML syntax (basic validation)
 */
export function validateFrontmatter(markdown: string): {
  valid: boolean;
  error?: string;
} {
  const trimmed = markdown.trim();
  
  // Check if document starts with frontmatter delimiter
  if (!trimmed.startsWith('---')) {
    return { valid: true }; // No frontmatter is valid
  }
  
  // Find the end of frontmatter (second ---)
  const firstDelimiterEnd = trimmed.indexOf('\n', 3);
  if (firstDelimiterEnd === -1) {
    return { valid: false, error: 'Frontmatter не закрыт' };
  }
  
  const secondDelimiterStart = trimmed.indexOf('\n---', firstDelimiterEnd);
  if (secondDelimiterStart === -1) {
    return { valid: false, error: 'Frontmatter не закрыт (отсутствует второй ---)' };
  }
  
  // Extract frontmatter content
  const frontmatterText = trimmed.substring(
    firstDelimiterEnd + 1,
    secondDelimiterStart
  ).trim();
  
  // Basic validation: check for common YAML errors
  const lines = frontmatterText.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    if (!line || line.startsWith('#')) {
      continue; // Skip empty lines and comments
    }
    
    // Check for key: value pattern
    if (!line.includes(':')) {
      return { 
        valid: false, 
        error: `Строка ${i + 1}: отсутствует двоеточие в определении переменной` 
      };
    }
    
    // Check for proper key: value format
    const colonIndex = line.indexOf(':');
    if (colonIndex === 0) {
      return { 
        valid: false, 
        error: `Строка ${i + 1}: отсутствует ключ переменной` 
      };
    }
  }
  
  return { valid: true };
}

