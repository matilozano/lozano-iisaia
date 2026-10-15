import test from 'node:test';
import assert from 'node:assert/strict';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readFileSync } from 'node:fs';
import { actions, initialCommand, reorder, normalizeSteps } from '../.test-build/sequences/model.js';
import { StepEditor } from '../.test-build/sequences/StepEditor.js';

test('editor ofrece exclusivamente comandos de los contratos existentes', () => {
  assert.deepEqual(actions('light'), ['on', 'off']);
  assert.deepEqual(actions('HYDRAULIC_ACTUATOR'), ['EXTEND', 'RETRACT', 'STOP']);
  assert.deepEqual(actions('SERVO'), ['SET_POSITION']);
  assert(!actions('light_bank').includes('SET_CHANNEL'));
  assert.deepEqual(initialCommand('start'), { action: 'start', direction: 'forward', speed: 40 });
  assert.deepEqual(initialCommand('stop'), { action: 'stop' });
});
test('reordenar conserva identificadores y parámetros sin mutar el borrador previo', () => {
  const items = [1,2,3].map(i => ({ id: String(i), order: i, delayMs: i * 100, componentId: 'front-lights', command: { action: 'on' } }));
  const changed = normalizeSteps(reorder(items, '3', '1'));
  assert.deepEqual(changed.map(s => s.id), ['3','1','2']);
  assert.deepEqual(changed.map(s => s.order), [1,2,3]);
  assert.equal(changed[0].delayMs, 300);
  assert.deepEqual(items.map(s => s.id), ['1','2','3']);
  assert.equal(reorder(items, 'missing', '1'), items);
});
test('servo ofrece posición y motor dirección/velocidad; lectura bloqueada durante ejecución', () => {
  const base = { id: 's', order: 1, delayMs: 100, componentId: 'servo-1', command: initialCommand('SET_POSITION') };
  const props = { devices: [], disabled: false, update() {}, remove() {} };
  const servo = renderToStaticMarkup(createElement(StepEditor, { ...props, step: base }));
  assert.match(servo, /Posición/); assert.doesNotMatch(servo, /Velocidad/);
  const motor = renderToStaticMarkup(createElement(StepEditor, { ...props, disabled: true, step: { ...base, command: initialCommand('start') } }));
  assert.match(motor, /fieldset disabled/); assert.match(motor, /Dirección/); assert.match(motor, /Velocidad/);
});
test('diseñador delega ejecución y no añade temporizadores ni polling operativo', () => {
  const source = readFileSync(new URL('../src/sequences/SequenceDesigner.tsx', import.meta.url), 'utf8');
  assert.doesNotMatch(source, /setInterval|setTimeout|devices\/.*commands/);
  assert.match(source, /start\(draft.id, mode\)/);
  const sortable = readFileSync(new URL('../src/sequences/SortableList.tsx', import.meta.url), 'utf8');
  assert.match(sortable, /KeyboardSensor/); assert.match(sortable, /aria-label/);
});
