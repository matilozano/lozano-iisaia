import test from 'node:test';
import assert from 'node:assert/strict';
import {ORDER_KEY, readOrder, reconcileOrder, moveCard, saveOrder} from '../.test-build/componentOrder.js';
const ids=['front','side','motor'];
test('saved order is validated, deduplicated and reconciled with current component IDs',()=>{
 assert.deepEqual(reconcileOrder(['motor','obsolete','motor',12,'front'],ids),['motor','front','side']);
 assert.deepEqual(reconcileOrder(null,ids),ids);
 assert.deepEqual(reconcileOrder({ids:['motor']},ids),ids);
 assert.deepEqual(reconcileOrder(['side'],[...ids,'servo']),['side','front','motor','servo']);
});
test('versioned storage tolerates corrupt/unavailable data and stores IDs only',()=>{
 assert.match(ORDER_KEY,/v1$/);
 assert.deepEqual(readOrder({getItem:()=>'{bad'}),[]);
 assert.deepEqual(readOrder({getItem:()=>{throw Error('denied');}}),[]);
 let written;
 assert.equal(saveOrder({setItem:(key,value)=>written={key,value}},['motor','front']),true);
 assert.deepEqual(written,{key:ORDER_KEY,value:'["motor","front"]'});
 assert.equal(saveOrder({setItem:()=>{throw Error('quota');}},ids),false);
});
test('drop moves only known IDs, cancellation/outside and same position are no-ops',()=>{
 assert.deepEqual(moveCard(ids,'front','motor'),['side','motor','front']);
 assert.deepEqual(moveCard(ids,'motor','front'),['motor','front','side']);
 for(const [from,to] of [['front',null],['unknown','side'],['front','unknown'],['side','side']]) assert.deepEqual(moveCard(ids,from,to),ids);
 assert.deepEqual(ids,['front','side','motor']);
});

test('layout reordering has no commands, API calls or additional polling',async()=>{
 const {readFile}=await import('node:fs/promises');
 const source=await readFile(new URL('../src/ComponentGrid.tsx',import.meta.url),'utf8');
 assert.doesNotMatch(source,/\b(fetch|send|useDevices|setInterval|setTimeout)\s*\(/);
 assert.doesNotMatch(source,/from\s+['"].*(api|useDevices)/);
});
