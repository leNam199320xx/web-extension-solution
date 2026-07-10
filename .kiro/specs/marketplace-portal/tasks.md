# Implementation Plan: Marketplace Portal

## Overview

Build a Blazor WebAssembly Standalone application (`PluginRuntime.Marketplace`) that provides a developer-facing frontend for browsing, searching, uploading, and managing extensions. The implementation uses .NET 10, C#, MudBlazor, OIDC/JWT Bearer authentication, and communicates with the existing `PluginRuntime.Api` backend via typed HTTP clients. Property-based tests use FsCheck; unit tests use xUnit + bUnit.

## Tasks

- [x] 1. Set up project structure and core infrastructure
  - [x] 1.1 Create Blazor WebAssembly Standalone project and solution structure
    - Create `src/Marketplace/PluginRuntime.Marketplace` Blazor WASM Standalone project targeting .NET 10
    - Add MudBlazor, `Microsoft.AspNetCore.Components.WebAssembly.Authentication` NuGet packages
    - Create folder structure: `Layout/`, `Pages/`, `Components/`, `Services/`, `Models/`, `Auth/`, `Search/`
    - Configure `Program.cs` with DI registration stubs and MudBlazor services
    - Set up `wwwroot/index.html` with MudBlazor CSS/JS references
    - _Requirements: 11.1, 13.2, 13.3_

  - [x] 1.2 Define all client-side DTO models
    - Create `Models/` directory with all record DTOs: `ExtensionSummaryDto`, `ExtensionDetailDto`, `VersionHistoryDto`, `PermissionBreakdownDto`, `PermissionGroupDto`, `PermissionItemDto`, `SubscriptionDto`, `SubscriptionRequestDto`, `ExpectedUsageDto`, `SubscriptionDecisionDto`, `SubscriptionResponseDto`, `UploadResultDto`, `ManifestPreviewDto`, `PublisherProfileDto`, `UserProfileDto`, `UpdateProfileDto`, `ApiKeyDto`, `ApiKeyListDto`, `EcosystemStatsDto`, `PaginatedResult<T>`, `ExtensionQuery`, `SearchCriteria`, `SearchResult<T>`, `ApiErrorResponse`, `ApiError`
    - _Requirements: 3.8, 4.1, 4.4, 7.5, 12.2_

  - [x] 1.3 Define service interfaces
    - Create `Services/IExtensionService.cs`, `Services/ISubscriptionService.cs`, `Services/IUploadService.cs`, `Services/IProfileService.cs`, `Search/ISearchEngine.cs`
    - Define all method signatures with `CancellationToken` parameters as specified in the design
    - _Requirements: 12.1, 12.6_

  - [x] 1.4 Create test projects
    - Create `tests/PluginRuntime.Marketplace.Tests` xUnit + bUnit test project
    - Add FsCheck and FsCheck.Xunit NuGet packages
    - Add bUnit and Moq/NSubstitute packages
    - Configure test project references to the main Marketplace project
    - _Requirements: All (testing infrastructure)_

