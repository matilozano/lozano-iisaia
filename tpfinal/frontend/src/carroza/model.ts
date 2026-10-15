import type { Device } from '../api';
import type { SequenceExecution } from '../Sequences';

// Pure presentation mapping. No clocks, commands, extrapolation or sequence steps.
export function toCarrozaModel(devices: readonly Device[], connected: boolean | null, execution: SequenceExecution | null) {
  const find = (id: string) => devices.find(device => device.id === id);
  const motor = find('main-motor');
  const hydraulic = find('hydraulic-1');
  const servo = find('servo-1');
  const bank = find('main-light-bank');
  const speed = motor?.speed ?? 0;
  return {
    api: connected === null ? 'CONECTANDO' : connected ? 'ONLINE' : 'OFFLINE',
    stale: connected !== true,
    front: find('front-lights')?.state === 'on',
    side: find('side-lights')?.state === 'on',
    channels: Array.from({ length: 8 }, (_, index) => bank?.channels?.[index] === true),
    effect: bank?.effect ?? 'SIN DATOS',
    motor: motor?.state ?? 'SIN DATOS', speed,
    direction: motor?.direction ?? '—',
    moving: motor?.state === 'running' && speed > 0,
    wheelSeconds: 60 / Math.max(1, speed),
    wheelDirection: motor?.direction === 'reverse' ? 'reverse' as const : 'normal' as const,
    position: hydraulic?.position ?? null,
    lift: (hydraulic?.position ?? 0) * 1.1,
    movement: hydraulic?.movement ?? 'SIN DATOS',
    angle: servo?.position ?? null,
    sequence: execution?.sequenceId ?? '—',
    sequenceStatus: execution?.status ?? 'SIN DATOS',
    offline: devices.filter(device => !device.online).map(device => device.name)
  };
}
export type CarrozaModel = ReturnType<typeof toCarrozaModel>;
