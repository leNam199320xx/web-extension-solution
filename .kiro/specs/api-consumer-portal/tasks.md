# Implementation Plan: API Consumer Portal

## Overview

This plan implements the API Consumer Portal — a Blazor WebAssembly Standalone application (.NET 10) using MudBlazor, OIDC authentication, typed HttpClient services, Chart.js interop, and Stripe Checkout/Portal redirects. The implementation proceeds from project scaffolding and core infrastructure (auth, API client, caching) through page-by-page feature delivery, ending with integration wiring and final validation.

## Tasks

- [ ] 1. Set up project structure and core infrastructure
  - [ ] 1.1 Create Blazor WASM Standalone project and configure solution
    - Create `src/ConsumerPortal/PluginRuntime.ConsumerPortal` Blazor WebAssembly Standalone project targeting .NET 10
    - Add NuGet references: MudBlazor, Microsoft.AspNetCore.Components.WebAssembly.Authentication, Microsoft.Extensions.Http.Resilience, FsCheck, FsCheck.Xunit, bunit, xunit
    - Configure `wwwroot/index.html` with MudBlazor CSS/JS references and Chart.js CDN
    - Set up `Program.cs` with MudBlazor services, auth, and HttpClient DI registration
    - Create directory structure: Layout/, Pages/, Components/, Services/, Models/DTOs/, Models/Requests/, Models/ViewModels/, State/, Auth/, Interop/, Caching/
    - _Requirements: 11.1, 13.2, 13.3_

  - [ ] 1.2 Define all DTOs, request models, and view models
    - Create all response DTOs in `Models/DTOs/`: TenantDto, PlanDto, ApiKeyListDto, ApiKeyGenerationResult, UsageAggregateDto, UsageSummaryDto, DashboardUsageDto, RecentActivityDto, InvoiceDto, InvoiceDetailDto, DailyOverageDto, BillingSummaryDto, PlanChangeResult, TenantRegistrationResult, SupportTicketResult, PaginatedResult<T>, ApiErrorResponse, ApiError
    - Create all request models in `Models/Requests/`: TenantRegistrationRequest, UpdateProfileRequest, NotificationPreferencesRequest, PlanChangeRequest, GenerateKeyRequest, SupportTicketRequest
    - Create view models in `Models/ViewModels/`: DashboardViewModel, OnboardingState
    - Create `ApiResult<T>` result pattern type for consistent error handling
    - _Requirements: 12.2, 2.2, 5.1, 6.1, 7.1_

  - [ ] 1.3 Implement service interfaces
    - Create `Services/ITenantService.cs`, `Services/IPlanService.cs`, `Services/IApiKeyService.cs`, `Services/IUsageService.cs`, `Services/IInvoiceService.cs`, `Services/ISupportService.cs`
    - All methods must accept `CancellationToken` parameter
    - All methods must return `ApiResult<T>` for consistent error handling
    - _Requirements: 12.1, 12.6_

- [ ] 2. Implement authentication and API client infrastructure
  - [ ] 2.1 Implement OIDC authentication configuration
    - Configure OIDC in `Program.cs` using `AddOidcAuthentication` with API_Consumer role claim mapping
    - Create `Auth/ConsumerAuthStateProvider.cs` extending `RemoteAuthenticationState` to validate API_Consumer role
    - Create `Auth/RoleRequirement.cs` for role-based authorization policy
    - Configure `CascadingAuthenticationState` in App.razor
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [ ] 2.2 Implement ApiAuthorizationMessageHandler
    - Create `Auth/ApiAuthorizationMessageHandler.cs` as a `DelegatingHandler`
    - Attach Bearer token from `IAccessTokenProvider` to all outgoing requests
    - Generate and attach `X-Correlation-Id` header (GUID) to each request
    - Handle 401 responses by triggering re-authentication flow
    - Handle 429 responses by extracting Retry-After header and surfacing to UI via `ApiResult`
    - _Requirements: 1.4, 12.5, 12.6_

  - [ ]* 2.3 Write property test for authenticated request headers
    - **Property 1: Authenticated Request Headers**
    - **Validates: Requirements 1.4, 12.6**

  - [ ] 2.4 Implement retry policy and HttpClient configuration
    - Configure `Microsoft.Extensions.Http.Resilience` (Polly v8) for transient failure retry
    - Max 3 retries with exponential backoff (1s, 2s, 4s) and jitter for HTTP 502, 503, 504
    - Register typed HttpClient with base URL and `ApiAuthorizationMessageHandler`
    - Set 15-second timeout for all API requests
    - _Requirements: 12.7, 12.1_

  - [ ]* 2.5 Write property test for transient failure retry
    - **Property 23: Transient Failure Retry**
    - **Validates: Requirements 12.7**

  - [ ]* 2.6 Write property test for role-based access control
    - **Property 2: Role-Based Access Control**
    - **Validates: Requirements 1.2, 1.3**

  - [ ]* 2.7 Write property test for route protection
    - **Property 3: Route Protection**
    - **Validates: Requirements 1.1, 11.3**

