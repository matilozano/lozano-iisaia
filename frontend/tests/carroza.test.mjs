import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { toCarrozaModel } from '../.test-build/carroza/model.js';
import { CarrozaSvg } from '../.test-build/carroza/CarrozaSvg.js';
const device = (id, fields = {}) => ({ id, name:id, state:'stopped', online:true, speed:0, direction:null, ...fields });
const map = (devices, execution=null) => toCarrozaModel(devices,true,execution);
const svg = model => renderToStaticMarkup(React.createElement(CarrozaSvg,{model}));

test('luz confirmada ON/OFF controla iluminación SVG', () => {
  for (const state of ['on','off']) {
    const model = map([device('front-lights',{state}),device('side-lights',{state})]);
    assert.equal(model.front,state==='on'); assert.equal(model.side,state==='on');
    assert.match(svg(model),new RegExp(`Frontal: ${state.toUpperCase()}`));
    assert.equal(svg(model).includes('twin-halo'),state==='on');
  }
});
for (const effect of ['SWEEP_RIGHT','SWEEP_LEFT','BLINK','NONE']) {
  test(`${effect}: representa canales recibidos sin generar patrones`, () => {
    for (const channels of [[true,false,false,false,false,false,false,false],[false,false,false,false,false,false,false,true],Array(8).fill(true),Array(8).fill(false)]) {
      const model = map([device('main-light-bank',{channels,effect})]);
      assert.deepEqual(model.channels,channels); assert.equal(model.effect,effect);
      channels.forEach((on,index)=>assert.ok(svg(model).includes(`CH0${index+1}: ${on?'ON':'OFF'}`)));
    }
  });
}
test('motor STOP, FORWARD, REVERSE y velocidad proporcional', () => {
  const motor = fields => map([device('main-motor',fields)]);
  assert.equal(motor({state:'stopped',speed:70}).moving,false);
  assert.equal(motor({state:'running',speed:0}).moving,false);
  for(const direction of ['forward','reverse']) {
    const slow=motor({state:'running',speed:20,direction}), fast=motor({state:'running',speed:100,direction});
    assert.equal(slow.moving,true); assert.ok(slow.wheelSeconds>fast.wheelSeconds);
    assert.equal(slow.wheelDirection,direction==='reverse'?'reverse':'normal');
    assert.match(svg(slow),/animation-play-state:running/);
  }
});
test('hidráulico y servo derivan su geometría de posición confirmada', () => {
  for(const position of [0,25,100]) {
    const model=map([device('hydraulic-1',{position,movement:'EXTENDING'})]);
    assert.equal(model.lift,position*1.1); assert.equal(model.movement,'EXTENDING');
    assert.ok(svg(model).includes(`translate(0 ${-model.lift})`));
  }
  for(const position of [0,90,120,180]) {
    const model=map([device('servo-1',{position})]);
    assert.equal(model.angle,position); assert.ok(svg(model).includes(`rotate(${position} 715 223)`));
  }
});
test('STOP ALL usa snapshot: inmóvil, oscuro y posiciones conservadas; CANCELLED confirmado', () => {
  const model=map([device('front-lights',{state:'off'}),device('side-lights',{state:'off'}),device('main-motor'),device('main-light-bank',{channels:Array(8).fill(false),effect:'NONE'}),device('hydraulic-1',{position:42,movement:'STOPPED'}),device('servo-1',{position:120})],{sequenceId:'SHOW_FNE',status:'CANCELLED'});
  assert.equal(model.moving,false); assert.equal(model.position,42); assert.equal(model.angle,120);
  assert.equal(model.sequenceStatus,'CANCELLED'); assert.equal(model.channels.some(Boolean),false);
  assert.match(svg(model),/animation-play-state:paused/); assert.ok(!svg(model).includes('twin-halo'));
});
test('RUNNING y pérdida de conexión no inventan parada o nuevos estados', () => {
  const devices=[device('main-motor',{state:'running',speed:40,direction:'forward'})];
  const execution={sequenceId:'SHOW_FNE',status:'RUNNING'};
  assert.equal(map(devices,execution).sequenceStatus,'RUNNING');
  const offline=toCarrozaModel(devices,false,execution);
  assert.equal(offline.api,'OFFLINE'); assert.equal(offline.stale,true); assert.equal(offline.speed,40);
  assert.equal(toCarrozaModel([],null,null).position,null);
});
test('frontera: twin sin peticiones, timers, hooks de estado ni comandos', () => {
  for(const file of readdirSync(new URL('../src/carroza/',import.meta.url)).filter(f=>/\.tsx?$/.test(f))) {
    const source=readFileSync(new URL(`../src/carroza/${file}`,import.meta.url),'utf8');
    assert.doesNotMatch(source,/\b(fetch|setTimeout|setInterval|requestAnimationFrame|useEffect|useState|useDevices)\s*\(/,file);
    assert.doesNotMatch(source,/\/api\/|SHOW_FNE\/start|action\s*:/,file);
  }
});

test('accesibilidad: movimiento reducido desactiva rotación continua', () => {
  const css=readFileSync(new URL('../src/carroza/carroza.css',import.meta.url),'utf8');
  assert.match(css, /@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{\s*\.twin-wheel\s*\{\s*animation:\s*none;/);
  const model=map([device('main-motor',{state:'running',speed:50,direction:'forward'})]);
  // The direction/speed indicator remains available when CSS removes wheel motion.
  assert.ok(svg(model).includes('→ 50%'));
});
