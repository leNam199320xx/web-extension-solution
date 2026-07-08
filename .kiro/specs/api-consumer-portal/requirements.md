# Requirements Document

## Introduction

The API Consumer Portal is a self-service Blazor WebAssembly Standalone application for API Consumers (paying customers) of the Plugin Runtime platform. It provides a streamlined frontend where small websites and systems can register, manage their API keys, monitor usage, select plans, subscribe to plugin packages, handle billing, and access documentation. The portal communicates exclusively with PluginRuntime.Api (the modular monolith backend) using typed HttpClient services and authenticates users via OIDC/JWT Bearer tokens (API_Consumer role).

Architecture:
```
API Consumer → Consumer Portal (Blazor WASM) → PluginRuntime.Api (Modular Monolith) → PostgreSQL / Redis / Stripe
                                                         │
                                                         ├── /api/tenants/* (registration, keys)
                                                         ├── /api/subscriptions/* (plan changes, package subscriptions)
                                                         └── /api/billing/* (invoices, usage)
```

Key design goals:
- Registration to first API call in under 5 minutes
- Simple, clean interface targeting small website developers
- Mobile-responsive layout using MudBlazor UI framework
- Consistent authentication model (same OIDC provider as Marketplace Portal, different role)

## Glossary

- **Consumer_Portal**: The Blazor WebAssembly Standalone application that serves as the self-service frontend for API Consumers
- **API_Consumer**: A paying customer (small website or system) that registers, selects a plan, manages API keys, and consumes API access through the Gateway
- **Billing_API**: The PluginRuntime.Api modular monolith backend that the Consumer_Portal communicates with for all data operations (tenants, subscriptions, billing endpoints)
- **OIDC_Provider**: The external OpenID Connect identity provider used for authentication (shared with Marketplace Portal)
- **Tenant**: The organizational entity representing the API Consumer's account in PluginRuntime.Api with profile, plan, keys, and billing data
- **Plan**: A subscription tier (Free, Pro, Enterprise) defining rate limits, daily quotas, pricing, and feature access — shared across all user types
- **Plugin_Package**: A curated group of plugins that Tenants can subscribe to for an additional monthly fee via the Subscriptions module
- **API_Key**: A cryptographic token issued to a Tenant for authentication against the Public API Gateway
- **Usage_Aggregate**: A daily summary of the Tenant's API usage including total requests, success/failure counts, and average response time
- **Invoice**: A billing document recording charges for a billing period including base plan cost and overage fees
- **Stripe_Portal**: The Stripe-hosted Customer Portal page where API Consumers manage payment methods and billing details
- **Dashboard**: The main overview page displaying usage statistics, current plan, active keys, quota usage, and recent activity
- **Onboarding_Wizard**: The multi-step first-time setup flow that guides a new API Consumer from registration to first API call
- **API_Client**: The typed HttpClient service that communicates with the Billing_API backend
- **Quick_Start_Guide**: The documentation section providing step-by-step instructions to make a first API call

## Requirements

### Requirement 1: Authentication and Session Management

**User Story:** As an API_Consumer, I want to authenticate via OIDC so that I can securely access my tenant account and portal features.

#### Acceptance Criteria

1. WHEN a user navigates to a protected page without a valid session, THE Consumer_Portal SHALL redirect the user to the OIDC_Provider login page.
2. WHEN the OIDC_Provider returns a valid token with the API_Consumer role claim, THE Consumer_Portal SHALL store the JWT token and establish an authenticated session.
3. IF the OIDC_Provider returns a token without the API_Consumer role claim, THEN THE Consumer_Portal SHALL display an access denied message indicating the portal is for API consumers only.
4. WHILE a user is authenticated, THE Consumer_Portal SHALL include the Bearer token in all requests to the Billing_API.
5. WHEN a JWT token is within 5 minutes of expiration, THE Consumer_Portal SHALL attempt a silent token refresh before prompting re-authentication.
6. WHEN a user clicks the logout button, THE Consumer_Portal SHALL clear the local session and redirect to the OIDC_Provider logout endpoint.
7. IF the OIDC_Provider is unreachable, THEN THE Consumer_Portal SHALL display an error message indicating authentication is temporarily unavailable with a retry option.

### Requirement 2: Registration and Onboarding

**User Story:** As a new API_Consumer, I want to register and complete onboarding quickly so that I can make my first API call within 5 minutes.

#### Acceptance Criteria

