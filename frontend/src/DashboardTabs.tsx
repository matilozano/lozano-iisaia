import { useRef, useState, type ReactNode } from 'react';

const tabs = [
  { id: 'overview', label: 'Vista general' },
  { id: 'components', label: 'Componentes' },
  { id: 'operations', label: 'Secuencias e historial' },
] as const;

export function tabIndexForKey(key: string, current: number): number | null {
  if (key === 'ArrowRight') return (current + 1) % tabs.length;
  if (key === 'ArrowLeft') return (current + tabs.length - 1) % tabs.length;
  if (key === 'Home') return 0;
  if (key === 'End') return tabs.length - 1;
  return null;
}

// Presentation only. Panels stay mounted to preserve unsent controls and focus state.
export function DashboardTabs({ panels }: { panels: readonly ReactNode[] }) {
  const [active, setActive] = useState(0);
  const buttons = useRef<(HTMLButtonElement | null)[]>([]);
  const sections = useRef<(HTMLElement | null)[]>([]);
  return <>
    <button className="skip-link" onClick={() => sections.current[active]?.focus()}>Ir al contenido</button>
    <div className="dashboard-tabs" role="tablist" aria-label="Secciones del dashboard">
      {tabs.map((tab, index) => <button key={tab.id} ref={element => { buttons.current[index] = element; }}
        role="tab" id={`tab-${tab.id}`} aria-controls={`panel-${tab.id}`} aria-selected={active === index}
        tabIndex={active === index ? 0 : -1} onClick={() => setActive(index)} onKeyDown={event => {
          const next = tabIndexForKey(event.key, index);
          if (next === null) return;
          event.preventDefault(); setActive(next); buttons.current[next]?.focus();
        }}>{tab.label}</button>)}
    </div>
    {tabs.map((tab, index) => <section key={tab.id} ref={element => { sections.current[index] = element; }}
      role="tabpanel" id={`panel-${tab.id}`} aria-labelledby={`tab-${tab.id}`} tabIndex={0}
      hidden={active !== index}>{panels[index]}</section>)}
  </>;
}
