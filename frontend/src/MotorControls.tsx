import { useEffect, useState } from 'react';
import type { Command, Device } from './api';

export function MotorControls({ device, disabled, send }: { device: Device; disabled: boolean; send: (device: Device, command: Command) => void }) {
  const [speed, setSpeed] = useState(50);
  useEffect(() => { if (device.state === 'running') setSpeed(device.speed); }, [device.speed, device.state]);
  const running = device.state === 'running';
  return <>
    <div className="rotor-stage"><div className="rotor" role="img" aria-label={`Motor: ${device.state}, ${device.speed}%`}
      style={{ animationDuration: `${60 / Math.max(1, device.speed)}s`, animationDirection: device.direction === 'reverse' ? 'reverse' : 'normal', animationPlayState: running && device.speed > 0 ? 'running' : 'paused' }}><span>✣</span></div><strong>{device.speed}%</strong></div>
    <p className="state">{running ? 'En marcha' : 'Detenido'} · {device.direction === 'reverse' ? 'Reversa' : device.direction === 'forward' ? 'Adelante' : 'Sin dirección'}</p>
    <div className="actions"><button disabled={disabled} onClick={() => send(device, { action: 'start', direction: 'reverse', speed })}>← Reversa</button><button className="secondary" disabled={disabled} onClick={() => send(device, { action: 'stop' })}>Detener</button><button disabled={disabled} onClick={() => send(device, { action: 'start', direction: 'forward', speed })}>Adelante →</button></div>
    <label htmlFor="motor-speed">Velocidad solicitada <strong>{speed}%</strong></label><input id="motor-speed" type="range" min="0" max="100" value={speed} disabled={disabled} onChange={event => setSpeed(Number(event.target.value))}/>
    <button className="secondary apply" disabled={disabled || !running} onClick={() => send(device, { action: 'start', direction: device.direction ?? 'forward', speed })}>Aplicar velocidad</button>
    <p className="hint">Elegí la velocidad antes de iniciar, o aplicala mientras el motor está en marcha.</p>
  </>;
}
