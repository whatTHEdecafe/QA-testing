export type TargetEnvironment = 'Development' | 'Staging' | 'Production'

export interface Target {
  id: string
  name: string
  startingUrl: string
  allowedHost: string
  environment: TargetEnvironment
  description: string | null
  isEnabled: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export type TargetInput = Omit<Target, 'id' | 'createdAtUtc' | 'updatedAtUtc'>
export interface DashboardSummary { totalTargets: number; enabledTargets: number }
export interface ApiProblem { title?: string; detail?: string; errors?: Record<string, string[]> }