- [ ] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 4. Implement response caching and state management
  - [ ] 4.1 Implement ResponseCacheService
    - Create `Caching/ResponseCacheService.cs` with `ConcurrentDictionary` storage
    - Implement `GetOrFetchAsync<T>` with 60-second staleness threshold
    - Implement `Invalidate(key)` to clear specific cache entries
    - Register as singleton in DI
    - _Requirements: 13.7_

  - [ ]* 4.2 Write property test for cache staleness
    - **Property 25: Response Cache Staleness**
    - **Validates: Requirements 13.7**

  - [ ] 4.3 Implement TenantState and NotificationState
    - Create `State/TenantState.cs` as scoped service holding current TenantDto and plan info
    - Create `State/NotificationState.cs` tracking quota warning state (>80% threshold)
    - Wire state services into DI in `Program.cs`
    - _Requirements: 11.2, 11.7, 3.6_

- [ ] 5. Implement application shell and navigation
  - [ ] 5.1 Implement MainLayout and sidebar navigation
    - Create `Layout/MainLayout.razor` with MudBlazor `MudLayout`, `MudAppBar`, `MudDrawer`
    - Create `Layout/SidebarNavMenu.razor` with `MudNavMenu` containing links to: Dashboard, API Keys, Usage Analytics, Plans, Billing, Documentation, Settings, Support
    - Implement responsive collapse to hamburger menu below 960px viewport width
    - Display tenant name and current plan badge in navigation header when authenticated
    - _Requirements: 11.1, 11.2, 11.4_

  - [ ] 5.2 Implement AuthLayout and route protection
    - Create `Layout/AuthLayout.razor` for unauthenticated pages (login, public docs)
    - Configure route authorization via `[Authorize]` attribute on protected pages
    - Implement `RedirectToLogin` component for unauthenticated users
    - Create `Pages/AccessDenied.razor` for users without API_Consumer role
    - _Requirements: 1.1, 1.3, 11.3_

  - [ ] 5.3 Implement shared UI components
    - Create `Components/LoadingSection.razor` with skeleton/spinner variants
    - Create `Components/ErrorBanner.razor` with dismissible notification, error code, message, retry action
    - Create `Components/CopyButton.razor` with clipboard API, visual confirmation, aria-live announcement
    - Create `Components/NotificationBadge.razor` for sidebar badge overlays
    - _Requirements: 12.2, 12.3, 12.4, 13.6, 11.7_

  - [ ]* 5.4 Write property test for navigation header context
    - **Property 24: Navigation Header Context**
    - **Validates: Requirements 11.2**

  - [ ]* 5.5 Write property test for quota warning threshold
    - **Property 10: Quota Warning Threshold**
    - **Validates: Requirements 3.6, 11.7**

  - [ ]* 5.6 Write property test for error notification display
    - **Property 21: Error Notification Display**
    - **Validates: Requirements 12.2**

  - [ ]* 5.7 Write property test for rate limit handling
    - **Property 22: Rate Limit Handling**
    - **Validates: Requirements 12.5**

  - [ ]* 5.8 Write property test for copy-to-clipboard accessible feedback
    - **Property 26: Copy-to-Clipboard Accessible Feedback**
    - **Validates: Requirements 13.6**

