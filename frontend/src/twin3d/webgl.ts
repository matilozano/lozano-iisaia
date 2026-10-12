// Capability detection only. The renderer still handles creation errors/context loss.
export function webGLAvailable(createCanvas = () => document.createElement('canvas')): boolean {
  try {
    const gl = createCanvas().getContext('webgl2');
    if (!gl) return false;
    gl.getExtension('WEBGL_lose_context')?.loseContext();
    return true;
  } catch { return false; }
}

export function watchContextLoss(target: EventTarget, onLost: () => void): () => void {
  target.addEventListener('webglcontextlost', onLost);
  return () => target.removeEventListener('webglcontextlost', onLost);
}
