# Requirements Document

## Introduction

The Plugin Marketplace Portal is a public-facing Blazor WebAssembly Standalone application that serves as the consumer-facing frontend for plugin developers in the Plugin Runtime ecosystem. It allows developers to browse, search, upload, and manage extensions. The portal communicates with the existing PluginRuntime.Api backend via typed HTTP clients and authenticates users through OIDC/JWT Bearer tokens. The UI is built with MudBlazor for consistency with the existing Admin Portal.

## Glossary

- **Marketplace_Portal**: The Blazor WebAssembly Standalone application that serves as the developer-facing frontend for browsing, uploading, and managing extensions.
- **Plugin_Developer**: An authenticated user who creates, uploads, and manages extensions in the ecosystem.
- **Extension**: A plugin package (.plugin.zip) that has been uploaded, verified, and registered in the Extension Registry.
- **Extension_Registry**: The central database tracking all published extensions with metadata such as visibility, category, and version information.
- **Plugin_API**: The existing PluginRuntime.Api backend service exposing RESTful endpoints for plugin management, subscriptions, and approvals.
- **Manifest**: The JSON metadata file inside a plugin package that declares permissions, capabilities, execution policy, and identity information.
- **Subscription**: A request from one extension to invoke another extension that uses the Subscription visibility model.
- **Upload_Wizard**: The multi-step UI workflow that guides a developer through uploading a plugin package.
- **Permission_Breakdown**: A visual representation of an extension's declared permissions categorized by risk level.
- **OIDC_Provider**: The external OpenID Connect identity provider used for authentication.
- **Search_Engine**: The client-side component responsible for filtering, sorting, and paginating extension listings.
- **Publisher_Profile**: A public page displaying information about a plugin developer and their published extensions.
- **API_Client**: The typed HttpClient service that communicates with the Plugin_API backend.

## Requirements

### Requirement 1: Authentication and Authorization

**User Story:** As a Plugin_Developer, I want to authenticate via OIDC so that I can securely access my extensions and marketplace features.

#### Acceptance Criteria

1. WHEN a user navigates to a protected page without a valid session, THE Marketplace_Portal SHALL redirect the user to the OIDC_Provider login page.
2. WHEN the OIDC_Provider returns a valid token, THE Marketplace_Portal SHALL store the JWT token and establish an authenticated session.
3. WHILE a user is authenticated, THE Marketplace_Portal SHALL include the Bearer token in all requests to the Plugin_API.
4. WHEN a JWT token expires, THE Marketplace_Portal SHALL attempt a silent token refresh before prompting re-authentication.
5. WHEN a user clicks the logout button, THE Marketplace_Portal SHALL clear the local session and redirect to the OIDC_Provider logout endpoint.
6. IF the OIDC_Provider is unreachable, THEN THE Marketplace_Portal SHALL display an error message indicating authentication is unavailable.

### Requirement 2: Landing Page

**User Story:** As a Plugin_Developer, I want to see a landing page with featured extensions and ecosystem statistics so that I can quickly discover popular content.

#### Acceptance Criteria

1. WHEN a user navigates to the root URL, THE Marketplace_Portal SHALL display the landing page with featured extensions, category navigation, and ecosystem statistics.
2. THE Marketplace_Portal SHALL display ecosystem statistics including total extensions count, total publishers count, and total subscriptions count.
3. THE Marketplace_Portal SHALL display a curated list of featured extensions with name, description, author, and category.
4. WHEN a user clicks on a featured extension card, THE Marketplace_Portal SHALL navigate to the extension detail page.
5. THE Marketplace_Portal SHALL display category cards that link to the browse page filtered by the selected category.

### Requirement 3: Browse and Search Extensions

**User Story:** As a Plugin_Developer, I want to browse and search all public extensions so that I can find extensions relevant to my needs.

#### Acceptance Criteria

1. WHEN a user navigates to the browse page, THE Marketplace_Portal SHALL display a paginated list of public extensions retrieved from the Plugin_API.
2. WHEN a user enters a search term, THE Search_Engine SHALL filter extensions by name, description, and extension_id and display matching results within 300ms of input debounce.
3. WHEN a user selects a category filter, THE Search_Engine SHALL display only extensions belonging to the selected category.
4. WHEN a user selects a risk level filter, THE Search_Engine SHALL display only extensions matching the selected overall risk level.
5. WHEN a user selects a capability type filter, THE Search_Engine SHALL display only extensions declaring the selected capability.
6. THE Marketplace_Portal SHALL display 20 extensions per page and provide pagination controls to navigate between pages.
7. WHEN no extensions match the applied filters, THE Marketplace_Portal SHALL display a message indicating no results were found.
8. THE Marketplace_Portal SHALL display each extension card with the extension name, author, category, version, risk level indicator, and short description.