1. WHEN a new user completes OIDC authentication for the first time and has no associated Tenant, THE Consumer_Portal SHALL display the Onboarding_Wizard.
2. THE Onboarding_Wizard SHALL collect tenant name (1–200 characters), contact email (pre-filled from OIDC token), and optional company name in the first step.
3. WHEN the user submits the registration form, THE Consumer_Portal SHALL send a registration request to the Billing_API and display the resulting tenant_id upon success.
4. WHEN registration succeeds, THE Onboarding_Wizard SHALL advance to the plan selection step displaying Free, Pro, and Enterprise plans with feature comparison.
5. WHEN the user selects the Free plan, THE Onboarding_Wizard SHALL advance to the API key generation step without requiring payment information.
6. WHEN the user selects a paid plan (Pro or Enterprise), THE Onboarding_Wizard SHALL redirect to Stripe Checkout for payment setup before advancing to the API key generation step.
7. WHEN the user reaches the API key generation step, THE Consumer_Portal SHALL automatically generate a first API key and display the plaintext key with a copy-to-clipboard button and a warning that the key will not be shown again.
8. WHEN the user completes the final step, THE Onboarding_Wizard SHALL display the Quick_Start_Guide with a code example showing how to make a first API call using the generated key.
9. IF the Billing_API returns error code "TM-REG-001" (duplicate email), THEN THE Consumer_Portal SHALL display a message indicating an account already exists for that email.
10. IF the Billing_API returns a validation error, THEN THE Consumer_Portal SHALL display field-level error messages and allow the user to correct the input.

### Requirement 3: Dashboard

**User Story:** As an API_Consumer, I want to see an overview of my account status so that I can quickly understand my usage, plan, and key health at a glance.

#### Acceptance Criteria

1. WHEN an authenticated user navigates to the dashboard, THE Consumer_Portal SHALL display the current Plan name, rate limit, and daily quota.
2. THE Consumer_Portal SHALL display the current billing period's total API requests, successful requests, and failed requests retrieved from Usage_Aggregates.
3. THE Consumer_Portal SHALL display a quota usage percentage bar showing daily requests consumed relative to the Plan's daily quota.
4. THE Consumer_Portal SHALL display the count of active API keys and the count of keys expiring within 7 days.
5. THE Consumer_Portal SHALL display a list of the 5 most recent API activity entries showing date, total requests, and success rate.
6. WHEN the quota usage percentage exceeds 80%, THE Consumer_Portal SHALL display a warning indicator suggesting the user consider upgrading their plan.
7. IF the Billing_API returns an error when fetching dashboard data, THEN THE Consumer_Portal SHALL display a partial dashboard with available data and an error indicator for failed sections.

### Requirement 4: Plan Selection and Upgrade

**User Story:** As an API_Consumer, I want to view and change my subscription plan so that I can scale my API usage up or down based on my needs.

#### Acceptance Criteria

1. WHEN a user navigates to the plan selection page, THE Consumer_Portal SHALL display all available plans (Free, Pro, Enterprise) with rate limits, daily quotas, monthly price, overage rates, and included features in a comparison layout.
2. THE Consumer_Portal SHALL visually indicate the user's current plan in the comparison layout.
3. WHEN a user selects a higher-tier plan (upgrade), THE Consumer_Portal SHALL display a confirmation dialog showing the prorated cost for the remainder of the current billing period and the new monthly amount.
4. WHEN a user confirms an upgrade, THE Consumer_Portal SHALL redirect to Stripe Checkout for payment processing and display a success message upon return.
5. WHEN a user selects a lower-tier plan (downgrade), THE Consumer_Portal SHALL display a confirmation dialog indicating the new plan takes effect at the start of the next billing period.
6. WHEN a user confirms a downgrade, THE Consumer_Portal SHALL send the plan change request to the Billing_API and display confirmation with the effective date.
7. IF the user is on the Free plan and selects a paid plan, THE Consumer_Portal SHALL redirect to Stripe Checkout for initial payment method setup.
8. IF the Billing_API returns error code "TM-CHANGE-001" (same plan selected), THEN THE Consumer_Portal SHALL display a message indicating the user is already on the selected plan.

### Requirement 5: API Key Management

**User Story:** As an API_Consumer, I want to manage my API keys so that I can control access credentials and respond to security concerns.

#### Acceptance Criteria

1. WHEN a user navigates to the API key management page, THE Consumer_Portal SHALL display a list of all API keys belonging to the Tenant showing key prefix, key suffix (masked middle), status, creation date, expiration date, and last used date.
2. WHEN a user clicks Generate New Key, THE Consumer_Portal SHALL prompt for an optional key name and optional expiration date (1–365 days in the future).
3. WHEN a new key is generated, THE Consumer_Portal SHALL display the plaintext key exactly once with a copy-to-clipboard button and a warning that the key cannot be retrieved again.
4. WHEN a user clicks Rotate on an active key, THE Consumer_Portal SHALL display a confirmation dialog warning that the old key will be immediately revoked, then generate a new key and display the plaintext value with a copy-to-clipboard button.
5. WHEN a user clicks Revoke on an active key, THE Consumer_Portal SHALL display a confirmation dialog, then revoke the key via the Billing_API and update the key's status to "revoked" in the list.
6. IF the Billing_API returns error code "TM-KEY-001" (maximum keys reached), THEN THE Consumer_Portal SHALL display a message indicating the key limit for the current plan has been reached with a suggestion to upgrade.
7. IF the Billing_API returns error code "TM-KEY-002" (key already revoked), THEN THE Consumer_Portal SHALL refresh the key list and display the current state.
8. THE Consumer_Portal SHALL display a badge on keys expiring within 7 days indicating upcoming expiration.

