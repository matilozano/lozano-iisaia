import type { Command } from './api';
export interface SequenceExecution {
  runId: string | null; sequenceId: string | null; status: 'IDLE' | 'RUNNING' | 'COMPLETED' | 'CANCELLED' | 'FAILED';
  currentStep: number; totalSteps: number; startedAt: string | null; finishedAt: string | null;
  lastResult: string | null; error: string | null; elapsedSeconds: number;
}
export interface SequenceEvent { id: number; runId: string; time: string; origin: 'SEQUENCE'; componentId: string; command: Command; result: string }
export function Sequences({ execution, pending, error, start, cancel }: {
  execution: SequenceExecution | null; pending: boolean; error: string; start: () => void; cancel: () => void;
}) {
  return <section className="bank" aria-label="Secuencias"><p className="eyebrow">SECUENCIAS · EJECUCIÓN EN BACKEND</p>
    <h2>SHOW FNE</h2><p>Demostración coordinada de la carroza.</p>
    <div className="actions"><button disabled={pending || !execution || execution.status === 'RUNNING'} onClick={start}>Iniciar show</button><button className="secondary" disabled={pending || execution?.status !== 'RUNNING'} onClick={cancel}>Detener secuencia</button></div>
    {execution && <><p role="status">{execution.status} · Paso {execution.currentStep} / {execution.totalSteps} · {execution.elapsedSeconds.toFixed(1)} s</p>
      <progress aria-label="Progreso de secuencia" max={execution.totalSteps || 1} value={execution.currentStep} />
      <p>Último comando: {execution.lastResult ?? '—'}</p>{execution.error && <p className="error" role="alert">{execution.error}</p>}</>}
    {error && <p className="error" role="alert">{error}</p>}
    <p className="hint">Durante RUNNING los comandos manuales están bloqueados por la API. Detener aplica una parada global segura.</p>
  </section>;
}