- [ ] 6. Implement registration and onboarding wizard
  - [ ] 6.1 Implement TenantService
    - Create `Services/TenantService.cs` implementing `ITenantService`
    - Implement `GetCurrentTenantAsync` → GET /api/tenants/me
    - Implement `RegisterAsync` → POST /api/tenants with TenantRegistrationRequest
    - Implement `UpdateProfileAsync` → PUT /api/tenants/me/profile
    - Implement `UpdateNotificationPreferencesAsync` → PUT /api/tenants/me/notifications
    - Handle error codes: TM-REG-001 (duplicate email), validation errors
    - Use `ResponseCacheService` for `GetCurrentTenantAsync`
    - _Requirements: 2.3, 2.9, 2.10, 9.2, 9.5_

  - [ ] 6.2 Implement OnboardingWizard with step components
    - Create `Pages/Onboarding/OnboardingWizard.razor` managing `OnboardingState` (step transitions)
    - Create `Pages/Onboarding/RegistrationStep.razor` with form: tenant name (1–200 chars validation), contact email (pre-filled from OIDC), optional company name
    - Create `Pages/Onboarding/PlanSelectionStep.razor` displaying Free, Pro, Enterprise with feature comparison
    - Create `Pages/Onboarding/KeyGenerationStep.razor` auto-generating first API key with plaintext display, copy button, and one-time warning
    - Create `Pages/Onboarding/QuickStartStep.razor` displaying Quick_Start_Guide with code example
    - Implement step navigation logic: register → plan → (Stripe if paid) → key → quick start
    - _Requirements: 2.1, 2.2, 2.4, 2.5, 2.6, 2.7, 2.8_

  - [ ]* 6.3 Write property test for tenant name validation
    - **Property 4: Tenant Name Validation**
    - **Validates: Requirements 2.2**

  - [ ]* 6.4 Write property test for validation error field mapping
    - **Property 6: Validation Error Field Mapping**
    - **Validates: Requirements 2.10, 9.3**

  - [ ]* 6.5 Write property test for API key display completeness
    - **Property 5: API Key Display Completeness**
    - **Validates: Requirements 2.7, 5.3**

- [ ] 7. Implement dashboard page
  - [ ] 7.1 Implement Dashboard page
    - Create `Pages/Dashboard.razor` fetching and displaying DashboardViewModel
    - Create `Components/QuotaUsageBar.razor` showing daily quota percentage with warning at >80%
    - Display plan name, rate limit, daily quota, total/success/failed requests
    - Display active key count and keys expiring within 7 days
    - Display 5 most recent activity entries (date, total requests, success rate)
    - Implement partial dashboard rendering with error indicators for failed sections
    - Use `ResponseCacheService` for dashboard data (60-second staleness)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 13.7_

  - [ ]* 7.2 Write property test for dashboard metrics rendering
    - **Property 7: Dashboard Metrics Rendering**
    - **Validates: Requirements 3.1, 3.2, 3.3**

  - [ ]* 7.3 Write property test for API key count computation
    - **Property 8: API Key Count Computation**
    - **Validates: Requirements 3.4, 5.8**

  - [ ]* 7.4 Write property test for recent activity truncation
    - **Property 9: Recent Activity Truncation**
    - **Validates: Requirements 3.5**

- [ ] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Implement plan selection and API key management pages
  - [ ] 9.1 Implement PlanService
    - Create `Services/PlanService.cs` implementing `IPlanService`
    - Implement `GetAllPlansAsync` → GET /api/plans
    - Implement `ChangePlanAsync` → POST /api/tenants/me/plan-changes with PlanChangeRequest
    - Implement `GetStripeCheckoutUrlAsync` → POST /api/tenants/me/checkout-sessions
    - Handle error codes: TM-CHANGE-001 (same plan)
    - _Requirements: 4.1, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_

  - [ ] 9.2 Implement Plans page
    - Create `Pages/Plans.razor` with plan comparison layout
    - Create `Components/PlanComparisonCard.razor` displaying: name, rate limit, daily quota, price, overage rate, features
    - Visually highlight user's current plan
    - Implement upgrade confirmation dialog (prorated cost, new monthly amount) with Stripe Checkout redirect
    - Implement downgrade confirmation dialog (effective at next billing period) via Billing API
    - Handle Free → Paid transition via Stripe Checkout for initial payment setup
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [ ]* 9.3 Write property test for plan comparison rendering
    - **Property 11: Plan Comparison Rendering**
    - **Validates: Requirements 4.1, 4.2**

  - [ ] 9.4 Implement ApiKeyService
    - Create `Services/ApiKeyService.cs` implementing `IApiKeyService`
    - Implement `GetKeysAsync` → GET /api/tenants/me/keys
    - Implement `GenerateKeyAsync` → POST /api/tenants/me/keys with GenerateKeyRequest
    - Implement `RotateKeyAsync` → POST /api/tenants/me/keys/{keyId}/rotate
    - Implement `RevokeKeyAsync` → DELETE /api/tenants/me/keys/{keyId}
    - Handle error codes: TM-KEY-001 (max keys), TM-KEY-002 (already revoked)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [ ] 9.5 Implement ApiKeys page
    - Create `Pages/ApiKeys.razor` displaying key list table
    - Create `Components/ApiKeyRow.razor` showing: key prefix, masked suffix, status, created date, expiration date, last used date, action buttons
    - Implement Generate New Key dialog (optional name, optional expiration 1–365 days)
    - Implement Rotate Key confirmation dialog with new plaintext display
    - Implement Revoke Key confirmation dialog
    - Display expiration warning badge on keys expiring within 7 days
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.8_

  - [ ]* 9.6 Write property test for key list rendering completeness
    - **Property 12: Key List Rendering Completeness**
    - **Validates: Requirements 5.1**

