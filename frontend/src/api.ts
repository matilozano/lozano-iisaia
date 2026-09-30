export interface Device {
  id: string;
  name: string;
  type: 'light' | 'motor' | 'light_bank' | 'HYDRAULIC_ACTUATOR' | 'SERVO';
  state: 'on' | 'off' | 'running' | 'stopped';
  online: boolean;
  direction: 'forward' | 'reverse' | null;
  speed: number;
  position?: number;
  movement?: 'STOPPED' | 'EXTENDING' | 'RETRACTING';
  limitExtended?: boolean;
  limitRetracted?: boolean;
  channels?: boolean[];
  effect?: string;
  effectSpeed?: number;
}
export interface Command { action: 'on' | 'off' | 'start' | 'stop' | 'ALL_ON' | 'ALL_OFF' | 'SWEEP_RIGHT' | 'SWEEP_LEFT' | 'PING_PONG' | 'BLINK' | 'STOP_EFFECT' | 'SET_SPEED' | 'EXTEND' | 'RETRACT' | 'STOP' | 'SET_POSITION'; direction?: 'forward' | 'reverse'; speed?: number; position?: number }
export interface CommandResult { deviceId: string; success: boolean; state: Omit<Device, 'type' | 'name'>; executedAt: string }

export class ApiError extends Error { constructor(message: string, public code: string) { super(message); } }

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
    throw new ApiError(error?.message ?? `La API respondió con error ${response.status}.`, error?.error ?? String(response.status));
  }
  return response.status === 204 ? undefined as T : response.json();
}