### Requirement 6: Usage Analytics

**User Story:** As an API_Consumer, I want to view detailed usage analytics so that I can understand my API consumption patterns and optimize my integration.

#### Acceptance Criteria

1. WHEN a user navigates to the usage analytics page, THE Consumer_Portal SHALL display a chart showing daily total API requests for the current billing period (default 30 days).
2. THE Consumer_Portal SHALL display a chart showing daily success rate (percentage of 2xx responses) for the selected period.
3. THE Consumer_Portal SHALL display the average response time (in milliseconds) per day for the selected period.
4. WHEN a user selects a custom date range (maximum 90 days), THE Consumer_Portal SHALL fetch and display Usage_Aggregates for the specified range.
5. THE Consumer_Portal SHALL display summary statistics for the selected period: total requests, average daily requests, total successful requests, total failed requests, and average response time.
6. WHEN a user hovers over a data point on a chart, THE Consumer_Portal SHALL display a tooltip showing the exact values for that day.
7. THE Consumer_Portal SHALL display a horizontal reference line on the daily requests chart indicating the Plan's daily quota limit.

### Requirement 7: Billing and Invoices

**User Story:** As an API_Consumer, I want to view my billing history and manage payment methods so that I can track my costs and keep payment information current.

#### Acceptance Criteria

1. WHEN a user navigates to the billing page, THE Consumer_Portal SHALL display the current billing period start date, end date, current accumulated charges (base plan cost plus estimated overage to date), and next invoice date.
2. THE Consumer_Portal SHALL display a paginated list of past invoices showing billing period, base amount, overage amount, total amount, and payment status (pending, paid, failed).
3. WHEN a user clicks on an invoice entry, THE Consumer_Portal SHALL display invoice details including a breakdown of daily overage calculations.
4. WHEN a user clicks Download on an invoice, THE Consumer_Portal SHALL retrieve the invoice PDF from the Billing_API and initiate a browser download.
5. WHEN a user clicks Manage Payment Method, THE Consumer_Portal SHALL redirect to the Stripe_Portal where the user can update payment cards and billing details.
6. WHEN a user returns from the Stripe_Portal, THE Consumer_Portal SHALL display the billing page with updated payment information.
7. IF an invoice has status "failed", THE Consumer_Portal SHALL display a warning banner on the billing page and dashboard indicating payment failure with a link to manage the payment method.

### Requirement 8: Documentation and Getting Started

**User Story:** As an API_Consumer, I want to access API documentation and code examples so that I can integrate quickly without leaving the portal.

#### Acceptance Criteria

1. WHEN a user navigates to the documentation page, THE Consumer_Portal SHALL display documentation sections organized with a navigation sidebar including: Quick Start, Authentication, Endpoints Reference, Code Examples, SDKs, and Error Reference.
2. THE Consumer_Portal SHALL display the Quick_Start_Guide with step-by-step instructions covering: obtaining an API key, setting the Authorization header, making a first request, and interpreting the response.
3. THE Consumer_Portal SHALL display code examples for making API calls in at least three languages: C#, JavaScript, and Python.
4. WHEN a user clicks a copy button on a code example, THE Consumer_Portal SHALL copy the code content to the clipboard and display a brief confirmation.
5. THE Consumer_Portal SHALL display a link to download available SDKs with version information and package manager installation commands.
6. THE Consumer_Portal SHALL render documentation content with syntax-highlighted code blocks.
7. THE Consumer_Portal SHALL display the API error code reference listing all error codes, HTTP status codes, and descriptions.

### Requirement 9: Account Settings

**User Story:** As an API_Consumer, I want to manage my account profile and preferences so that I can keep my information current and control notifications.

#### Acceptance Criteria

