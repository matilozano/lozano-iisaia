import { useEffect, useState } from 'react';
import { request, type Device } from '../api';
import type { SequenceExecution } from '../Sequences';
import { actions, initialCommand, normalizeSteps, reorder, type Definition } from './model.js';
import { SortableList } from './SortableList.js';
import { StepEditor } from './StepEditor.js';
import './designer.css';

export function SequenceDesigner({ devices, execution, blocked, start, cancel }: {
  devices: Device[]; execution: SequenceExecution | null; blocked: boolean;
  start: (id: string, mode: string) => void; cancel: () => void;
}) {
  const [definitions, setDefinitions] = useState<Definition[]>([]);
  const [draft, setDraft] = useState<Definition | null>(null);
  const [pending, setPending] = useState(false), [error, setError] = useState(''), [notice, setNotice] = useState('');
  const [mode, setMode] = useState('ONCE');
  const running = execution?.status === 'RUNNING';
  const disabled = pending || blocked || running;
  async function load() { setDefinitions(await request<Definition[]>('sequences')); }
  useEffect(() => { void load().catch(e => setError(String(e))); }, []);
  async function perform(action: () => Promise<void>) {
    setPending(true); setError(''); setNotice('');
    try { await action(); } catch (e) { setError(e instanceof Error ? e.message : String(e)); }
    finally { setPending(false); }
  }
  function select(value: Definition | null) {
    if (draft && !window.confirm('Cambiar de definición descarta el borrador sin guardar. ¿Continuar?')) return;
    setDraft(value ? { ...structuredClone(value), steps: value.steps.map((s, i) => ({ ...structuredClone(s), id: s.id ?? `builtin-${i}` })) } : { id: crypto.randomUUID(), name: 'Nueva secuencia', description: '', steps: [] });
  }
  const builtin = draft?.id === 'SHOW_FNE';
  const readonly = disabled || builtin;
  return <section className="bank sequence-designer" aria-label="Diseñador de secuencias">
    <p className="eyebrow">DISEÑADOR · DEFINICIONES GUARDADAS EN BACKEND</p><h2>Secuencias personalizadas</h2>
    <p>Esperas antes de cada comando, contadas desde la confirmación anterior. La ejecución siempre pertenece al backend.</p>
    <div className="actions"><button disabled={disabled} onClick={() => select(null)}>Crear secuencia</button><button disabled={pending} onClick={() => void perform(load)}>Recargar listado</button></div>
    <SortableList items={definitions.filter(d => d.id !== 'SHOW_FNE')} disabled={disabled} label={d => d.name} move={(from, to) => void perform(async () => {
      const ordered = reorder(definitions.filter(d => d.id !== 'SHOW_FNE'), from, to);
      await request('sequences/order', ordered.map(d => d.id)); await load();
    })} render={d => <button disabled={pending} onClick={() => select(d)}>{d.name}</button>} />
    <button disabled={pending || !definitions.some(d => d.id === 'SHOW_FNE')} onClick={() => select(definitions.find(d => d.id === 'SHOW_FNE')!)}>Seleccionar SHOW FNE</button>
    {draft && <>
      <label>Nombre<input maxLength={100} disabled={readonly} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })}/></label>
      <label>Descripción<textarea maxLength={2000} disabled={readonly} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })}/></label>
      {builtin && <p>SHOW_FNE es de solo lectura. Duplicar permite crear una versión editable.</p>}
      <div className="actions">
        <button disabled={readonly} onClick={() => void perform(async () => {
          const saved = await request<Definition>(`sequences${draft.revision ? '/' + encodeURIComponent(draft.id) : ''}`, { ...draft, steps: normalizeSteps(draft.steps) }, draft.revision ? 'PUT' : 'POST');
          setDraft(saved); await load(); setNotice('Definición guardada y validada por backend.');
        })}>Guardar</button>
        <button disabled={disabled || !definitions.some(d => d.id === draft.id)} onClick={() => void perform(async () => {
          setDraft(await request<Definition>(`sequences/${encodeURIComponent(draft.id)}/duplicate`, {})); await load();
        })}>Duplicar</button>
        <button disabled={readonly || !draft.revision} onClick={() => {
          if (window.confirm(`¿Eliminar definitivamente ${draft.name}?`)) void perform(async () => {
            await request(`sequences/${encodeURIComponent(draft.id)}?revision=${encodeURIComponent(draft.revision!)}`, undefined, 'DELETE'); setDraft(null); await load();
          });
        }}>Eliminar secuencia</button>
      </div>
      <SortableList items={draft.steps} disabled={readonly} label={s => `paso ${s.order}`} move={(from, to) => setDraft({ ...draft, steps: normalizeSteps(reorder(draft.steps, from, to)) })} render={(step, index) => <StepEditor
        step={step} devices={devices} disabled={readonly}
        duplicate={draft.steps.length >= 200 ? undefined : () => { const steps = [...draft.steps]; steps.splice(index + 1, 0, { ...structuredClone(step), id: crypto.randomUUID() }); setDraft({ ...draft, steps: normalizeSteps(steps) }); }}
        update={patch => setDraft({ ...draft, steps: draft.steps.map(s => s.id === step.id ? { ...s, ...patch } : s) })}
        up={index === 0 ? undefined : () => setDraft({ ...draft, steps: normalizeSteps(reorder(draft.steps, step.id, draft.steps[index - 1].id)) })}
        down={index === draft.steps.length - 1 ? undefined : () => setDraft({ ...draft, steps: normalizeSteps(reorder(draft.steps, step.id, draft.steps[index + 1].id)) })}
        remove={() => { if (window.confirm(`¿Eliminar paso ${index + 1}?`)) setDraft({ ...draft, steps: normalizeSteps(draft.steps.filter(s => s.id !== step.id)) }); }}
      />} />
      <button disabled={readonly || !devices.length || draft.steps.length >= 200} onClick={() => { const d = devices[0]; setDraft({ ...draft, steps: [...draft.steps, { id: crypto.randomUUID(), order: draft.steps.length + 1, delayMs: 1000, componentId: d.id, command: initialCommand(actions(d.type)[0]) }] }); }}>Agregar paso</button>
      <label>Modo de ejecución<select disabled={disabled} value={mode} onChange={e => setMode(e.target.value)}><option value="ONCE">Una vez</option><option value="CONTINUOUS">Continuo hasta detención</option></select></label>
      <p>Se ejecuta la última definición guardada, no el borrador. Tras una parada nunca se reinicia automáticamente.</p>
      <div className="actions"><button disabled={disabled || !definitions.some(d => d.id === draft.id)} onClick={() => start(draft.id, mode)}>Ejecutar definición guardada</button><button disabled={!running || pending} onClick={cancel}>Detener secuencia</button></div>
    </>}
    {pending && <p role="status">Enviando al backend…</p>}{notice && <p role="status">{notice}</p>}{error && <p role="alert" className="error">{error}</p>}
  </section>;
}
