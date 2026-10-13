import type { ReactNode } from 'react';
import { DndContext, KeyboardSensor, PointerSensor, closestCenter, useSensor, useSensors } from '@dnd-kit/core';
import { SortableContext, useSortable, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
function Row({ id, label, children, disabled }: { id: string; label: string; children: ReactNode; disabled: boolean }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id, disabled });
  return <div ref={setNodeRef} className="sequence-sort-row" style={{ transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? .6 : 1 }}>
    <button type="button" className="sequence-handle" disabled={disabled} {...attributes} {...listeners} aria-label={`Reordenar ${label}`}>⠿</button>{children}
  </div>;
}
export function SortableList<T extends { id: string }>({ items, disabled, label, render, move }: {
  items: T[]; disabled: boolean; label: (item: T) => string; render: (item: T, index: number) => ReactNode; move: (from: string, to: string) => void;
}) {
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }), useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates, scrollBehavior: 'auto' }));
  return <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={({ active, over }) => { if (over) move(String(active.id), String(over.id)); }} accessibility={{ screenReaderInstructions: { draggable: 'Espacio para tomar. Flechas para reordenar. Espacio para soltar o Escape para cancelar.' } }}>
    <SortableContext items={items.map(i => i.id)} strategy={verticalListSortingStrategy}>
      {items.map((item, index) => <Row key={item.id} id={item.id} label={label(item)} disabled={disabled}>{render(item, index)}</Row>)}
    </SortableContext>
  </DndContext>;
}