- [ ] 10. Implement usage analytics with Chart.js interop
  - [ ] 10.1 Implement Chart.js interop layer
    - Create `wwwroot/js/chartInterop.js` with functions: renderLineChart, updateData, destroy
    - Support datasets with border/background colors, responsive option, tooltip callbacks
    - Support horizontal reference line annotation (for quota limit)
    - Create `Interop/ChartInterop.cs` wrapping IJSRuntime calls with IAsyncDisposable cleanup
    - Define `ChartDataset` and `ChartOptions` record types
    - _Requirements: 6.1, 6.6, 6.7, 13.5_

  - [ ] 10.2 Implement UsageService
    - Create `Services/UsageService.cs` implementing `IUsageService`
    - Implement `GetDailyUsageAsync` → GET /api/tenants/me/usage?startDate={}&endDate={}
    - Implement `GetUsageSummaryAsync` → GET /api/tenants/me/usage/summary?startDate={}&endDate={}
    - Implement `GetDashboardUsageAsync` → GET /api/tenants/me/usage/dashboard
    - Use `ResponseCacheService` for dashboard usage data
    - _Requirements: 6.1, 6.4, 6.5_

  - [ ] 10.3 Implement UsageAnalytics page
    - Create `Pages/UsageAnalytics.razor` with date range picker (max 90 days)
    - Create `Components/UsageChart.razor` wrapping ChartInterop for reusable chart rendering
    - Render daily requests chart with quota reference line
    - Render success rate chart (successful/total × 100 per day)
    - Render average response time chart (avg_duration_ms per day)
    - Display tooltip on hover showing exact daily values
    - Display summary statistics: total requests, avg daily, total successful, total failed, avg response time
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ]* 10.4 Write property test for usage chart data correctness
    - **Property 13: Usage Chart Data Correctness**
    - **Validates: Requirements 6.1, 6.2, 6.3**

  - [ ]* 10.5 Write property test for date range validation
    - **Property 14: Date Range Validation**
    - **Validates: Requirements 6.4**

  - [ ]* 10.6 Write property test for usage summary computation
    - **Property 15: Usage Summary Computation**
    - **Validates: Requirements 6.5**

  - [ ]* 10.7 Write property test for chart reference line
    - **Property 16: Chart Reference Line**
    - **Validates: Requirements 6.7**

  - [ ]* 10.8 Write property test for chart ARIA labels
    - **Property 27: ARIA Labels for Charts**
    - **Validates: Requirements 13.5**

- [ ] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 12. Implement billing, invoices, and payment management
  - [ ] 12.1 Implement InvoiceService
    - Create `Services/InvoiceService.cs` implementing `IInvoiceService`
    - Implement `GetInvoicesAsync` → GET /api/tenants/me/invoices?page={}&pageSize={}
    - Implement `GetInvoiceDetailAsync` → GET /api/tenants/me/invoices/{invoiceId}
    - Implement `DownloadInvoicePdfAsync` → GET /api/tenants/me/invoices/{invoiceId}/pdf (returns Stream)
    - Implement `GetBillingSummaryAsync` → GET /api/tenants/me/billing/summary
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 12.2 Implement Billing and InvoiceDetail pages
    - Create `Pages/Billing.razor` displaying: billing period summary, current charges, next invoice date
    - Implement paginated invoice list with `Components/InvoiceListItem.razor` (period, base/overage/total amounts, status)
    - Implement Download button triggering PDF download via browser
    - Implement Manage Payment Method button redirecting to Stripe Portal
    - Display warning banner for any invoice with "failed" status linking to payment management
    - Create `Pages/InvoiceDetail.razor` with daily overage breakdown table (date, requests, quota, overage, charge)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [ ]* 12.3 Write property test for billing summary rendering
    - **Property 17: Billing Summary Rendering**
    - **Validates: Requirements 7.1**

  - [ ]* 12.4 Write property test for invoice list rendering
    - **Property 18: Invoice List Rendering**
    - **Validates: Requirements 7.2**

  - [ ]* 12.5 Write property test for invoice detail breakdown
    - **Property 19: Invoice Detail Breakdown**
    - **Validates: Requirements 7.3**

  - [ ]* 12.6 Write property test for failed invoice warning
    - **Property 20: Failed Invoice Warning**
    - **Validates: Requirements 7.7**

