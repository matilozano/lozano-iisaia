import type { CarrozaModel } from './model';

export const cameraHome = { yaw: 0.55, pitch: 0.35, distance: 11 };
export type CameraPose = typeof cameraHome;
export function changeCamera(pose: CameraPose, yaw: number, pitch: number, zoom: number): CameraPose {
  return { yaw: pose.yaw + yaw, pitch: Math.max(0.15, Math.min(1.2, pose.pitch + pitch)),
    distance: Math.max(7, Math.min(18, pose.distance + zoom)) };
}

// Geometry and decorative rotation only; no effect generator or physical extrapolation.
export function toSceneState(model: CarrozaModel, reducedMotion: boolean) {
  return { front: model.front, side: model.side, channels: model.channels,
    platformY: 1.25 + (model.position ?? 0) * 0.015,
    servoRadians: (model.angle ?? 0) * Math.PI / 180,
    wheelRate: model.moving && !model.stale && !reducedMotion
      ? Math.PI * model.speed / 30 * (model.direction === 'reverse' ? -1 : 1) : 0 };
}
