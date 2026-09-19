import { getToken } from '@/lib/tokenStorage';

export const apiBaseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5080').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
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
      throw new ApiError(title, response.status);
    }
    const title = readTitle(data) ?? `Request failed (${response.status})`;
    throw new ApiError(title, response.status);
  }

  return data as T;
}

function readTitle(data: unknown): string | null {
  if (data && typeof data === 'object' && 'title' in data) {
    const title = (data as { title?: unknown }).title;
    return typeof title === 'string' && title.length > 0 ? title : null;
  }
  return null;
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}
