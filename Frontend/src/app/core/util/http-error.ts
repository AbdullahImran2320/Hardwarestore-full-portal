import { HttpErrorResponse } from '@angular/common/http';

const DEFAULT_MESSAGE = 'Something went wrong. Please try again.';

// Turns an HTTP error into a message that can be shown to the user.
export function httpErrorMessage(err: unknown, fallback: string = DEFAULT_MESSAGE): string {
  const e = err as HttpErrorResponse;
  if (e && e.status === 0) return 'Cannot reach the server. Is the portal running?';
  if (e && e.status === 403) return 'You do not have permission to do this.';

  const msg = e && e.error ? e.error.message : null;
  return typeof msg === 'string' && msg.length > 0 ? msg : fallback;
}

// Same, but also understands errors that came back from a file download (the body is a Blob).
export async function httpErrorMessageAsync(err: unknown, fallback: string = DEFAULT_MESSAGE): Promise<string> {
  const e = err as HttpErrorResponse;
  if (e && e.error instanceof Blob) {
    try {
      const text = await e.error.text();
      const parsed = JSON.parse(text);
      if (parsed && typeof parsed.message === 'string') return parsed.message;
    } catch {
      // not JSON, fall through
    }
    return httpErrorMessage({ status: e.status } as HttpErrorResponse, fallback);
  }
  return httpErrorMessage(err, fallback);
}
