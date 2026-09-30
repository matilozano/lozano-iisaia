import { useEffect, useState } from 'react';
import type { Command, Device } from './api';

const effects: [Command['action'], string][] = [
  ['SWEEP_RIGHT', '→ Barrido'], ['SWEEP_LEFT', '← Barrido'], ['PING_PONG', '⇄ Ida y vuelta'],
  ['BLINK', 'Parpadeo'], ['ALL_ON', 'Todos ON'], ['ALL_OFF', 'Todos OFF'], ['STOP_EFFECT', 'Detener efecto']
];
export function LightBank({ device, disabled, send }: { device: Device; disabled: boolean; send: (device: Device, command: Command) => void }) {
  const [speed, setSpeed] = useState(device.effectSpeed ?? 50);
  useEffect(() => setSpeed(device.effectSpeed ?? 50), [device.effectSpeed]);
  return <section className="bank" aria-label="Banco de iluminación">
    <p className="eyebrow">OCHO CANALES · ESTADO CONFIRMADO</p><h2>Banco de iluminación</h2>
    <div className="channels">{(device.channels ?? Array(8).fill(false)).map((on, index) => <div key={index}>
      <span>CH{String(index + 1).padStart(2, '0')}</span><div className={`lamp ${on ? 'on' : ''}`} role="img" aria-label={`Canal ${index + 1}: ${on ? 'ON' : 'OFF'}`} /><small>{on ? 'ON' : 'OFF'}</small>
    </div>)}</div>
    <p className="state">Efecto: {device.effect === 'NONE' ? 'Detenido' : device.effect} · Velocidad confirmada: {device.effectSpeed}%</p>
    <div className="bank-actions">{effects.map(([action, label]) => <button key={action} className="secondary" disabled={disabled} onClick={() => send(device, { action })}>{label}</button>)}</div>
    <label htmlFor="effect-speed">Velocidad solicitada <strong>{speed}%</strong></label>
    <input id="effect-speed" type="range" min="1" max="100" value={speed} disabled={disabled} onChange={e => setSpeed(Number(e.target.value))} />
    <button className="secondary" disabled={disabled} onClick={() => send(device, { action: 'SET_SPEED', speed })}>Aplicar velocidad del efecto</button>
    <p className="hint">Detener efecto conserva el patrón. Todos OFF apaga los ocho canales.</p>
  </section>;
}
