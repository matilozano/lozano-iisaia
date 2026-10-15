import type { Command, Device } from '../api';
export interface Step { id: string; order: number; delayMs: number; componentId: string; command: Command; description?: string }
export interface Definition { id: string; name: string; description: string; steps: Step[]; revision?: string | null }
export function actions(type: Device['type']): Command['action'][] {
  switch (type) {
    case 'light': return ['on', 'off'];
    case 'motor': return ['start', 'stop'];
    case 'light_bank': return ['ALL_ON', 'ALL_OFF', 'SWEEP_RIGHT', 'SWEEP_LEFT', 'PING_PONG', 'BLINK', 'STOP_EFFECT', 'SET_SPEED'];
    case 'HYDRAULIC_ACTUATOR': return ['EXTEND', 'RETRACT', 'STOP'];
    case 'SERVO': return ['SET_POSITION'];
  }
}
export function initialCommand(action: Command['action']): Command {
  return action === 'start' ? { action, direction: 'forward', speed: 40 }
    : action === 'SET_SPEED' ? { action, speed: 50 }
    : action === 'SET_POSITION' ? { action, position: 90 } : { action };
}
export function reorder<T extends { id: string }>(items: T[], from: string, to: string): T[] {
  const start = items.findIndex(i => i.id === from), end = items.findIndex(i => i.id === to);
  if (start < 0 || end < 0 || start === end) return items;
  const result = [...items]; result.splice(end, 0, result.splice(start, 1)[0]); return result;
}
export function normalizeSteps(steps: Step[]): Step[] { return steps.map((s, i) => ({ ...s, order: i + 1 })); }
