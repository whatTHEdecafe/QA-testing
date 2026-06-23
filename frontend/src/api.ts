import type { ApiProblem, DashboardSummary, Target, TargetInput, ScanSummary, ScanDetails } from './types'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'

export class ApiError extends Error {
  constructor(message: string, public readonly fieldErrors: Record<string, string[]> = {}) { super(message) }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as ApiProblem
    throw new ApiError(problem.detail ?? problem.title ?? `Request failed (${response.status}).`, problem.errors)
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

export const api = {
  dashboard: () => request<DashboardSummary>('/dashboard/summary'),
  targets: () => request<Target[]>('/targets'),
  createTarget: (target: TargetInput) => request<Target>('/targets', { method: 'POST', body: JSON.stringify(target) }),
  updateTarget: (id: string, target: TargetInput) => request<Target>(`/targets/${id}`, { method: 'PUT', body: JSON.stringify(target) }),
  deleteTarget: (id: string) => request<void>(`/targets/${id}`, { method: 'DELETE' }),
  scans: () => request<ScanSummary[]>('/scans'),
  scan: (id: string) => request<ScanDetails>(`/scans/${id}`),
  startScan: (targetId: string) => request<{id:string}>(`/targets/${targetId}/scans`, { method: 'POST' }),
  cancelScan: (id: string) => request<ScanSummary>(`/scans/${id}/cancel`, { method: 'POST' }),
}
