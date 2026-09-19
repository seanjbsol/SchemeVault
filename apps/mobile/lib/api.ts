import { getToken } from '@/lib/tokenStorage';

export const apiBaseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5080').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public requiredPlan?: string | null,
    public feature?: string | null,
  ) {
    super(message);
  }
}

type Options = {
  method?: string;
  body?: unknown;
  token?: string | null;
  auth?: boolean;
};

export async function api<T>(path: string, options: Options = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };
  let body: string | undefined;
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(options.body);
  }

  const token = options.auth === false ? null : (options.token ?? (await getToken()));
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body,
    });
  } catch {
    throw new ApiError(
      `Cannot reach the SchemeVault API at ${apiBaseUrl}. Is it running? On a physical device set EXPO_PUBLIC_API_URL to your machine's LAN address.`,
      0,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const data = text ? safeJson(text) : null;

  if (!response.ok) {
    if (response.status === 402) {
      const title = readTitle(data) ?? 'An active SchemeVault subscription is required.';
      const requiredPlan = readString(data, 'requiredPlan');
      const feature = readString(data, 'feature');
      throw new ApiError(title, response.status, requiredPlan, feature);
    }
    const title = readTitle(data) ?? `Request failed (${response.status})`;
    throw new ApiError(title, response.status);
  }

  return data as T;
}

function readString(data: unknown, key: string): string | null {
  if (data && typeof data === 'object' && key in data) {
    const value = (data as Record<string, unknown>)[key];
    return typeof value === 'string' && value.length > 0 ? value : null;
  }
  return null;
}

function readTitle(data: unknown): string | null {
  return readString(data, 'title');
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

export type UploadFile = {
  uri: string;
  name: string;
  type: string;
};

export async function uploadFile<T>(path: string, file: UploadFile, fields?: Record<string, string>): Promise<T> {
  const token = await getToken();
  const form = new FormData();
  if (fields) {
    for (const [key, value] of Object.entries(fields)) {
      form.append(key, value);
    }
  }

  const isBlobUri = file.uri.startsWith('blob:') || file.uri.startsWith('data:') || file.uri.startsWith('http');
  if (isBlobUri) {
    const res = await fetch(file.uri);
    const blob = await res.blob();
    form.append('file', blob, file.name);
  } else {
    form.append('file', { uri: file.uri, name: file.name, type: file.type } as unknown as Blob);
  }

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: form,
    });
  } catch {
    throw new ApiError(`Cannot reach the SchemeVault API at ${apiBaseUrl}.`, 0);
  }

  if (response.status === 204) {
    return undefined as T;
  }
  const text = await response.text();
  const data = text ? safeJson(text) : null;
  if (!response.ok) {
    throw new ApiError(readTitle(data) ?? `Upload failed (${response.status})`, response.status, readString(data, 'requiredPlan'), readString(data, 'feature'));
  }
  return data as T;
}

export async function downloadAuthorizedFile(path: string, filename: string): Promise<void> {
  const token = await getToken();
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      Accept: 'application/pdf,application/octet-stream',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
  if (!response.ok) {
    const text = await response.text();
    const data = text ? safeJson(text) : null;
    throw new ApiError(readTitle(data) ?? `Download failed (${response.status})`, response.status, readString(data, 'requiredPlan'));
  }
  if (typeof document === 'undefined' || typeof URL.createObjectURL !== 'function') {
    throw new ApiError('PDF download is available in the Expo web app for this demo. The generated markdown is shown on this screen.', 0);
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

