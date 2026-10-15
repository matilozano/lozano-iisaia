import { Component, Suspense, lazy, useEffect, useRef, useState, type ReactNode } from 'react';
import { Canvas, useThree } from '@react-three/fiber';
import type { CarrozaModel } from '../carroza/model';
import { cameraHome, changeCamera } from '../carroza/sceneModel';
import { CarrozaSvg } from '../carroza/CarrozaSvg';
import { webGLAvailable, watchContextLoss } from './webgl';
import './twin3d.css';

const Scene=lazy(()=>import('./CarrozaScene'));
function ContextGuard({onLost}:{onLost:()=>void}) {
  const gl=useThree(state=>state.gl);
  useEffect(()=>watchContextLoss(gl.domElement,onLost),[gl,onLost]);
  return null;
}
class RenderBoundary extends Component<{children:ReactNode;fallback:ReactNode;onFailure:()=>void},{failed:boolean}> {
  state={failed:false};
  static getDerivedStateFromError(){return {failed:true};}
  componentDidCatch(){this.props.onFailure();}
  render(){return this.state.failed ? this.props.fallback : this.props.children;}
}
export function TwinViewport({model}:{model:CarrozaModel}) {
  const [supported,setSupported]=useState<boolean|null>(null);
  const [flat,setFlat]=useState(false);
  const [lost,setLost]=useState(false);
  const [pose,setPose]=useState(cameraHome);
  const [reduced,setReduced]=useState(false);
  const [pauseMotion,setPauseMotion]=useState(false);
  const [visible,setVisible]=useState(false);
  const host=useRef<HTMLDivElement>(null);
  const pointer=useRef<{id:number;x:number;y:number}|null>(null);
  useEffect(()=>{
    setSupported(webGLAvailable());
    const preference=matchMedia('(prefers-reduced-motion: reduce)');
    const update=()=>setReduced(preference.matches); update();
    preference.addEventListener('change',update);
    const observer=new IntersectionObserver(([entry])=>setVisible(entry.isIntersecting));
    if(host.current) observer.observe(host.current);
    return ()=>{preference.removeEventListener('change',update);observer.disconnect();};
  },[]);
  const fallback=<><p className="twin-note" role="status">Vista 2D · {flat ? 'seleccionada' : 'WebGL no disponible o renderizado interrumpido'}. Mismos estados confirmados.</p><CarrozaSvg model={model}/></>;
  const three=supported===true && !flat && !lost;
  const move=(yaw=0,pitch=0,zoom=0)=>setPose(old=>changeCamera(old,yaw,pitch,zoom));
  return <div ref={host} className="twin-viewport" data-reduced-motion={reduced || pauseMotion}>
    <div className="camera-controls" aria-label="Cámara del Digital Twin">
      <button onClick={()=>setFlat(old=>!old)} aria-pressed={flat}>{flat ? 'Ver 3D' : 'Ver 2D'}</button>
      <button disabled={!three} onClick={()=>move(-0.2)} aria-label="Rotar cámara izquierda">↶</button>
      <button disabled={!three} onClick={()=>move(0.2)} aria-label="Rotar cámara derecha">↷</button>
      <button disabled={!three} onClick={()=>move(0,-0.1)} aria-label="Bajar cámara">↓</button>
      <button disabled={!three} onClick={()=>move(0,0.1)} aria-label="Elevar cámara">↑</button>
      <button disabled={!three} onClick={()=>move(0,0,-1)} aria-label="Acercar cámara">＋</button>
      <button disabled={!three} onClick={()=>move(0,0,1)} aria-label="Alejar cámara">−</button>
      <button disabled={!three} onClick={()=>setPose({...cameraHome})}>Restablecer cámara</button>
      <button aria-pressed={pauseMotion} onClick={()=>setPauseMotion(old=>!old)}>Pausar animación</button>
    </div>
    {three ? <RenderBoundary fallback={fallback} onFailure={()=>setLost(true)}><div className="scene-stage" tabIndex={0} role="group" aria-label="Carroza 3D interactiva" aria-describedby="camera-help"
      onKeyDown={event=>{
        const keys:Record<string,()=>void>={ArrowLeft:()=>move(-0.2),ArrowRight:()=>move(0.2),ArrowUp:()=>move(0,0.1),ArrowDown:()=>move(0,-0.1),'+':()=>move(0,0,-1),'-':()=>move(0,0,1),Home:()=>setPose({...cameraHome})};
        if(keys[event.key]) {event.preventDefault();keys[event.key]();}
      }}
      onPointerDown={event=>{if(event.isPrimary){pointer.current={id:event.pointerId,x:event.clientX,y:event.clientY};event.currentTarget.setPointerCapture(event.pointerId);}}}
      onPointerMove={event=>{const p=pointer.current;if(!p || p.id!==event.pointerId)return;move((event.clientX-p.x)*0.008,(event.clientY-p.y)*0.006);pointer.current={id:p.id,x:event.clientX,y:event.clientY};}}
      onPointerUp={()=>{pointer.current=null;}} onPointerCancel={()=>{pointer.current=null;}}
    >
      <Canvas dpr={[1,1.5]} camera={{fov:38,near:0.1,far:100}} frameloop={visible && model.moving && !model.stale && !reduced && !pauseMotion ? 'always' : 'demand'} fallback={fallback}>
        <ContextGuard onLost={()=>setLost(true)}/>
        <Suspense fallback={null}><Scene model={model} pose={pose} reducedMotion={reduced || pauseMotion}/></Suspense>
      </Canvas>
    </div></RenderBoundary> : fallback}
    <p id="camera-help" className="camera-help">{three ? '3D · Arrastrá para rotar. Flechas: cámara · +/−: zoom · Inicio: restablecer.' : 'Representación 2D disponible sin WebGL.'} {(reduced || pauseMotion) && 'Movimiento reducido: ruedas estáticas; posiciones y estados confirmados visibles.'}</p>
  </div>;
}
