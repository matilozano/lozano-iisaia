import type { Device } from './api';
import type { SequenceExecution, SequenceEvent } from './Sequences';
import type { Telemetry } from './Telemetry';
export async function pollSnapshot(read:<T>(path:string)=>Promise<T>) {
  const [devices,execution,events,telemetry]=await Promise.allSettled([read<Device[]>('devices'),read<SequenceExecution>('sequences/execution'),read<SequenceEvent[]>('sequences/events'),read<Telemetry>('telemetry')]);
  return {devices,execution,events,telemetry};
}
