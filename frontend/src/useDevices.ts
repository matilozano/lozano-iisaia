import type { SequenceExecution, SequenceEvent } from './Sequences';
import { useEffect, useRef, useState } from 'react';
import { ApiError, request, type Command, type CommandResult, type Device } from './api';

export interface HistoryEntry { origin: 'MANUAL' | 'SEQUENCE'; timestamp: number; time: string; component: string; command: string; result: string }

export function useDevices() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [connected, setConnected] = useState<boolean | null>(null);
  const [error, setError] = useState('');
  const [connectionError, setConnectionError] = useState('');
  const [componentErrors, setComponentErrors] = useState<Record<string, string>>({});
  const [pending, setPending] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  function record(component: string, command: string, result: string) {
    setHistory(items => [{ origin: 'MANUAL' as const, timestamp: Date.now(), time: new Date().toLocaleTimeString(), component, command, result }, ...items].slice(0, 100));
  }
  const [execution, setExecution] = useState<SequenceExecution | null>(null);
  const [sequencePending, setSequencePending] = useState(false);
  const [sequenceError, setSequenceError] = useState('');
  const seenEvents = useRef(new Set<string>());
  const sessionStarted = useRef(Date.now());
  function applySequence(current: SequenceExecution, events: SequenceEvent[]) {
    setExecution(current);
    const fresh = events.filter(event => {
      const key = `${event.runId}/${event.id}`;
      if (seenEvents.current.has(key) || Date.parse(event.time) < sessionStarted.current) return false;
      seenEvents.current.add(key); return true;
    }).map(event => ({ origin: 'SEQUENCE' as const, timestamp: Date.parse(event.time), time: new Date(event.time).toLocaleTimeString(), component: event.componentId,
      command: [event.command.action, event.command.direction, event.command.speed === null || event.command.speed === undefined ? '' : event.command.speed + '%', event.command.position === null || event.command.position === undefined ? '' : event.command.position + '°'].filter(Boolean).join(' '), result: event.result }));
    if (fresh.length) setHistory(items => [...items, ...fresh].sort((a,b) => b.timestamp - a.timestamp).slice(0,100));
  }
  async function sequenceAction(action: 'SHOW_FNE/start' | 'cancel') {
    if (busy.current || stopping.current) return;
    busy.current = true; const version = ++generation.current;
    setSequencePending(true); setSequenceError('');
    try {
      const result = await request<SequenceExecution>(`sequences/${action}`, {});
      if (version === generation.current) setExecution(result);
      record(result.sequenceId ?? 'SEQUENCES', action, 'OK');
    }
    catch (error) { setSequenceError(error instanceof Error ? error.message : String(error)); record('SEQUENCES', action, 'ERROR'); }
    finally { busy.current = false; setSequencePending(false); }
  }  const generation = useRef(0);
  const busy = useRef(false);
  const stopping = useRef(false);

  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      if (!busy.current && !stopping.current) {
        const version = generation.current;
        try {
          const [states, current, events] = await Promise.all([request<Device[]>('devices'), request<SequenceExecution>('sequences/execution'), request<SequenceEvent[]>('sequences/events')]);
          if (!disposed && version === generation.current) { setDevices(states); applySequence(current, events); setConnected(true); setConnectionError(''); }
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
    if (busy.current || stopping.current || execution?.status === 'RUNNING') return;
    busy.current = true;
    const version = ++generation.current;
    setPending(`Enviando comando a ${device.name}…`); setError('');
    try {
      const result = await request<CommandResult>(`devices/${device.id}/commands`, command);
      if (!result.success) throw new Error('El backend no confirmó el comando.');
      record(device.id, [command.action, command.direction, command.speed === undefined ? '' : command.speed + '%', command.position === undefined ? '' : command.position + '°'].filter(Boolean).join(' '), 'OK');
      if (version === generation.current) {
        setDevices(items => items.map(item => item.id === device.id ? { ...item, ...result.state } : item));
        setComponentErrors(errors => ({ ...errors, [device.id]: '' }));
        setConnected(true); setConfirmation(`${device.name}: comando confirmado.`);
      }
    } catch (e) {
      const message = `${device.name} (${device.id}): ${e instanceof Error ? e.message : String(e)}`;
      record(device.id, command.action, e instanceof ApiError ? e.code : 'ERROR');
      setComponentErrors(errors => ({ ...errors, [device.id]: message }));
      if (version === generation.current) {
        setError(message);
        if (e instanceof ApiError && e.code === 'DEVICE_OFFLINE')
          setDevices(items => items.map(item => item.id === device.id ? { ...item, online: false } : item));
      }
    }
    finally { busy.current = false; if (version === generation.current) setPending(''); }
  }

  async function stopAll() {
    if (stopping.current) return;
    stopping.current = true; generation.current++;
    setPending('Deteniendo todos los componentes…'); setError('');
    try {
      const states = await request<Device[]>('devices/stop-all?includeState=true', {});
      setDevices(states);
      record('TODOS', 'STOP ALL', 'OK');
      setConnected(true); setConfirmation('Parada general confirmada. Luces y canales apagados; motor y actuador detenidos; servo conserva posición.');
    } catch (e) { record('TODOS', 'STOP ALL', 'ERROR'); setError(e instanceof Error ? e.message : String(e)); }
    finally { stopping.current = false; setPending(''); }
  }
  return { execution, sequencePending, sequenceError, startSequence: () => sequenceAction('SHOW_FNE/start'), cancelSequence: () => sequenceAction('cancel'), devices, connected, error: error || connectionError, pending, confirmation, history, componentErrors, send, stopAll };
}
