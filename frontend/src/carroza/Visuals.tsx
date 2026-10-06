import type { CarrozaModel } from './model.js';

export function LightVisual({ x, y, on, label, small = false }: { x: number; y: number; on: boolean; label: string; small?: boolean }) {
  return <g transform={`translate(${x} ${y})`} role="img" aria-label={`${label}: ${on ? 'ON' : 'OFF'}`}>
    {on && <circle r={small ? 19 : 32} className="twin-halo" />}
    <circle r={small ? 8 : 14} className={`twin-light ${on ? 'is-on' : ''}`} />
  </g>;
}
export function LedBankVisual({ channels }: { channels: boolean[] }) {
  return <g><rect x="283" y="279" width="360" height="53" rx="12" fill="#0b1926" stroke="#56747e" />
    {channels.map((on, index) => <g key={index}><LightVisual x={310 + index * 44} y={298} on={on} small label={`CH${String(index + 1).padStart(2, '0')}`} /><text x={310 + index * 44} y="322" textAnchor="middle" className="twin-channel">{String(index + 1).padStart(2, '0')}</text></g>)}
  </g>;
}
export function WheelsVisual({ model }: { model: CarrozaModel }) {
  return <g aria-label={`Tracción: ${model.motor}, ${model.direction}, ${model.speed}%`} role="img">
    {[245, 715].map(x => <g key={x} transform={`translate(${x} 397)`}>
      <circle r="49" fill="#0a1119" stroke="#57737e" strokeWidth="5" />
      <g className="twin-wheel" style={{ animationDuration: `${model.wheelSeconds}s`, animationDirection: model.wheelDirection, animationPlayState: model.moving ? 'running' : 'paused' }}>
        <circle r="33" fill="#243f4a" stroke="#91b4b8" strokeWidth="3" />
        {[0, 60, 120].map(angle => <path key={angle} d="M-30 0H30" transform={`rotate(${angle})`} stroke="#91b4b8" strokeWidth="5" />)}
      </g><circle r="9" fill="#88ddc0" />
    </g>)}
  </g>;
}
export function HydraulicVisual({ model }: { model: CarrozaModel }) {
  return <g role="img" aria-label={`Plataforma hidráulica: ${model.position ?? 'sin datos'}%, ${model.movement}`}>
    <path d={`M400 275V${229 - model.lift} M560 275V${229 - model.lift}`} stroke="#b3cbd1" strokeWidth="10" />
    <path d="M400 280V248 M560 280V248" stroke="#608491" strokeWidth="24" />
    <g transform={`translate(0 ${-model.lift})`}>
      <path d="M350 230H610" stroke="#8adbbe" strokeWidth="12" strokeLinecap="round" />
      <path d="M372 223L392 163L420 194L480 134L540 194L568 163L588 223Z" fill="#30585e" stroke="#89dbc0" strokeWidth="3" />
      <circle cx="480" cy="187" r="22" fill="#15343c" stroke="#dab776" strokeWidth="3" />
      <text x="480" y="194" textAnchor="middle" fill="#f8df9e" fontSize="18">FNE</text>
    </g>
  </g>;
}
export function ServoVisual({ angle }: { angle: number | null }) {
  return <g role="img" aria-label={`Pieza servo: ${angle ?? 'sin datos'} grados`}>
    <path d="M715 275V223" stroke="#6b909b" strokeWidth="12" />
    <path d="M635 223 A80 80 0 0 1 795 223" fill="none" stroke="#3c5965" strokeDasharray="4 6" />
    <g transform={`rotate(${angle ?? 0} 715 223)`}>
      <path d="M715 223H640" stroke="#e8bd7e" strokeWidth="9" strokeLinecap="round" />
      <path d="M640 223L650 198L624 204Z" fill="#f4d69d" />
    </g><circle cx="715" cy="223" r="12" fill="#203c49" stroke="#f4d69d" strokeWidth="3" />
  </g>;
}