- [x] 2. Implement authentication and API communication infrastructure
  - [x] 2.1 Implement OIDC authentication configuration
    - Create `Auth/` configuration for OIDC provider integration using `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
    - Configure `Program.cs` with OIDC settings (authority, client ID, scopes) from `appsettings.json`
    - Implement `AuthenticationStateProvider` cascading parameter usage
    - Implement login redirect for protected pages and logout flow
    - _Requirements: 1.1, 1.2, 1.4, 1.5, 1.6_

  - [x] 2.2 Implement ApiAuthorizationMessageHandler
    - Create `Auth/ApiAuthorizationMessageHandler.cs` as a `DelegatingHandler`
    - Attach Bearer token from authentication state to all outgoing requests
    - Generate and attach `X-Correlation-Id` header (Guid) on each request
    - Handle 401 responses by triggering re-authentication
    - Handle 429 responses by extracting `Retry-After` header
    - Register handler in DI and attach to typed HttpClient instances
    - _Requirements: 1.3, 12.1, 12.5, 12.6_

  - [x]* 2.3 Write property test for API Client Request Headers (Property 1)
    - **Property 1: API Client Request Headers**
    - Generate random HTTP requests (various methods, paths) through the handler while authenticated
    - Verify every request includes both a valid Bearer token in Authorization header and a unique X-Correlation-Id header
    - **Validates: Requirements 1.3, 12.6**

  - [x] 2.4 Implement Result type and error handling pipeline
    - Create `Services/Result.cs` with `Result<T>` type supporting Success, ApiError, RateLimited, and NetworkError states
    - Implement `SafeCallAsync<T>` helper for consistent error handling across all service calls
    - Implement retry with exponential backoff (1s, 2s, 4s) for network errors and 5xx responses
    - Configure 15-second timeout for all API requests
    - _Requirements: 12.2, 12.3, 12.5_

  - [x]* 2.5 Write property test for Error Response Display Completeness (Property 8)
    - **Property 8: Error Response Display Completeness**
    - Generate random `ApiError` instances with varied codes, categories, and messages
    - Verify the error display component renders code, category, and human-readable message for every generated error
    - **Validates: Requirements 12.2**

- [x] 3. Implement client-side search engine
  - [x] 3.1 Implement SearchEngine service
    - Create `Search/SearchEngine.cs` implementing `ISearchEngine`
    - Implement text search filtering against Name, Description, and ExtensionId (case-insensitive contains)
    - Implement exact-match filtering for Category, RiskLevel, and CapabilityType
    - Implement pagination logic (page size 20, page calculation)
    - Return `SearchResult<T>` with correct Items, TotalCount, Page, and PageSize
    - _Requirements: 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x]* 3.2 Write property test for Search Filter Correctness (Property 2)
    - **Property 2: Search Filter Correctness**
    - Generate random extension lists and random filter criteria combinations
    - Verify all items in the filtered result match every active filter criterion
    - **Validates: Requirements 3.2, 3.3, 3.4, 3.5**

  - [x]* 3.3 Write property test for Pagination Invariant (Property 3)
    - **Property 3: Pagination Invariant**
    - Generate random-length extension lists (0–200 items)
    - Verify each page contains at most 20 items and total items across all pages equals the total matching items
    - **Validates: Requirements 3.6**

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement shared UI components
  - [x] 5.1 Implement ExtensionCard component
    - Create `Components/ExtensionCard.razor` displaying extension name, author, category, version, risk level badge, and short description
    - Use MudBlazor `MudCard`, `MudChip` for risk level color coding (Critical=Red, High=Orange, Medium=Yellow, Low=Green)
    - Support click navigation to extension detail page
    - _Requirements: 2.3, 3.8_

  - [x]* 5.2 Write property test for Extension Card Rendering Completeness (Property 4)
    - **Property 4: Extension Card Rendering Completeness**
    - Generate random `ExtensionSummaryDto` instances with varied data
    - Verify the rendered card contains extension name, author, category, version, risk level indicator, and short description
    - **Validates: Requirements 2.3, 3.8**

  - [x] 5.3 Implement PermissionBreakdown component
    - Create `Components/PermissionBreakdown.razor` that groups permissions by risk level
    - Display each permission with scope, mapped capability, risk level badge, and justification text
    - Use MudBlazor `MudExpansionPanel` for collapsible risk level groups
    - _Requirements: 4.4, 7.6_

  - [x]* 5.4 Write property test for Permission Breakdown Grouping (Property 5)
    - **Property 5: Permission Breakdown Grouping**
    - Generate random permission sets with varied risk levels
    - Verify correct grouping by risk level and that each entry displays scope, mapped capability, risk level, and justification
    - **Validates: Requirements 4.4, 7.6**

  - [x] 5.5 Implement SearchBar, PaginationControls, LoadingIndicator, and ErrorDisplay components
    - Create `Components/SearchBar.razor` with debounced text input (300ms) and filter dropdowns for category, risk level, capability type
    - Create `Components/PaginationControls.razor` with page navigation and total count display
    - Create `Components/LoadingIndicator.razor` with skeleton/spinner for API loading states
    - Create `Components/ErrorDisplay.razor` rendering structured API errors with retry button
    - _Requirements: 3.2, 3.6, 12.2, 12.3, 12.4_

  - [x] 5.6 Implement UploadDropZone component
    - Create `Components/UploadDropZone.razor` with drag-and-drop file upload area
    - Validate file extension is `.plugin.zip` on client side before accepting
    - Display filename and file size after acceptance
    - Display validation error for invalid file types
    - _Requirements: 7.2, 7.3, 7.4_

  - [x]* 5.7 Write property test for File Type Validation (Property 6)
    - **Property 6: File Type Validation**
    - Generate random filenames (with and without `.plugin.zip` suffix)
    - Verify that files not ending with `.plugin.zip` are always rejected with a validation error
    - **Validates: Requirements 7.4**

- [x] 6. Implement service layer
  - [x] 6.1 Implement ExtensionService
    - Create `Services/ExtensionService.cs` implementing `IExtensionService`
    - Implement all methods: `GetExtensionsAsync`, `GetExtensionDetailAsync`, `GetFeaturedExtensionsAsync`, `GetEcosystemStatsAsync`, `GetMyExtensionsAsync`, `GetVersionHistoryAsync`
    - Use typed `HttpClient` with proper API endpoint paths and query string construction
    - Wrap all calls with `SafeCallAsync` error handling
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 4.1, 4.3, 6.1_

  - [x] 6.2 Implement SubscriptionService
    - Create `Services/SubscriptionService.cs` implementing `ISubscriptionService`
    - Implement `RequestSubscriptionAsync`, `GetOutgoingRequestsAsync`, `GetIncomingRequestsAsync`, `DecideSubscriptionAsync`
    - _Requirements: 4.6, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

  - [x] 6.3 Implement UploadService
    - Create `Services/UploadService.cs` implementing `IUploadService`
    - Implement `ParseManifestAsync` for client-side zip/manifest extraction using System.IO.Compression
    - Implement `UploadPluginAsync` for multipart file upload to the API
    - _Requirements: 7.5, 7.7, 7.8, 7.9, 7.10_

  - [x]* 6.4 Write property test for Manifest Parsing Extraction (Property 7)
    - **Property 7: Manifest Parsing Extraction**
    - Generate random valid manifest data, package into zip archives
    - Verify that parsed output matches the original extension_id, version, permissions list, and capabilities list
    - **Validates: Requirements 7.5**

  - [x] 6.5 Implement ProfileService
    - Create `Services/ProfileService.cs` implementing `IProfileService`
    - Implement `GetPublisherProfileAsync`, `GetCurrentUserProfileAsync`, `UpdateProfileAsync`, `GenerateApiKeyAsync`, `RevokeApiKeyAsync`, `GetApiKeysAsync`
    - _Requirements: 5.1, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x]* 6.6 Write unit tests for service layer
    - Mock `HttpClient` via `MockHttpMessageHandler` for each service
    - Verify correct API endpoint calls, request serialization, and response mapping
    - Test error handling paths (4xx, 5xx, network errors)
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement pages - Landing, Browse, Extension Detail
  - [x] 8.1 Implement Landing Page (Home.razor)
    - Create `Pages/Home.razor` displaying featured extensions, category navigation cards, and ecosystem statistics
    - Call `IExtensionService.GetFeaturedExtensionsAsync` and `GetEcosystemStatsAsync` on initialization
    - Display stats: total extensions, total publishers, total subscriptions
    - Use `ExtensionCard` for featured items with click navigation to detail page
    - Display category cards linking to browse page with pre-applied category filter
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 8.2 Implement Browse Page (Browse.razor)
    - Create `Pages/Browse.razor` with `SearchBar`, paginated extension list, and `PaginationControls`
    - Integrate `ISearchEngine` for client-side filtering of the current page results
    - Call `IExtensionService.GetExtensionsAsync` with server-side pagination
    - Display "no results" message when filters return empty
    - Support pre-applied category filter from URL query parameter
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [x] 8.3 Implement Extension Detail Page (ExtensionDetail.razor)
    - Create `Pages/ExtensionDetail.razor` showing full extension metadata, README, version history, and permission breakdown
    - Call `IExtensionService.GetExtensionDetailAsync` on initialization
    - Render README content as formatted text
    - Display version history list with version number, status, and creation date
    - Display `PermissionBreakdown` component with grouped permissions
    - Show Subscribe button for Subscription-visibility extensions when user is authenticated
    - Handle subscription request submission via `ISubscriptionService.RequestSubscriptionAsync`
    - Display error with retry option if detail request fails
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_

  - [x]* 8.4 Write property test for Version History Display (Property 9)
    - **Property 9: Version History Display**
    - Generate random version lists with varied version numbers, statuses, and dates
    - Verify each version entry renders version number, status, and creation date
    - **Validates: Requirements 4.3**

- [x] 9. Implement pages - Publisher Profile, My Plugins, Upload Wizard
  - [x] 9.1 Implement Publisher Profile Page (PublisherProfile.razor)
    - Create `Pages/PublisherProfile.razor` displaying publisher name, description, join date
    - Display list of publisher's public extensions using `ExtensionCard` components
    - Support click navigation from extension cards to detail pages
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 9.2 Implement My Plugins Dashboard (MyPlugins.razor)
    - Create `Pages/MyPlugins.razor` (protected route requiring authentication)
    - Display list of current user's extensions with status, latest version, upload date, subscriber count
    - Show rejection reason for rejected extensions
    - Support click navigation to version history detail view
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 9.3 Implement Upload Wizard (Upload.razor)
    - Create `Pages/Upload.razor` (protected route) with multi-step form: File Selection → Manifest Preview → Permission Review → Confirmation
    - Step 1: Integrate `UploadDropZone` for file selection
    - Step 2: Call `IUploadService.ParseManifestAsync` and display manifest preview (extension_id, version, permissions, capabilities)
    - Step 3: Display `PermissionBreakdown` for permission review
    - Step 4: Submit via `IUploadService.UploadPluginAsync`, display success (plugin version ID + Scanning status) or error with retry
    - Handle manifest parse failure with "manifest missing or malformed" error
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10_

- [x] 10. Implement pages - Subscriptions, Settings, Documentation
  - [x] 10.1 Implement My Subscriptions Page (Subscriptions.razor)
    - Create `Pages/Subscriptions.razor` (protected route) with two tabs: Outgoing Requests and Incoming Requests
    - Display outgoing requests with target extension name, status, request date, reason
    - Display incoming requests with requesting extension name, status, date, reason, expected usage
    - Show Approve/Reject buttons for incoming requests with status Requested
    - Implement Approve flow with optional conditions and expiration date
    - Implement Reject flow with reason prompt
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_

  - [x] 10.2 Implement Settings Page (Settings.razor)
    - Create `Pages/Settings.razor` (protected route) displaying profile info (display name, email, publisher description)
    - Implement profile edit form with save functionality and success/error feedback
    - Display API keys list with name, creation date, last used date
    - Implement Generate New Key flow (display key value once)
    - Implement Revoke Key flow with confirmation dialog
    - Preserve form state on update failure
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

  - [x] 10.3 Implement Documentation Page (Documentation.razor)
    - Create `Pages/Documentation.razor` with topic-organized content and navigation sidebar
    - Include sections: Getting Started, Manifest Reference, Capability Reference, CLI Tools, Extension Development Standard
    - Implement sidebar topic selection with smooth scroll to section
    - Render documentation with syntax-highlighted code examples using MudBlazor or a Markdown renderer
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [x] 11. Implement application shell, navigation, and accessibility
  - [x] 11.1 Implement MainLayout and NavMenu
    - Create `Layout/MainLayout.razor` with persistent top navigation bar (Home, Browse, Documentation, user menu)
    - Implement authenticated user menu with links to My Plugins, My Subscriptions, Upload, Settings, Logout
    - Implement unauthenticated state showing Login button
    - Implement responsive layout adapting to desktop and mobile widths using MudBlazor breakpoints
    - Ensure SPA navigation (no full page reload) via Blazor router
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

  - [x] 11.2 Implement accessibility compliance
    - Add ARIA labels to all dynamic content regions (loading states, error messages, search results)
    - Ensure keyboard navigation for all interactive elements (buttons, links, form controls, modals)
    - Verify MudBlazor components have appropriate aria attributes
    - Configure lazy loading for page components to minimize initial WASM download
    - _Requirements: 13.1, 13.3, 13.4, 13.5_

  - [x]* 11.3 Write unit tests for page components
    - bUnit tests for Landing, Browse, Detail, Upload, Subscriptions, Settings pages
    - Verify correct rendering with mock service data
    - Verify loading indicators display during API calls
    - Verify error states render correctly
    - _Requirements: 12.4, 13.1_

- [x] 12. Integration wiring and final verification
  - [x] 12.1 Wire all services in Program.cs and configure DI
    - Register all service interfaces and implementations in DI container
    - Configure typed `HttpClient` instances with base URL from configuration
    - Attach `ApiAuthorizationMessageHandler` to all API-bound HttpClients
    - Configure OIDC authentication with settings from `appsettings.json` / `appsettings.Development.json`
    - Configure MudBlazor services and theming
    - Register `ISearchEngine` implementation
    - _Requirements: 12.1, 1.2, 11.1_

  - [x] 12.2 Configure routing and page authorization
    - Set up Blazor router with all page routes
    - Apply `[Authorize]` attribute to protected pages (MyPlugins, Upload, Subscriptions, Settings)
    - Implement `RedirectToLogin` component for unauthorized access to protected routes
    - Configure `CascadingAuthenticationState` in App.razor
    - _Requirements: 1.1, 11.5_

  - [x]* 12.3 Write integration tests for key workflows
    - Test OIDC login/logout flow with mock identity provider
    - Test browse → search → filter → paginate flow
    - Test upload wizard end-to-end with mock API responses
    - Test subscription request → approve/reject cycle
    - _Requirements: 1.1, 1.2, 3.1, 7.1, 8.5, 8.6_

- [x] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck
- Unit tests validate specific examples and edge cases using xUnit + bUnit
- All async methods must include `CancellationToken` per workspace coding standards
- Security > Performance > Convenience per workspace decision hierarchy

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.4"] },
    { "id": 2, "tasks": ["2.3", "2.5", "3.1"] },
    { "id": 3, "tasks": ["3.2", "3.3", "5.1", "5.3", "5.5", "5.6"] },
    { "id": 4, "tasks": ["5.2", "5.4", "5.7", "6.1", "6.2", "6.3", "6.5"] },
    { "id": 5, "tasks": ["6.4", "6.6"] },
    { "id": 6, "tasks": ["8.1", "8.2", "8.3", "9.1", "9.2", "9.3"] },
    { "id": 7, "tasks": ["8.4", "10.1", "10.2", "10.3"] },
    { "id": 8, "tasks": ["11.1", "11.2"] },
    { "id": 9, "tasks": ["11.3", "12.1", "12.2"] },
    { "id": 10, "tasks": ["12.3"] }
  ]
}
```
