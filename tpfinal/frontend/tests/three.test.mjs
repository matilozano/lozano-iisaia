import test from 'node:test';
import assert from 'node:assert/strict';
import { toCarrozaModel } from '../.test-build/carroza/model.js';
import { cameraHome, changeCamera, toSceneState } from '../.test-build/carroza/sceneModel.js';
import {readFileSync,readdirSync} from 'node:fs';
import {webGLAvailable, watchContextLoss} from '../.test-build/twin3d/webgl.js';

const model = (overrides={}) => ({...toCarrozaModel([],true,null), ...overrides});
test('3D uses confirmed lights/channels, position and angle without inventing effect frames',()=>{
  const s=toSceneState(model({front:true,side:false,channels:[true,false,true,false,false,false,false,false],effect:'SWEEP_RIGHT',position:50,angle:120}),false);
  assert.equal(s.front,true); assert.equal(s.side,false);
  assert.deepEqual(s.channels,[true,false,true,false,false,false,false,false]);
  assert.equal(s.platformY,2); assert.equal(s.servoRadians,120*Math.PI/180);
});
test('3D wheel animation follows speed and direction; STOP, offline and reduced motion freeze it',()=>{
  const running=model({moving:true,speed:50,direction:'forward'});
  assert.equal(toSceneState(running,false).wheelRate,Math.PI*50/30);
  assert.equal(toSceneState({...running,direction:'reverse'},false).wheelRate,-Math.PI*50/30);
  for(const m of [{...running,moving:false},{...running,stale:true}]) assert.equal(toSceneState(m,false).wheelRate,0);
  assert.equal(toSceneState(running,true).wheelRate,0);
});
test('camera controls clamp zoom/elevation and reset without affecting operational state',()=>{
  assert.equal(changeCamera(cameraHome,0,100,100).distance,18);
  assert.equal(changeCamera(cameraHome,0,-100,-100).distance,7);
  assert.equal(changeCamera(cameraHome,0,-100,0).pitch,0.15);
  assert.deepEqual(changeCamera(cameraHome,1,0,0),{...cameraHome,yaw:cameraHome.yaw+1});
  assert.equal(cameraHome.distance,11);
});
test('3D renderer cannot send commands, poll or schedule sequence steps',()=>{
  for(const file of readdirSync(new URL('../src/twin3d/',import.meta.url)).filter(f=>/\.tsx?$/.test(f))) {
    const source=readFileSync(new URL(`../src/twin3d/${file}`,import.meta.url),'utf8');
    assert.doesNotMatch(source,/\b(fetch|useDevices|setTimeout|setInterval)\s*\(|\/api\/|action\s*:/,file);
  }
});
test('WebGL unavailability or creation errors select the non-WebGL path; probes are released',()=>{
  assert.equal(webGLAvailable(()=>({getContext:()=>null})),false);
  assert.equal(webGLAvailable(()=>{throw new Error('GPU blocked');}),false);
  let released=false;
  assert.equal(webGLAvailable(()=>({getContext:()=>({getExtension:()=>({loseContext:()=>{released=true;}})})})),true);
  assert.equal(released,true);
});
test('context loss triggers fallback while mounted, but intentional disposal after cleanup does not',()=>{
  const target=new EventTarget(); let failures=0;
  const dispose=watchContextLoss(target,()=>failures++);
  target.dispatchEvent(new Event('webglcontextlost')); assert.equal(failures,1);
  dispose(); target.dispatchEvent(new Event('webglcontextlost')); assert.equal(failures,1);
});
