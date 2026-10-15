import test from 'node:test';
import assert from 'node:assert/strict';
const module=await import('../.test-build/pollSnapshot.js').catch(()=>({}));
test('telemetry error does not discard successful devices or sequence responses',async()=>{assert.equal(typeof module.pollSnapshot,'function');const result=await module.pollSnapshot(async path=>{if(path==='telemetry')throw Error('sensor endpoint failed');return path==='devices'?[{id:'confirmed'}]:[];});assert.equal(result.devices.status,'fulfilled');assert.deepEqual(result.devices.value,[{id:'confirmed'}]);assert.equal(result.telemetry.status,'rejected');assert.equal(result.execution.status,'fulfilled');});
