import type { Device } from '../api';
import type { SequenceExecution } from '../Sequences';
import { toCarrozaModel, type CarrozaModel } from './model.js';
import { CarrozaSvg } from './CarrozaSvg.js';
import './carroza.css';

function CarrozaHud({ model }: { model: CarrozaModel }) {
  const fields = [ ['API', model.api], ['Motor', `${model.motor} · ${model.speed}% · ${model.direction}`],
    ['Hidráulico', `${model.position?.toFixed(1) ?? '—'}% · ${model.movement}`], ['Servo', `${model.angle ?? '—'}°`],
    ['Banco', model.effect], ['Secuencia', `${model.sequence} · ${model.sequenceStatus}`] ];
  return <dl className="twin-hud">{fields.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>;
}
export function CarrozaView({ devices, connected, execution }: { devices: readonly Device[]; connected: boolean | null; execution: SequenceExecution | null }) {
  const model = toCarrozaModel(devices, connected, execution);
  return <section className="carroza-view" aria-label="Vista carroza">
    <div className="twin-heading"><div><p className="eyebrow">FNE · DIGITAL TWIN</p><h2>VISTA CARROZA</h2></div><span>Estado confirmado · Sin hardware</span></div>
    {model.stale && <p className="twin-notice">{connected === null ? 'Esperando estado de la API.' : 'API OFFLINE: último estado confirmado; la representación no confirma movimiento actual.'}</p>}
    {model.offline.length > 0 && <p className="twin-notice">Componentes OFFLINE: {model.offline.join(', ')}</p>}
    <CarrozaSvg model={model} /><CarrozaHud model={model} />
    <p className="twin-note">LED 01 → 08 de izquierda a derecha · Plataforma = posición hidráulica · Pieza dorada = ángulo del servo</p>
  </section>;
}