### Requirement 4: Extension Detail Page

**User Story:** As a Plugin_Developer, I want to view detailed information about an extension so that I can evaluate whether to subscribe to it.

#### Acceptance Criteria

1. WHEN a user navigates to an extension detail page, THE Marketplace_Portal SHALL display the extension name, description, author, category, visibility, and latest version.
2. THE Marketplace_Portal SHALL display the extension README content rendered as formatted text.
3. THE Marketplace_Portal SHALL display a version history list showing all published versions with their status and creation date.
4. THE Marketplace_Portal SHALL display the Permission_Breakdown showing all declared permissions grouped by risk level with justification text.
5. WHEN the extension visibility is Subscription and the user is authenticated, THE Marketplace_Portal SHALL display a Subscribe button.
6. WHEN a user clicks the Subscribe button, THE Marketplace_Portal SHALL send a subscription request to the Plugin_API and display a confirmation message with the pending status.
7. WHEN the extension visibility is Public, THE Marketplace_Portal SHALL display the extension as available for invocation without requiring a subscription.
8. IF the extension detail request fails, THEN THE Marketplace_Portal SHALL display an error message with the option to retry.

### Requirement 5: Publisher Profile Page

**User Story:** As a Plugin_Developer, I want to view a publisher's profile so that I can assess the credibility of extension authors.

#### Acceptance Criteria

1. WHEN a user navigates to a Publisher_Profile page, THE Marketplace_Portal SHALL display the publisher name, description, and join date.
2. THE Marketplace_Portal SHALL display a list of all public extensions published by the publisher with name, category, version, and risk level.
3. WHEN a user clicks an extension in the publisher's list, THE Marketplace_Portal SHALL navigate to the extension detail page.

### Requirement 6: My Plugins Dashboard

**User Story:** As a Plugin_Developer, I want to view the status of my uploaded extensions so that I can track their progress through the approval pipeline.

#### Acceptance Criteria

1. WHEN an authenticated user navigates to the My Plugins page, THE Marketplace_Portal SHALL display a list of all extensions owned by the current user.
2. THE Marketplace_Portal SHALL display each extension with its current status including Scanning, Pending Approval, Approved, and Rejected.
3. WHEN an extension has status Rejected, THE Marketplace_Portal SHALL display the rejection reason.
4. THE Marketplace_Portal SHALL display the latest version number, upload date, and total subscriber count for each extension.
5. WHEN a user clicks on an extension in the dashboard, THE Marketplace_Portal SHALL navigate to a detail view showing version history and approval status per version.

### Requirement 7: Upload Wizard

**User Story:** As a Plugin_Developer, I want a guided upload experience so that I can submit my extension package correctly and understand what permissions it declares.

#### Acceptance Criteria

1. WHEN an authenticated user navigates to the upload page, THE Upload_Wizard SHALL display a multi-step form with steps: File Selection, Manifest Preview, Permission Review, and Confirmation.
2. WHEN a user drags and drops a .plugin.zip file onto the drop zone, THE Upload_Wizard SHALL accept the file and display the filename and file size.
3. WHEN a user selects a file via the file picker, THE Upload_Wizard SHALL accept the file and display the filename and file size.
4. IF a user selects a file that is not a .zip archive, THEN THE Upload_Wizard SHALL display a validation error indicating only .plugin.zip files are accepted.
5. WHEN a valid .plugin.zip file is accepted, THE Upload_Wizard SHALL parse the manifest.json from the archive and display the extension_id, version, permissions, and capabilities in a preview panel.
6. THE Upload_Wizard SHALL display the Permission_Breakdown showing each declared permission with its mapped capability and risk level.
7. WHEN a user confirms the upload on the final step, THE Upload_Wizard SHALL submit the file to the Plugin_API upload endpoint and display the resulting status.
8. WHEN the Plugin_API returns a 202 Accepted response, THE Upload_Wizard SHALL display a success message with the plugin version ID and initial Scanning status.
9. IF the Plugin_API returns an error response, THEN THE Upload_Wizard SHALL display the error message and allow the user to retry the upload.
10. IF the manifest.json cannot be parsed from the archive, THEN THE Upload_Wizard SHALL display a validation error indicating the manifest is missing or malformed.

### Requirement 8: My Subscriptions

**User Story:** As a Plugin_Developer, I want to manage my subscription requests so that I can track outgoing requests and respond to incoming access requests for my extensions.

#### Acceptance Criteria

