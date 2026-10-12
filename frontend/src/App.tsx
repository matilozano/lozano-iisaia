import { Sequences } from './Sequences';
import { CarrozaView } from './carroza/CarrozaView';
import { useDevices } from './useDevices';
import { DevicePanel } from './DevicePanel';
import { CommandHistory } from './CommandHistory';
import { TelemetryPanel } from './Telemetry';
import { DashboardTabs } from './DashboardTabs';

export function App() {
  const state = useDevices();
  const {execution,sequencePending,sequenceError,startSequence,cancelSequence,devices,connected,error,pending,confirmation,history,componentErrors,send,stopAll,telemetry,telemetryError,telemetryPending,telemetryAction} = state;
  const disabled = Boolean(pending) || execution?.status === 'RUNNING' || sequencePending || telemetryPending || connected !== true || telemetry?.protectionLatched === true;
  return <div className="dashboard-shell"><div className="dashboard-scroll"><main>
    <header className="dashboard-header"><div className="brand"><span className="brand-mark" aria-hidden="true">F</span><div><p className="eyebrow">FIESTA NACIONAL DE LOS ESTUDIANTES</p><h1>Centro de control <span>/ Carroza</span></h1></div></div><div className="status-cluster"><span className={`connection ${connected ? 'online' : ''}`}>API {connected === null ? 'CONECTANDO' : connected ? 'ONLINE' : 'OFFLINE'}</span><span className="gateway">{telemetry?.gateway ?? 'GATEWAY SIN CONSULTAR'}</span></div></header>
    <p className="controller-status">Controlador {telemetry ? telemetry.online ? 'ONLINE' : 'OFFLINE' : 'SIN CONSULTAR'}{telemetryError ? ' · dato anterior' : ''} · Secuencia {execution?.status ?? 'SIN CONSULTAR'}</p>
    <div className="feedback" role="status"><span className="status-dot"/>{pending || (telemetryPending ? 'Enviando control de telemetría…' : sequencePending ? 'Enviando comando de secuencia…' : confirmation) || 'Sistema listo para consultar y operar. Los estados se confirman en backend.'}</div>
    {error && <p className="error" role="alert">{error} Se conserva el último estado confirmado.</p>}
    {devices.length === 0 && <p className="empty">Esperando los componentes de la API…</p>}
    {(telemetryError || telemetry?.protectionLatched || execution?.status === 'RUNNING') && <p className="operation-notice" role="status">{telemetryError ? `Telemetría sin actualizar: ${telemetryError}. ` : ''}{telemetry?.protectionLatched ? `Protección enclavada · ${telemetry.stopConfirmed ? 'Parada confirmada' : 'Parada sin confirmar'}. Resolver alertas en Vista general. ` : ''}{execution?.status === 'RUNNING' ? 'Secuencia RUNNING: comandos manuales bloqueados; STOP ALL disponible.' : 'Se conserva el último estado confirmado.'}</p>}
    <DashboardTabs panels={[
      <div className="dashboard-top" key="overview"><div className="twin-column"><CarrozaView devices={devices} connected={connected} execution={execution} />{(telemetryError || telemetry?.online === false || telemetry?.quality === 'STALE' || telemetry?.quality === 'INVALID') && <p className="twin-notice">Controlador o telemetría sin datos actuales. El Twin conserva los estados confirmados; no demuestra movimiento físico actual.</p>}</div><TelemetryPanel telemetry={telemetry} error={telemetryError} pending={telemetryPending || Boolean(pending) || sequencePending} act={action=>void telemetryAction(action)} /></div>,
      <DevicePanel key="components" devices={devices} disabled={disabled} componentErrors={componentErrors} send={send}/>,
      <div className="operations-grid" key="operations"><Sequences execution={execution} pending={sequencePending || Boolean(pending) || connected !== true || telemetryPending} startBlocked={telemetry?.protectionLatched === true} error={sequenceError} start={() => void startSequence()} cancel={() => void cancelSequence()} /><CommandHistory history={history}/></div>
    ]}/>
    <footer>{telemetry?.source === 'SIMULATED' ? 'Telemetría simulada' : 'Telemetría no disponible'} · Fuente de estado: API · Historial de esta sesión · Sin persistencia</footer>
  </main></div><div className="stop-bar"><div><strong>Parada general</strong><p>Apaga luces y efectos, detiene motor e hidráulico. Conserva el ángulo del servo.</p></div><button className="stop" disabled={pending.startsWith('Deteniendo')} onClick={() => void stopAll()}>■ STOP ALL · Detener todo</button></div></div>;
}
