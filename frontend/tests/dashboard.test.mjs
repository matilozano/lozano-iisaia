import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
test('dashboard reserves persistent STOP region and explicit keyboard navigation',()=>{const app=readFileSync(new URL('../src/App.tsx',import.meta.url),'utf8'); assert.match(app,/href="#controls"/); assert.match(app,/dashboard-top/); const css=readFileSync(new URL('../src/styles.css',import.meta.url),'utf8'); assert.match(css,/grid-template-rows:\s*minmax\(0,\s*1fr\) auto/);assert.match(css,/\.dashboard-scroll[^}]*overflow-y:\s*auto/s);});