1. WHEN an authenticated user navigates to the My Subscriptions page, THE Marketplace_Portal SHALL display two tabs: Outgoing Requests and Incoming Requests.
2. THE Marketplace_Portal SHALL display outgoing subscription requests with target extension name, status, request date, and reason.
3. THE Marketplace_Portal SHALL display incoming subscription requests with requesting extension name, status, request date, reason, and expected usage information.
4. WHEN an incoming request has status Requested, THE Marketplace_Portal SHALL display Approve and Reject action buttons.
5. WHEN a user clicks Approve on an incoming request, THE Marketplace_Portal SHALL send the approval decision to the Plugin_API and update the request status to Approved.
6. WHEN a user clicks Reject on an incoming request, THE Marketplace_Portal SHALL prompt for a reason, send the rejection decision to the Plugin_API, and update the request status to Rejected.
7. WHEN a user approves a request, THE Marketplace_Portal SHALL allow the user to optionally specify conditions and an expiration date.

### Requirement 9: Settings Page

**User Story:** As a Plugin_Developer, I want to manage my profile and API keys so that I can configure my identity and CLI access.

#### Acceptance Criteria

1. WHEN an authenticated user navigates to the settings page, THE Marketplace_Portal SHALL display profile information including display name, email, and publisher description.
2. WHEN a user modifies profile fields and clicks Save, THE Marketplace_Portal SHALL submit the updated profile to the Plugin_API and display a success confirmation.
3. THE Marketplace_Portal SHALL display a list of active API keys with their name, creation date, and last used date.
4. WHEN a user clicks Generate New Key, THE Marketplace_Portal SHALL create a new API key via the Plugin_API and display the key value once.
5. WHEN a user clicks Revoke on an API key, THE Marketplace_Portal SHALL prompt for confirmation, then revoke the key via the Plugin_API and remove the key from the list.
6. IF a profile update fails, THEN THE Marketplace_Portal SHALL display the error message and preserve the form state.

### Requirement 10: SDK Documentation Page

**User Story:** As a Plugin_Developer, I want to access SDK documentation within the portal so that I can learn how to develop extensions without leaving the marketplace.

#### Acceptance Criteria

1. WHEN a user navigates to the documentation page, THE Marketplace_Portal SHALL display SDK documentation content organized by topic with a navigation sidebar.
2. THE Marketplace_Portal SHALL display documentation sections including Getting Started, Manifest Reference, Capability Reference, CLI Tools, and Extension Development Standard.
3. WHEN a user selects a topic in the sidebar, THE Marketplace_Portal SHALL scroll to the selected section.
4. THE Marketplace_Portal SHALL render documentation content with syntax-highlighted code examples.

### Requirement 11: Application Shell and Navigation

**User Story:** As a Plugin_Developer, I want consistent navigation so that I can move between marketplace features efficiently.

#### Acceptance Criteria

1. THE Marketplace_Portal SHALL display a persistent top navigation bar with links to Home, Browse, Documentation, and user account menu.
2. WHILE a user is authenticated, THE Marketplace_Portal SHALL display a user menu with links to My Plugins, My Subscriptions, Upload, Settings, and Logout.
3. WHILE a user is not authenticated, THE Marketplace_Portal SHALL display a Login button in the navigation bar.
4. THE Marketplace_Portal SHALL display a responsive layout that adapts to desktop and mobile screen widths.
5. WHEN navigation occurs, THE Marketplace_Portal SHALL update the browser URL without full page reload.

### Requirement 12: API Communication and Error Handling

**User Story:** As a Plugin_Developer, I want reliable communication with the backend so that I receive clear feedback when operations succeed or fail.

#### Acceptance Criteria

1. THE API_Client SHALL use typed HttpClient instances configured with the Plugin_API base URL and authentication headers.
2. WHEN the Plugin_API returns an error response, THE Marketplace_Portal SHALL display the error code, category, and human-readable message from the standard error format.
3. IF the Plugin_API is unreachable, THEN THE Marketplace_Portal SHALL display a connectivity error with a retry option.
4. WHILE a request to the Plugin_API is in progress, THE Marketplace_Portal SHALL display a loading indicator.
5. WHEN the Plugin_API returns a 429 Too Many Requests response, THE Marketplace_Portal SHALL display a rate limit message and disable the action until the Retry-After period elapses.
6. THE API_Client SHALL include a correlation ID header with each request for traceability.

### Requirement 13: Accessibility and Performance

**User Story:** As a Plugin_Developer, I want an accessible and performant portal so that I can use the marketplace efficiently regardless of ability or connection speed.

#### Acceptance Criteria

1. THE Marketplace_Portal SHALL comply with WCAG 2.1 Level AA accessibility standards for all interactive components.
2. THE Marketplace_Portal SHALL load the initial application shell within 3 seconds on a 4G connection.
3. THE Marketplace_Portal SHALL implement lazy loading for page components to minimize the initial WebAssembly download size.
4. THE Marketplace_Portal SHALL support keyboard navigation for all interactive elements.
5. THE Marketplace_Portal SHALL provide appropriate ARIA labels for dynamic content regions.
