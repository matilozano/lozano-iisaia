import { useLayoutEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import type { Group } from 'three';
import { toSceneState, type CameraPose } from '../carroza/sceneModel';
import type { CarrozaModel } from '../carroza/model';

function Box({ position, scale, color }: { position: [number,number,number]; scale: [number,number,number]; color: string }) {
  return <mesh position={position}><boxGeometry args={scale}/><meshStandardMaterial color={color} metalness={0.35} roughness={0.5}/></mesh>;
}
function Lamp({position,on,color='#ffe2a0'}:{position:[number,number,number];on:boolean;color?:string}) {
  return <mesh position={position}><sphereGeometry args={[0.1,12,8]}/><meshStandardMaterial color={on ? color : '#263d46'} emissive={color} emissiveIntensity={on ? 2.5 : 0}/></mesh>;
}
function Wheel({x,z,rate}:{x:number;z:number;rate:number}) {
  const wheel=useRef<Group>(null);
  useFrame((_,delta)=>{ if(wheel.current && rate) wheel.current.rotation.z-=rate*Math.min(delta,0.05); });
  return <group position={[x,0.5,z]} ref={wheel}>
    <mesh rotation={[Math.PI/2,0,0]}><cylinderGeometry args={[0.48,0.48,0.26,24]}/><meshStandardMaterial color="#172631"/></mesh>
    <mesh rotation={[Math.PI/2,0,0]}><cylinderGeometry args={[0.31,0.31,0.28,16]}/><meshStandardMaterial color="#70bcae" metalness={0.65} roughness={0.3}/></mesh>
    {[0,Math.PI/3,2*Math.PI/3].map(a=><group key={a} rotation={[0,0,a]}><Box position={[0,0,0]} scale={[0.06,0.66,0.3]} color="#d2e9df"/></group>)}
  </group>;
}
export default function CarrozaScene({model,pose,reducedMotion}:{model:CarrozaModel;pose:CameraPose;reducedMotion:boolean}) {
  const {camera,invalidate}=useThree();
  const state=toSceneState(model,reducedMotion);
  useLayoutEffect(()=>{
    camera.position.set(Math.sin(pose.yaw)*pose.distance, 1.4+Math.sin(pose.pitch)*pose.distance, Math.cos(pose.yaw)*pose.distance);
    camera.lookAt(0,1.4,0); camera.updateProjectionMatrix(); invalidate();
  },[camera,invalidate,pose]);
  return <>
    <ambientLight intensity={1.8}/><directionalLight position={[4,8,5]} intensity={3}/><directionalLight position={[-5,2,-3]} intensity={1} color="#6ccec5"/>
    <Box position={[0,0.04,0]} scale={[12,0.08,6]} color="#152b37"/>
    <gridHelper args={[12,24,'#355666','#213e4c']} position={[0,0.09,0]}/>
    <Box position={[0,0.85,0]} scale={[6.6,0.3,2]} color="#315866"/>
    <Box position={[0,1.05,0]} scale={[6.4,0.15,2.2]} color="#8ac7b5"/>
    <Box position={[-2.5,1.4,0]} scale={[1,0.6,1.8]} color="#467988"/>
    {[-2.1,2.1].flatMap(x=>[-1.05,1.05].map(z=><Wheel key={`${x}/${z}`} x={x} z={z} rate={state.wheelRate}/>))}
    {/* Hydraulic rod and platform use the confirmed position directly. */}
    {[-0.8,0.8].map(z=><group key={z}>
      <Box position={[0,(1.1+state.platformY)/2,z]} scale={[0.14,state.platformY-1.1,0.14]} color="#b4cdd0"/>
      <Box position={[0,1.2,z]} scale={[0.25,0.35,0.25]} color="#d6ac5f"/>
    </group>)}
    <group position={[0,state.platformY,0]}>
      <Box position={[0,0,0]} scale={[2.4,0.18,1.8]} color="#79c6af"/>
      <mesh position={[0,0.6,0]}><coneGeometry args={[0.7,1.2,5]}/><meshStandardMaterial color="#59a992" metalness={0.4} roughness={0.4}/></mesh>
      <mesh position={[0,0.65,0.55]} rotation={[Math.PI/2,0,0]}><torusGeometry args={[0.25,0.035,8,24]}/><meshStandardMaterial color="#f0cf83"/></mesh>
    </group>
    <Box position={[2.25,1.5,0]} scale={[0.2,0.8,0.2]} color="#d0c291"/>
    <group position={[2.25,1.85,0]} rotation={[0,0,state.servoRadians]}>
      <Box position={[0.5,0,0]} scale={[1,0.13,0.16]} color="#efc96f"/>
      <mesh position={[1,0,0]}><coneGeometry args={[0.18,0.35,3]}/><meshStandardMaterial color="#ffe2a0"/></mesh>
    </group>
    <Box position={[0,0.63,0]} scale={[0.9,0.32,0.7]} color={model.moving ? '#74cdb2' : '#334c58'}/>
    {[-1.12,1.12].map(z=><group key={z}>
      <Box position={[0,1.28,z]} scale={[3.3,0.3,0.08]} color="#0c1a23"/>
      {state.channels.map((on,i)=><Lamp key={i} position={[-1.4+i*0.4,1.28,z*1.05]} on={on}/>)}
      {[-2.4,0,2.4].map(x=><Lamp key={x} position={[x,0.92,z*1.02]} on={state.side} color="#94e8d3"/>)}
    </group>)}
    {[-0.7,0.7].map(z=><Lamp key={z} position={[3.33,0.9,z]} on={state.front}/>)}
  </>;
}
