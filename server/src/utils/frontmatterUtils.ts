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

