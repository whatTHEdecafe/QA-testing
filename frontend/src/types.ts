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
export type ElementClassification = 'Informational'|'Navigational'|'Input'|'Action'|'Submission'|'Upload'|'DateOrTime'|'PotentiallyDestructive'|'UnknownCustomControl'
export type DiagnosticCategory = 'BrowserConsoleError'|'BrowserConsoleWarning'|'PageError'|'FailedNetworkRequest'|'HttpResponseError'|'NavigationError'|'ScreenshotError'|'ScannerWarning'
export type DiagnosticSeverity = 'Information'|'Warning'|'Error'

export interface PagedResponse<T> { items:T[]; pageNumber:number; pageSize:number; totalCount:number }
export interface ScanSummary { id:string; targetId:string; targetName:string; status:ScanStatus; stage:string; requestedAtUtc:string; startedAtUtc:string|null; completedAtUtc:string|null; startingUrl:string; finalUrl:string|null; pageTitle:string|null; detectedPageCount:number; detectedElementCount:number; warningCount:number; errorCount:number; failureSummary:string|null; cancellationRequested:boolean }
export interface Selector { id:string; type:string; value:string; priority:number; wasUnique:boolean; confidence:number; isPreferred:boolean; isScannerPreferred:boolean; isManualPreferred:boolean; isEffectivePreferred:boolean }
export interface DetectedElement { id:string; pageId:string; pageDisplayName:string; discoveryOrder:number; tagName:string; inputType:string|null; accessibleRole:string|null; accessibleName:string|null; visibleText:string|null; associatedLabel:string|null; placeholder:string|null; nameAttribute:string|null; htmlId:string|null; testId:string|null; classification:ElementClassification; effectiveClassification:ElementClassification; classificationOverride:ElementClassification|null; userDisplayName:string|null; displayName:string; hasManualReview:boolean; isActionable:boolean; isVisible:boolean; isEnabled:boolean; isPotentiallyDestructive:boolean; hasCrop:boolean; screenshotError:string|null; manualPreferredSelectorCandidateId:string|null; selectors:Selector[] }
export interface ScannedPage { id:string; originalUrl:string; finalUrl:string; route:string; title:string|null; mainHeading:string|null; generatedDisplayName:string; userDisplayName:string|null; displayName:string; discoveryOrder:number; hasScreenshot:boolean; hasThumbnail:boolean; screenshotWidth:number|null; screenshotHeight:number|null; reviewUpdatedAtUtc:string|null; elements:DetectedElement[] }
export interface Diagnostic { id:string; category:DiagnosticCategory; severity:DiagnosticSeverity; message:string; url:string|null; method:string|null; statusCode:number|null; createdAtUtc:string }
export interface ScanSettings { overallTimeoutSeconds:number; navigationTimeoutMilliseconds:number; actionTimeoutMilliseconds:number; maximumDetectedElements:number; maximumDiagnosticRecords:number; elementScreenshotPadding:number; viewportWidth:number; viewportHeight:number }
export interface SettingLimit { default:number; min:number; max:number }
export interface ScannerSettingsMetadata { overallTimeoutSeconds:SettingLimit; navigationTimeoutMilliseconds:SettingLimit; actionTimeoutMilliseconds:SettingLimit; maximumDetectedElements:SettingLimit; maximumDiagnosticRecords:SettingLimit; elementScreenshotPadding:SettingLimit; viewportWidth:SettingLimit; viewportHeight:SettingLimit; fixedSafetyRules:string[] }
export interface ScanDetails { summary:ScanSummary; pages:ScannedPage[]; diagnostics:Diagnostic[]; settings:ScanSettings }
