import type { ApiProblem, DashboardSummary, Target, TargetInput, ScanSummary, ScanDetails, PagedResponse, DetectedElement, Diagnostic, ScannerSettingsMetadata, ScanSettings, ElementClassification } from './types'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'

export class ApiError extends Error {
  constructor(message: string, public readonly fieldErrors: Record<string, string[]> = {}) { super(message) }
}

function query(params: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  const value = search.toString()
  return value ? `?${value}` : ''
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
  scans: (filters: Record<string, string | number | boolean | null | undefined> = {}) => request<PagedResponse<ScanSummary>>(`/scans${query(filters)}`),
  scan: (id: string) => request<ScanDetails>(`/scans/${id}`),
  scanElements: (id: string, filters: Record<string, string | number | boolean | null | undefined>) => request<PagedResponse<DetectedElement>>(`/scans/${id}/elements${query(filters)}`),
  scanDiagnostics: (id: string, filters: Record<string, string | number | boolean | null | undefined>) => request<PagedResponse<Diagnostic>>(`/scans/${id}/diagnostics${query(filters)}`),
  scannerSettings: () => request<ScannerSettingsMetadata>('/scans/settings'),
  startScan: (targetId: string, settings?: ScanSettings) => request<{id:string}>(`/targets/${targetId}/scans`, { method: 'POST', body: JSON.stringify({ settings }) }),
  cancelScan: (id: string) => request<ScanSummary>(`/scans/${id}/cancel`, { method: 'POST' }),
  updatePageReview: (scanId: string, pageId: string, displayName: string | null) => request(`/scans/${scanId}/pages/${pageId}/review`, { method: 'PUT', body: JSON.stringify({ displayName }) }),
  updateElementReview: (scanId: string, elementId: string, displayName: string | null, classificationOverride: ElementClassification | null) => request<DetectedElement>(`/scans/${scanId}/elements/${elementId}/review`, { method: 'PUT', body: JSON.stringify({ displayName, classificationOverride }) }),
  selectManualSelector: (scanId: string, elementId: string, selectorCandidateId: string | null) => request<DetectedElement>(`/scans/${scanId}/elements/${elementId}/manual-selector`, { method: 'PUT', body: JSON.stringify({ selectorCandidateId }) }),
}
