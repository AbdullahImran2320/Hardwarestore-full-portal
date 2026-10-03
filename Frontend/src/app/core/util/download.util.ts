import { HttpResponse } from '@angular/common/http';

// Saves a Blob as a file in the browser.
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Reads the file name the server suggested (Content-Disposition), or uses the fallback.
export function fileNameFromResponse(res: HttpResponse<Blob>, fallback: string): string {
  const header = res.headers.get('Content-Disposition');
  if (header) {
    const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header);
    if (encoded) {
      try {
        return decodeURIComponent(encoded[1]);
      } catch {
        // ignore and try the plain form
      }
    }
    const plain = /filename="?([^";]+)"?/i.exec(header);
    if (plain) return plain[1].trim();
  }
  return fallback;
}

export function saveResponse(res: HttpResponse<Blob>, fallbackName: string): void {
  if (!res.body) return;
  saveBlob(res.body, fileNameFromResponse(res, fallbackName));
}
