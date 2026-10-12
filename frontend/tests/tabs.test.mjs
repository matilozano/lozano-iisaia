import test from 'node:test';
import assert from 'node:assert/strict';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { DashboardTabs, tabIndexForKey } from '../.test-build/DashboardTabs.js';

test('tabs expose one selected tab, linked panels and preserve all panel contents', () => {
  const html = renderToStaticMarkup(React.createElement(DashboardTabs, {
    panels: [React.createElement('input', {key:0, defaultValue:'70'}), 'Componentes conservados', 'Historial conservado']
  }));
  assert.equal((html.match(/role="tab"/g) ?? []).length, 3);
  assert.equal((html.match(/aria-selected="true"/g) ?? []).length, 1);
  assert.equal((html.match(/role="tabpanel"/g) ?? []).length, 3);
  assert.equal((html.match(/hidden=""/g) ?? []).length, 2);
  for (const id of ['overview','components','operations']) {
    assert.match(html, new RegExp(`aria-controls="panel-${id}"`));
    assert.match(html, new RegExp(`aria-labelledby="tab-${id}"`));
  }
  assert.match(html, /Historial conservado/);
  assert.match(html, /Componentes conservados/);
  assert.doesNotMatch(html, /href="#/);
});

test('keyboard selection wraps and supports Home/End without consuming unrelated keys', () => {
  assert.equal(tabIndexForKey('ArrowRight',2),0);
  assert.equal(tabIndexForKey('ArrowLeft',0),2);
  assert.equal(tabIndexForKey('Home',2),0);
  assert.equal(tabIndexForKey('End',0),2);
  assert.equal(tabIndexForKey('Tab',1),null);
  assert.equal(tabIndexForKey('Enter',1),null);
});
