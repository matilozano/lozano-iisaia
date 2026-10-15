export const ORDER_KEY = 'fne.component-order.v1';

export function reconcileOrder(saved: unknown, current: string[]): string[] {
  const valid = Array.isArray(saved) ? saved.filter((id): id is string => typeof id === 'string' && current.includes(id)) : [];
  return [...new Set([...valid, ...current])];
}

export function readOrder(storage: Pick<Storage, 'getItem'>): unknown {
  try { return JSON.parse(storage.getItem(ORDER_KEY) ?? '[]'); }
  catch { return []; }
}

export function saveOrder(storage: Pick<Storage, 'setItem'>, ids: string[]): boolean {
  try { storage.setItem(ORDER_KEY, JSON.stringify(ids)); return true; }
  catch { return false; }
}

export function moveCard(ids: string[], active: string, over: string | null): string[] {
  const from = ids.indexOf(active), to = over === null ? -1 : ids.indexOf(over);
  if (from < 0 || to < 0 || from === to) return ids;
  const next = [...ids];
  next.splice(to, 0, next.splice(from, 1)[0]);
  return next;
}
