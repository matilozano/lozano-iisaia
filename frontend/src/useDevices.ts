import { useEffect, useRef, useState } from 'react';
import { request, type Command, type CommandResult, type Device } from './api';

export interface HistoryEntry { time: string; component: string; command: string; result: string }

export function useDevices() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [connected, setConnected] = useState<boolean | null>(null);
  const [error, setError] = useState('');
  const [connectionError, setConnectionError] = useState('');
  const [pending, setPending] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  function record(component: string, command: string, result: string) {
    setHistory(items => [{ time: new Date().toLocaleTimeString(), component, command, result }, ...items].slice(0, 100));
  }
  const generation = useRef(0);
  const busy = useRef(false);
  const stopping = useRef(false);

  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      if (!busy.current && !stopping.current) {
        const version = generation.current;
        try {
          const states = await request<Device[]>('devices');
          if (!disposed && version === generation.current) { setDevices(states); setConnected(true); setConnectionError(''); }
        } catch (e) {
          if (!disposed && version === generation.current) { setConnected(false); setConnectionError(e instanceof Error ? e.message : String(e)); }
        }
      }
      if (!disposed) timer = setTimeout(poll, 150);
    }
    void poll();
    return () => { disposed = true; clearTimeout(timer); };
  }, []);

  async function send(device: Device, command: Command) {
    if (busy.current || stopping.current) return;
    busy.current = true;
    const version = ++generation.current;
    setPending(`Enviando comando a ${device.name}…`); setError('');
    try {
      const result = await request<CommandResult>(`devices/${device.id}/commands`, command);
      if (!result.success) throw new Error('El backend no confirmó el comando.');
      record(device.id, [command.action, command.direction, command.speed === undefined ? '' : command.speed + '%'].filter(Boolean).join(' '), 'OK');
      if (version === generation.current) {
        setDevices(items => items.map(item => item.id === device.id ? { ...item, ...result.state } : item));
        setConnected(true); setConfirmation(`${device.name}: comando confirmado.`);
      }
    } catch (e) { record(device.id, command.action, 'ERROR'); if (version === generation.current) setError(e instanceof Error ? e.message : String(e)); }
    finally { busy.current = false; if (version === generation.current) setPending(''); }
  }

  async function stopAll() {
    if (stopping.current) return;
    stopping.current = true; generation.current++;
    setPending('Deteniendo todos los componentes…'); setError('');
    try {
      await request<void>('devices/stop-all', {});
      // HTTP 204 confirms the documented all-off/stopped outcome; polling then refreshes it.
      setDevices(items => items.map(item => ({ ...item, state: item.type === 'motor' ? 'stopped' : 'off', speed: 0, ...(item.type === 'light_bank' ? { channels: Array(8).fill(false), effect: 'NONE' } : {}) })));
      record('TODOS', 'STOP ALL', 'OK');
      setConnected(true); setConfirmation('Parada general confirmada. Luces y canales apagados; motor detenido.');
    } catch (e) { record('TODOS', 'STOP ALL', 'ERROR'); setError(e instanceof Error ? e.message : String(e)); }
    finally { stopping.current = false; setPending(''); }
  }
  return { devices, connected, error: error || connectionError, pending, confirmation, history, send, stopAll };
}
