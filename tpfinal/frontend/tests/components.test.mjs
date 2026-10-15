import test from 'node:test';
import assert from 'node:assert/strict';
import React from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {DevicePanel} from '../.test-build/DevicePanel.js';
const devices = ['light','light','motor','HYDRAULIC_ACTUATOR','SERVO','light_bank'].map((type,i)=>({id:`device-${i}`,name:`Componente ${i}`,type,state:'stopped',online:true,direction:null,speed:0,position:90,movement:'STOPPED',channels:Array(8).fill(false),effect:'NONE',effectSpeed:50}));
const render = (disabled=false) => renderToStaticMarkup(React.createElement(DevicePanel,{devices,disabled,componentErrors:{'device-0':'TIMEOUT: Componente 0'},send:async()=>{}}));
test('compact grid includes the bank, preserves direct controls and expandable advanced controls',()=>{
 const html=render();
 assert.equal((html.match(/data-component-id="device-/g)??[]).length,6);
 assert.match(html, /class="component-card" data-component-id="device-5"[\s\S]*<section class="bank"/, 'bank remains a sortable grid card');
 for(const label of ['Encender','Apagar','Reversa','Detener','Adelante','Retraer','STOP','Extender','Aplicar posición','Todos OFF','Detener efecto','Barrido','Ida y vuelta','Parpadeo','Todos ON','Aplicar velocidad del efecto']) assert.ok(html.includes(label),label);
 assert.match(html, /<summary>Efectos y velocidad<\/summary>/);
 assert.equal((html.match(/aria-label="Canal /g)??[]).length,8);
 assert.match(html,/role="alert">TIMEOUT: Componente 0/);
});
test('global command blocking also applies to controls inside disclosures',()=>{
 const buttons=[...render(true).matchAll(/<button([^>]*)>([^<]*)/g)].filter(([,attributes,text])=>!attributes.includes('drag-handle') && text!=='Restablecer orden').map(([,attributes])=>attributes);
 assert.ok(buttons.length>=17);
 assert.ok(buttons.every(button=>button.includes('disabled=""')));
});

test('each card has an independent accessible drag handle without disabling layout changes',()=>{
 const html=render(true);
 assert.equal((html.match(/class="drag-handle"/g)??[]).length,6);
 for(let i=0;i<6;i++) assert.ok(html.includes(`aria-label="Reordenar Componente ${i}"`));
 assert.match(html,/aria-describedby="component-order-help"/);
 assert.match(html,/Restablecer orden/);
});