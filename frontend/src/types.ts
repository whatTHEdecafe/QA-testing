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

export type ScanStatus = 'Queued'|'Running'|'Completed'|'Failed'|'Cancelled'
export interface ScanSummary { id:string; targetId:string; targetName:string; status:ScanStatus; stage:string; requestedAtUtc:string; startedAtUtc:string|null; completedAtUtc:string|null; startingUrl:string; finalUrl:string|null; pageTitle:string|null; detectedPageCount:number; detectedElementCount:number; warningCount:number; errorCount:number; failureSummary:string|null; cancellationRequested:boolean }
export interface Selector { id:string; type:string; value:string; priority:number; wasUnique:boolean; confidence:number; isPreferred:boolean }
export interface DetectedElement { id:string; discoveryOrder:number; tagName:string; inputType:string|null; accessibleRole:string|null; accessibleName:string|null; visibleText:string|null; associatedLabel:string|null; placeholder:string|null; classification:string; isActionable:boolean; isVisible:boolean; isEnabled:boolean; isPotentiallyDestructive:boolean; hasCrop:boolean; screenshotError:string|null; selectors:Selector[] }
export interface ScannedPage { id:string; finalUrl:string; title:string|null; mainHeading:string|null; displayName:string; hasScreenshot:boolean; hasThumbnail:boolean; screenshotWidth:number|null; screenshotHeight:number|null; elements:DetectedElement[] }
export interface Diagnostic { id:string; category:string; severity:string; message:string; url:string|null; method:string|null; statusCode:number|null; createdAtUtc:string }
export interface ScanDetails { summary:ScanSummary; pages:ScannedPage[]; diagnostics:Diagnostic[] }
