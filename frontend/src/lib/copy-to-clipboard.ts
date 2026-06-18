/**
 * Copy text that may arrive asynchronously while preserving the user-gesture
 * context required by the async Clipboard API (fetch-then-copy pattern).
 */
export async function copyTextFromPromise(getText: () => Promise<string>): Promise<void> {
  if (typeof ClipboardItem !== 'undefined' && navigator.clipboard?.write) {
    const item = new ClipboardItem({
      'text/plain': getText().then((text) => new Blob([text], { type: 'text/plain' })),
    });
    await navigator.clipboard.write([item]);
    return;
  }

  const text = await getText();
  await copyText(text);
}

export async function copyText(text: string): Promise<void> {
  if (navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);
      return;
    } catch {
      // Fall through to execCommand when focus or permissions block writeText.
    }
  }

  const textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.setAttribute('readonly', '');
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  document.body.appendChild(textarea);
  textarea.select();
  const copied = document.execCommand('copy');
  document.body.removeChild(textarea);
  if (!copied) throw new Error('Could not copy to clipboard');
}