- [ ] 13. Implement documentation, settings, and support pages
  - [ ] 13.1 Implement Documentation page
    - Create `Pages/Documentation.razor` with sidebar navigation (Quick Start, Authentication, Endpoints Reference, Code Examples, SDKs, Error Reference)
    - Create `Components/CodeBlock.razor` with syntax highlighting and copy button
    - Display Quick_Start_Guide: obtain key, set Authorization header, make request, interpret response
    - Display code examples in C#, JavaScript, and Python with copy buttons
    - Display SDK download links with version info and package manager commands
    - Display error code reference table (code, HTTP status, description)
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

  - [ ] 13.2 Implement Settings page
    - Create `Pages/Settings.razor` with profile form (tenant name, contact email, company name)
    - Implement save profile via TenantService.UpdateProfileAsync with success confirmation
    - Display field-level validation errors on failure
    - Implement notification preference toggles: usage alerts, billing notifications, key expiration reminders
    - Implement save preferences via TenantService.UpdateNotificationPreferencesAsync
    - Display link to OIDC provider account management for password changes
    - Display current plan name with link to Plans page
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

  - [ ] 13.3 Implement Support page
    - Create `Pages/Support.razor`
    - Create `Components/FaqAccordion.razor` with expandable Q&A entries (auth issues, quota exceeded, key rotation, billing)
    - Create `Components/SupportForm.razor` with fields: subject, category (technical/billing/account), priority (low/medium/high), message
    - Implement SupportService.SubmitTicketAsync → POST /api/support/tickets
    - Display confirmation with ticket reference number on success
    - Preserve form content on submission failure
    - Display platform status page link and estimated response times by priority
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

  - [ ] 13.4 Implement SupportService
    - Create `Services/SupportService.cs` implementing `ISupportService`
    - Implement `SubmitTicketAsync` → POST /api/support/tickets with SupportTicketRequest
    - Return `ApiResult<SupportTicketResult>` with ticket reference
    - _Requirements: 10.3, 10.4_

- [ ] 14. Implement accessibility and performance optimizations
  - [ ] 14.1 Implement lazy loading and performance optimizations
    - Configure lazy loading for all page assemblies except Dashboard (use `@attribute [LazyAssembly]` or router-based lazy loading)
    - Verify initial shell loads within performance budget on simulated 4G
    - Ensure all MudBlazor components use appropriate ARIA attributes
    - Add ARIA labels to chart containers describing chart type and data context
    - Ensure keyboard navigation for all interactive elements including chart controls
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

  - [ ] 14.2 Implement breadcrumbs and final navigation polish
    - Add contextual breadcrumbs on sub-pages (InvoiceDetail, Documentation sections)
    - Verify browser URL updates via client-side routing without full page reload
    - Verify notification badge appears on Dashboard nav item when quota >80%
    - _Requirements: 11.5, 11.6, 11.7_

- [ ] 15. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (27 properties total)
- Unit tests (bUnit + xUnit) validate specific examples and edge cases
- FsCheck is used for property-based testing with minimum 100 iterations per property
- Chart.js interop requires `wwwroot/js/chartInterop.js` to be loaded in `index.html`
- Stripe Checkout/Portal uses redirect model (no PCI scope in our app)
- All services use `CancellationToken` on async methods per coding standards
- All HTTP communication uses `ApiResult<T>` pattern for consistent error handling

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["2.1", "2.2", "4.1", "4.3"] },
    { "id": 3, "tasks": ["2.4", "2.3", "2.6", "2.7", "4.2"] },
    { "id": 4, "tasks": ["2.5", "5.1", "5.2", "5.3"] },
    { "id": 5, "tasks": ["5.4", "5.5", "5.6", "5.7", "5.8", "6.1", "9.1", "9.4", "10.2", "12.1", "13.4"] },
    { "id": 6, "tasks": ["6.2", "7.1", "9.2", "9.5", "10.1"] },
    { "id": 7, "tasks": ["6.3", "6.4", "6.5", "7.2", "7.3", "7.4", "9.3", "9.6", "10.3"] },
    { "id": 8, "tasks": ["10.4", "10.5", "10.6", "10.7", "10.8", "12.2", "13.1", "13.2", "13.3"] },
    { "id": 9, "tasks": ["12.3", "12.4", "12.5", "12.6", "14.1", "14.2"] }
  ]
}
```
