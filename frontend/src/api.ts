export interface Device {
  id: string;
  name: string;
  type: 'light' | 'motor';
  state: 'on' | 'off' | 'running' | 'stopped';
  online: boolean;
  direction: 'forward' | 'reverse' | null;
  speed: number;
}
export interface Command { action: 'on' | 'off' | 'start' | 'stop'; direction?: 'forward' | 'reverse'; speed?: number }
export interface CommandResult { deviceId: string; success: boolean; state: Omit<Device, 'type' | 'name'>; executedAt: string }

export async function request<T>(path: string, command?: unknown): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`/api/${path}`, {
      method: command === undefined ? 'GET' : 'POST',
      headers: command === undefined ? {} : { 'Content-Type': 'application/json' },
      body: command === undefined ? undefined : JSON.stringify(command),
      signal: AbortSignal.timeout(5000)
    });
  } catch { throw new Error('No se pudo contactar a la API. Verificá que el backend esté iniciado.'); }
  if (!response.ok) {
    const error = await response.json().catch(() => null);
    throw new Error(error?.message ?? `La API respondió con error ${response.status}.`);
  }
  return response.status === 204 ? undefined as T : response.json();
}
