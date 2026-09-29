import { useDevices } from './useDevices';
import { MotorControls } from './MotorControls';

export function App() {
  const { devices, connected, error, pending, confirmation, send, stopAll } = useDevices();
  const disabled = Boolean(pending) || connected !== true;
  return <main>
    <header><div><p className="eyebrow">FNE · SIMULADOR</p><h1>Control de carroza</h1><p>Iluminación y movimiento · Iteración 1</p></div><span className={`connection ${connected ? 'online' : ''}`}>API {connected === null ? 'CONECTANDO' : connected ? 'ONLINE' : 'OFFLINE'}</span></header>
    <p className="feedback" role="status">{pending || confirmation || 'Los controles reflejan el estado confirmado por el backend.'}</p>
    {error && <p className="error" role="alert">{error} Se conserva el último estado confirmado.</p>}
    {devices.length === 0 && <p className="empty">Esperando los componentes de la API…</p>}
    <section className="devices" aria-label="Componentes">
      {devices.map(device => <article key={device.id} aria-label={device.name}>
        <p className="eyebrow">{device.type === 'light' ? 'ILUMINACIÓN' : 'MOVIMIENTO'}</p><h2>{device.name}</h2>
        {device.type === 'light' ? <>
          <div className="lamp-stage"><div className={`lamp ${device.state === 'on' ? 'on' : ''}`} role="img" aria-label={`${device.name}: ${device.state === 'on' ? 'encendidas' : 'apagadas'}`}/></div>
          <p className="state">{device.state === 'on' ? 'Encendidas' : 'Apagadas'}</p>
          <div className="actions"><button disabled={disabled} onClick={() => void send(device, { action: 'on' })}>Encender</button><button className="secondary" disabled={disabled} onClick={() => void send(device, { action: 'off' })}>Apagar</button></div>
        </> : <MotorControls device={device} disabled={disabled} send={send} />}
      </article>)}
    </section>
    <div className="stop-bar"><div><strong>Parada general</strong><p>Apaga ambas luces y detiene el motor.</p></div><button className="stop" disabled={pending.startsWith('Deteniendo')} onClick={() => void stopAll()}>■ Detener todo</button></div>
    <footer>Gateway simulado · Sin hardware físico · Estados consultados cada segundo</footer>
  </main>;
}
