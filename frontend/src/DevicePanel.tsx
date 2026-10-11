import type { Device, Command } from './api';
import { HydraulicControls, ServoControls } from './PositionControls';
import { LightBank } from './LightBank';
import { MotorControls } from './MotorControls';
export function DevicePanel({devices,disabled,componentErrors,send}:{devices:Device[];disabled:boolean;componentErrors:Record<string,string>;send:(device:Device,command:Command)=>Promise<void>}) {
return <section id="controls" className="control-panel" aria-label="Control manual"><div className="section-heading"><div><p className="eyebrow">OPERACIÓN</p><h2>Control manual</h2></div><span>Estados confirmados</span></div>    <section className="devices" aria-label="Componentes">
      {devices.filter(device => device.type !== 'light_bank').map(device => <article key={device.id} aria-label={device.name}>
        <p className="eyebrow">{device.type === 'light' ? 'ILUMINACIÓN' : 'MOVIMIENTO'}</p><h2>{device.name}</h2><p className={device.online ? "device-online" : "failure"}>{device.online ? "ONLINE" : "DEVICE OFFLINE"}</p>{componentErrors[device.id] && <p className="error" role="alert">{componentErrors[device.id]}</p>}
        {device.type === 'light' ? <>
          <div className="lamp-stage"><div className={`lamp ${device.state === 'on' ? 'on' : ''}`} role="img" aria-label={`${device.name}: ${device.state === 'on' ? 'encendidas' : 'apagadas'}`}/></div>
          <p className="state">{device.state === 'on' ? 'Encendidas' : 'Apagadas'}</p>
          <div className="actions"><button disabled={disabled || !device.online} onClick={() => void send(device, { action: 'on' })}>Encender</button><button className="secondary" disabled={disabled || !device.online} onClick={() => void send(device, { action: 'off' })}>Apagar</button></div>
        </> : device.type === "HYDRAULIC_ACTUATOR" ? <HydraulicControls device={device} disabled={disabled || !device.online} send={send} /> : device.type === "SERVO" ? <ServoControls device={device} disabled={disabled || !device.online} send={send} /> : <MotorControls device={device} disabled={disabled || !device.online} send={send} />}
      </article>)}
    </section>
    {devices.filter(device => device.type === 'light_bank').map(device => <LightBank error={componentErrors[device.id]} key={device.id} device={device} disabled={disabled || !device.online} send={send} />)}
</section>; }
