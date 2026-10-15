import { useEffect, useState } from 'react';
import type { Command, Device } from './api';

type Props = { device: Device; disabled: boolean; send: (device: Device, command: Command) => void };
export function HydraulicControls({ device, disabled, send }: Props) {
  const position = device.position ?? 0;
  return <>
    <svg className="mechanism" viewBox="0 0 320 150" role="img" aria-label={`Pistón: ${position.toFixed(1)}%, ${device.movement}`}>
      <path d="M20 130H300" stroke="#526672" strokeWidth="6" />
      <rect x="25" y="48" width="125" height="60" rx="8" fill="#243744" stroke="#81cbb6" strokeWidth="3" />
      <rect x="145" y="67" width={20 + position * 1.1} height="22" fill="#a3b6be" />
      <rect x={160 + position * 1.1} y="54" width="12" height="48" rx="3" fill="#8bdec0" />
    </svg>
    <p className="state">{position.toFixed(1)}% · {device.movement}</p>
    <div className="actions"><button disabled={disabled} onClick={() => send(device, { action: 'RETRACT' })}>Retraer</button><button disabled={disabled} className="secondary" onClick={() => send(device, { action: 'STOP' })}>STOP</button><button disabled={disabled} onClick={() => send(device, { action: 'EXTEND' })}>Extender</button></div>
    <p className="hint">{device.limitRetracted ? 'Límite retraído' : device.limitExtended ? 'Límite extendido' : 'Posición intermedia'} · Posición confirmada por API</p>
  </>;
}
export function ServoControls({ device, disabled, send }: Props) {
  const [position, setPosition] = useState(device.position ?? 90);
  useEffect(() => setPosition(device.position ?? 90), [device.position]);
  return <>
    <svg className="mechanism" viewBox="0 0 320 150" role="img" aria-label={`Servo: ${device.position} grados`}>
      <path d="M60 125 A100 100 0 0 1 260 125" fill="none" stroke="#526672" strokeWidth="3" />
      <g transform={`rotate(${device.position ?? 90} 160 125)`}><line x1="160" y1="125" x2="65" y2="125" stroke="#8bdec0" strokeWidth="10" strokeLinecap="round" /></g>
      <circle cx="160" cy="125" r="13" fill="#243744" stroke="#8bdec0" strokeWidth="3" />
      <text x="42" y="145" fill="#a3b6be">0°</text><text x="263" y="145" fill="#a3b6be">180°</text>
    </svg>
    <p className="state">Ángulo confirmado: {device.position}°</p>
    <label htmlFor="servo-position">Ángulo solicitado <strong>{position}°</strong></label>
    <input id="servo-position" type="range" min="0" max="180" value={position} disabled={disabled} onChange={e => setPosition(Number(e.target.value))} />
    <button disabled={disabled} className="apply" onClick={() => send(device, { action: 'SET_POSITION', position })}>Aplicar posición</button>
  </>;
}
