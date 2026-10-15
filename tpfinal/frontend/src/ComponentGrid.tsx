import {useState, type ReactNode} from 'react';
import {DndContext, DragOverlay, closestCenter, KeyboardSensor, MouseSensor, TouchSensor, useSensor, useSensors} from '@dnd-kit/core';
import {SortableContext, useSortable, rectSortingStrategy, sortableKeyboardCoordinates} from '@dnd-kit/sortable';
import {CSS} from '@dnd-kit/utilities';
import {moveCard, readOrder, reconcileOrder, saveOrder} from './componentOrder.js';

type Card = {id: string; name: string; content: ReactNode};

function SortableCard({card}: {card: Card}) {
  const {attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging} = useSortable({id: card.id});
  return <div ref={setNodeRef} className={`component-card${isDragging ? ' dragging' : ''}`} data-component-id={card.id}
    style={{transform: CSS.Transform.toString(transform), transition}}>
    <button ref={setActivatorNodeRef} {...attributes} {...listeners} className="drag-handle" aria-label={`Reordenar ${card.name}`} title={`Reordenar ${card.name}`}><span aria-hidden="true">⠿</span></button>
    {card.content}
  </div>;
}

export function ComponentGrid({cards}: {cards: Card[]}) {
  const [saved, setSaved] = useState<unknown>(() => {
    try { return readOrder(window.localStorage); } catch { return []; }
  });
  const [active, setActive] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const ids = reconcileOrder(saved, cards.map(card => card.id));
  const sensors = useSensors(
    useSensor(MouseSensor, {activationConstraint: {distance: 8}}),
    useSensor(TouchSensor, {activationConstraint: {delay: 250, tolerance: 5}}),
    useSensor(KeyboardSensor, {coordinateGetter: sortableKeyboardCoordinates, scrollBehavior: 'auto'})
  );
  function persist(next: string[], message: string) {
    setSaved(next);
    let stored = false;
    try { stored = saveOrder(window.localStorage, next); } catch { /* Storage may be blocked. */ }
    setNotice(stored ? message : `${message} No se pudo guardar en este navegador; se conserva durante esta sesión.`);
  }
  const name = (id: string | number) => cards.find(card => card.id === id)?.name ?? String(id);
  return <>
    <div className="component-order-tools"><p id="component-order-help">Agarre ⠿ para mover. Teclado: Espacio/Enter, flechas, Espacio/Enter para soltar; Escape cancela. Táctil: mantener el agarre.</p>
      <button className="secondary" disabled={active !== null} onClick={() => persist(cards.map(card => card.id), 'Orden predeterminado restablecido.')}>Restablecer orden</button></div>
    <p className="order-notice" role="status">{notice}</p>
    <DndContext sensors={sensors} collisionDetection={closestCenter}
      accessibility={{screenReaderInstructions: {draggable: 'Para reordenar, pulsá Espacio o Enter en el agarre. Usá las flechas. Volvé a pulsar Espacio o Enter para soltar, o Escape para cancelar.'},
        announcements: {
          onDragStart: ({active}) => `Moviendo ${name(active.id)}.`,
          onDragOver: ({active, over}) => over ? `${name(active.id)} sobre ${name(over.id)}, posición ${ids.indexOf(String(over.id)) + 1} de ${ids.length}.` : 'Fuera de las tarjetas.',
          onDragEnd: ({active, over}) => over ? `${name(active.id)} soltado en posición ${ids.indexOf(String(over.id)) + 1}.` : 'Orden sin cambios.',
          onDragCancel: () => 'Reordenamiento cancelado. Orden sin cambios.'
        }}}
      onDragStart={({active}) => setActive(String(active.id))}
      onDragCancel={() => setActive(null)}
      onDragEnd={({active, over}) => {
        setActive(null);
        const next = moveCard(ids, String(active.id), over ? String(over.id) : null);
        if (next !== ids) persist(next, 'Orden de componentes guardado.');
      }}>
      <SortableContext items={ids} strategy={rectSortingStrategy}>
        <section className="devices" aria-label="Componentes" aria-describedby="component-order-help">
          {ids.map(id => <SortableCard key={id} card={cards.find(card => card.id === id)!} />)}
        </section>
      </SortableContext>
      <DragOverlay dropAnimation={null} zIndex={1}>{active ? <div className="card-drag-preview">⠿ {name(active)}</div> : null}</DragOverlay>
    </DndContext>
  </>;
}
