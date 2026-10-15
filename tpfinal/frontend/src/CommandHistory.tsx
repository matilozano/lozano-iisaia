import type { HistoryEntry } from './useDevices';
export function CommandHistory({history}:{history:HistoryEntry[]}) { return     <section className="history" aria-label="Historial de comandos"><h2>Historial de la sesión</h2>
      {history.length === 0 ? <p>Los comandos ejecutados aparecerán aquí.</p> : <div className="table-scroll"><table><thead><tr><th>Hora</th><th>Origen</th><th>Componente</th><th>Comando</th><th>Resultado</th></tr></thead><tbody>{history.map((entry, index) => <tr key={index}><td>{entry.time}</td><td>{entry.origin}</td><td>{entry.component}</td><td>{entry.command}</td><td className={entry.result === 'OK' ? 'success' : 'failure'}>{entry.result}</td></tr>)}</tbody></table></div>}
    </section>
; }
