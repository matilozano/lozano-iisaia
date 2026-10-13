import type { Device } from '../api';
import { actions, initialCommand, type Step } from './model.js';
export function StepEditor({ step, devices, disabled, update, remove, duplicate, up, down }: {
  step: Step; devices: Device[]; disabled: boolean; update: (patch: Partial<Step>) => void;
  remove: () => void; duplicate?: () => void; up?: () => void; down?: () => void;
}) {
  const device = devices.find(d => d.id === step.componentId);
  return <fieldset disabled={disabled}><legend>Paso {step.order}</legend>
    <label>Componente<select value={step.componentId} onChange={e => { const d = devices.find(d => d.id === e.target.value)!; update({ componentId: d.id, command: initialCommand(actions(d.type)[0]) }); }}>
      {devices.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
    </select></label>
    <label>Acción<select value={step.command.action} onChange={e => update({ command: initialCommand(e.target.value as Step['command']['action']) })}>
      {(device ? actions(device.type) : [step.command.action]).map(a => <option key={a} value={a}>{a}</option>)}
    </select></label>
    <label>Espera anterior (ms)<input type="number" min={0} max={600000} step={1} value={step.delayMs} onChange={e => update({ delayMs: Number(e.target.value) })}/></label>
    {step.command.action === 'start' && <label>Dirección<select value={step.command.direction ?? 'forward'} onChange={e => update({ command: { ...step.command, direction: e.target.value as 'forward' | 'reverse' } })}><option value="forward">Adelante</option><option value="reverse">Reversa</option></select></label>}
    {['start', 'SET_SPEED'].includes(step.command.action) && <label>Velocidad (%)<input type="number" min={step.command.action === 'SET_SPEED' ? 1 : 0} max={100} value={step.command.speed ?? 40} onChange={e => update({ command: { ...step.command, speed: Number(e.target.value) } })}/></label>}
    {step.command.action === 'SET_POSITION' && <label>Posición (°)<input type="number" min={0} max={180} step="any" value={step.command.position ?? 90} onChange={e => update({ command: { ...step.command, position: Number(e.target.value) } })}/></label>}
    <label>Descripción del paso<input maxLength={500} value={step.description ?? ''} onChange={e => update({ description: e.target.value })}/></label>
    <div className="actions"><button disabled={!up} onClick={up}>Subir paso</button><button disabled={!down} onClick={down}>Bajar paso</button><button disabled={!duplicate} onClick={duplicate}>Duplicar paso</button><button onClick={remove}>Eliminar paso</button></div>
  </fieldset>;
}
