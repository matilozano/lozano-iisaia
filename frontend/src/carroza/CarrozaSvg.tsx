import type { CarrozaModel } from './model.js';
import { HydraulicVisual, LedBankVisual, LightVisual, ServoVisual, WheelsVisual } from './Visuals.js';

export function CarrozaSvg({ model }: { model: CarrozaModel }) {
  return <svg className="carroza-svg" viewBox="0 0 960 480" role="img" aria-label="Carroza FNE de perfil: luces, banco de ocho LED, tracción, plataforma hidráulica y pieza servo">
    <path d="M50 446H910" stroke="#3f626f" strokeWidth="2" />
    <path d="M65 461H155 M245 461H335 M425 461H515 M605 461H695 M785 461H875" stroke="#28414d" strokeWidth="3" />
    <text x="65" y="50" className="twin-caption">FIESTA NACIONAL DE LOS ESTUDIANTES</text>
    <text x="65" y="77" className="twin-subcaption">CARROZA TÉCNICA / PERFIL LATERAL</text>
    <HydraulicVisual model={model} /><ServoVisual angle={model.angle} />
    <path d="M130 274H793L830 321V372H130Z" fill="#22404d" stroke="#648996" strokeWidth="3" />
    <path d="M148 339H805V379H148Z" fill="#172e3a" stroke="#54727e" strokeWidth="3" />
    <path d="M148 351H805" stroke="#8adbbe" strokeWidth="3" />
    <path d="M151 276L180 242H272V276" fill="#335663" stroke="#648996" strokeWidth="3" />
    <text x="182" y="307" fill="#aac9d0" fontSize="20">FNE</text>
    <LedBankVisual channels={model.channels} />
    <LightVisual x={811} y={321} on={model.front} label="Frontal" />
    {[190, 470, 755].map(x => <LightVisual key={x} x={x} y={359} on={model.side} small label="Lateral" />)}
    <WheelsVisual model={model} />
    <rect x="400" y="372" width="160" height="37" rx="8" fill="#142a34" stroke="#58818a" />
    <text x="480" y="396" textAnchor="middle" fill="#a0e2ce" fontSize="16">{model.moving ? `${model.direction === 'reverse' ? '←' : '→'} ${model.speed}%` : 'TRACCIÓN • STOP'}</text>
    <text x="60" y="423" className="twin-caption">ATRÁS</text><text x="838" y="423" className="twin-caption">FRENTE</text>
  </svg>;
}
