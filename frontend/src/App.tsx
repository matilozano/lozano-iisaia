import { HydraulicControls, ServoControls } from './PositionControls';
import { LightBank } from './LightBank';
import { useDevices } from './useDevices';
import { MotorControls } from './MotorControls';

export function App() {
  const { devices, connected, error, pending, confirmation, history, componentErrors, send, stopAll } = useDevices();
  const disabled = Boolean(pending) || connected !== true;
  return <main>
    <header><div><p className="eyebrow">FNE · SIMULADOR</p><h1>Control de carroza</h1><p>Panel de simulación y control electromecánico</p></div><span className={`connection ${connected ? 'online' : ''}`}>API {connected === null ? 'CONECTANDO' : connected ? 'ONLINE' : 'OFFLINE'}</span></header>
    <p className="feedback" role="status">{pending || confirmation || 'Los controles reflejan el estado confirmado por el backend.'}</p>
    {error && <p className="error" role="alert">{error} Se conserva el último estado confirmado.</p>}
    {devices.length === 0 && <p className="empty">Esperando los componentes de la API…</p>}
    <section className="devices" aria-label="Componentes">
      {devices.filter(device => device.type !== 'light_bank').map(device => <article key={device.id} aria-label={device.name}>
        <p className="eyebrow">{device.type === 'light' ? 'ILUMINACIÓN' : 'MOVIMIENTO'}</p><h2>{device.name}</h2><p className={device.online ? "device-online" : "failure"}>{device.online ? "ONLINE" : "DEVICE OFFLINE"}</p>{componentErrors[device.id] && <p className="error" role="alert">{componentErrors[device.id]}</p>}
        {device.type === 'light' ? <>
          <div className="lamp-stage"><div className={`lamp ${device.state === 'on' ? 'on' : ''}`} role="img" aria-label={`${device.name}: ${device.state === 'on' ? 'encendidas' : 'apagadas'}`}/></div>
          <p className="state">{device.state === 'on' ? 'Encendidas' : 'Apagadas'}</p>
          <div className="actions"><button disabled={disabled || !device.online} onClick={() => void send(device, { action: 'on' })}>Encender</button><button className="secondary" disabled={disabled || !device.online} onClick={() => void send(device, { action: 'off' })}>Apagar</button></div>
        </> : device.type === "HYDRAULIC_ACTUATOR" ? <HydraulicControls device={device} disabled={disabled || !device.online} send={send} /> : device.type === "SERVO" ? <ServoControls device={device} disabled={disabled || !device.online} send={send} /> : <MotorControls device={device} disabled={disabled || !device.online} send={send} />}
      </article>)}
    </section>
    {devices.filter(device => device.type === 'light_bank').map(device => <LightBank error={componentErrors[device.id]} key={device.id} device={device} disabled={disabled || !device.online} send={send} />)}
    <section className="system" aria-label="Estado del sistema">
      <span>API <strong>{connected === null ? 'CONECTANDO' : connected ? 'ONLINE' : 'OFFLINE'}</strong></span>
      <span>Gateway <strong>SIMULATOR</strong></span><span>Controlador <strong>SIMULADO</strong></span>
      <span className="last-command">Último comando: {history[0] ? `${history[0].component} / ${history[0].command} / ${history[0].result}` : 'Sin comandos en esta sesión'}</span>
    </section>
    <section className="history" aria-label="Historial de comandos"><h2>Historial de la sesión</h2>
      {history.length === 0 ? <p>Los comandos ejecutados aparecerán aquí.</p> : <div className="table-scroll"><table><thead><tr><th>Hora</th><th>Componente</th><th>Comando</th><th>Resultado</th></tr></thead><tbody>{history.map((entry, index) => <tr key={index}><td>{entry.time}</td><td>{entry.component}</td><td>{entry.command}</td><td className={entry.result === 'OK' ? 'success' : 'failure'}>{entry.result}</td></tr>)}</tbody></table></div>}
    </section>
    <div className="stop-bar"><div><strong>Parada general</strong><p>Apaga luces y canales, detiene motor, actuador y efectos.</p></div><button className="stop" disabled={pending.startsWith('Deteniendo')} onClick={() => void stopAll()}>■ Detener todo</button></div>
    <footer>Gateway simulado · Sin hardware físico · Canales consultados desde la API · Sin persistencia</footer>
  </main>;
}