1. WHEN a user navigates to the account settings page, THE Consumer_Portal SHALL display profile information including tenant name, contact email, and company name in editable form fields.
2. WHEN a user modifies profile fields and clicks Save, THE Consumer_Portal SHALL submit the updated profile to the Billing_API and display a success confirmation.
3. IF a profile update fails validation, THEN THE Consumer_Portal SHALL display field-level error messages and preserve the form state.
4. THE Consumer_Portal SHALL display notification preference toggles for: usage alerts (quota threshold warnings), billing notifications (invoice generated, payment failed), and key expiration reminders.
5. WHEN a user changes notification preferences and clicks Save, THE Consumer_Portal SHALL persist the preferences via the Billing_API and display a success confirmation.
6. THE Consumer_Portal SHALL display a link to change password or authentication settings that redirects to the OIDC_Provider account management page.
7. THE Consumer_Portal SHALL display the current plan name and a link to the plan selection page for quick access.

### Requirement 10: Support and Help

**User Story:** As an API_Consumer, I want to access support resources so that I can resolve issues and get help when needed.

#### Acceptance Criteria

1. WHEN a user navigates to the support page, THE Consumer_Portal SHALL display a FAQ section with expandable question-and-answer entries covering common topics (authentication issues, quota exceeded, key rotation, billing questions).
2. THE Consumer_Portal SHALL display a contact support form with fields for subject, category (technical, billing, account), priority (low, medium, high), and message body.
3. WHEN a user submits the support form, THE Consumer_Portal SHALL send the support request to the Billing_API and display a confirmation with a ticket reference number.
4. IF the support form submission fails, THEN THE Consumer_Portal SHALL display an error message and preserve the form content.
5. THE Consumer_Portal SHALL display a link to the platform status page in a prominent location.
6. THE Consumer_Portal SHALL display estimated response times by priority level (high: 4 hours, medium: 24 hours, low: 72 hours).

### Requirement 11: Application Shell and Navigation

**User Story:** As an API_Consumer, I want consistent and intuitive navigation so that I can move between portal features efficiently.

#### Acceptance Criteria

1. THE Consumer_Portal SHALL display a persistent navigation sidebar with links to Dashboard, API Keys, Usage Analytics, Plans, Billing, Documentation, Settings, and Support.
2. WHILE a user is authenticated, THE Consumer_Portal SHALL display the tenant name and current plan badge in the navigation header.
3. WHILE a user is not authenticated, THE Consumer_Portal SHALL display only the login page and public documentation.
4. THE Consumer_Portal SHALL display a responsive layout that collapses the sidebar into a hamburger menu on viewport widths below 960 pixels.
5. WHEN navigation occurs, THE Consumer_Portal SHALL update the browser URL without full page reload.
6. THE Consumer_Portal SHALL display contextual breadcrumbs on pages deeper than the top-level navigation.
7. WHEN a user's plan quota exceeds 80% usage, THE Consumer_Portal SHALL display a notification badge on the Dashboard navigation item.

### Requirement 12: API Communication and Error Handling

**User Story:** As an API_Consumer, I want reliable communication with the backend so that I receive clear feedback when operations succeed or fail.

#### Acceptance Criteria

1. THE API_Client SHALL use typed HttpClient instances configured with the Billing_API base URL and Bearer authentication headers.
2. WHEN the Billing_API returns an error response, THE Consumer_Portal SHALL display the error code and human-readable message from the standard error format in a dismissible notification.
3. IF the Billing_API is unreachable, THEN THE Consumer_Portal SHALL display a connectivity error banner with a retry option.
4. WHILE a request to the Billing_API is in progress, THE Consumer_Portal SHALL display a loading indicator appropriate to the context (skeleton for page loads, spinner for actions).
5. WHEN the Billing_API returns a 429 Too Many Requests response, THE Consumer_Portal SHALL display a rate limit message and disable the action until the Retry-After period elapses.
6. THE API_Client SHALL include a correlation ID header with each request for traceability.
7. THE API_Client SHALL implement automatic retry with exponential backoff (maximum 3 attempts) for transient failures (HTTP 502, 503, 504).

### Requirement 13: Accessibility and Performance

**User Story:** As an API_Consumer, I want an accessible and performant portal so that I can manage my API access efficiently regardless of ability or connection speed.

#### Acceptance Criteria

1. THE Consumer_Portal SHALL comply with WCAG 2.1 Level AA accessibility standards for all interactive components.
2. THE Consumer_Portal SHALL load the initial application shell within 3 seconds on a 4G connection.
3. THE Consumer_Portal SHALL implement lazy loading for page components to minimize the initial WebAssembly download size.
4. THE Consumer_Portal SHALL support keyboard navigation for all interactive elements including chart interactions.
5. THE Consumer_Portal SHALL provide appropriate ARIA labels for dynamic content regions and chart data.
6. THE Consumer_Portal SHALL implement copy-to-clipboard functionality with visual and screen-reader-accessible confirmation feedback.
7. THE Consumer_Portal SHALL cache API responses for dashboard data with a 60-second staleness threshold to reduce redundant network requests.
