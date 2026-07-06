# BR-400 Capability Business Rules

## Overview

This document defines the business rules governing the Capability Model of the Metadata-Driven Secure Plugin Runtime.

Capabilities represent the smallest unit of authorization granted to a plugin. Every privileged operation performed by a plugin shall be explicitly authorized through one or more approved capabilities.

The Capability Model follows the principles of Zero Trust, Least Privilege and Explicit Authorization.

---

# Scope

This document applies to:

- Capability Declaration
- Capability Assignment
- Capability Resolution
- Capability Dependencies
- Capability Lifecycle
- Capability Revocation
- Capability Auditing
- Capability Governance

---

## BR-401 Explicit Capability Declaration

Every privileged operation shall require one or more explicitly declared capabilities.

Capabilities shall never be inferred automatically.

### Related Functional Requirements

- FR-301
- FR-304
- FR-305

---

## BR-402 Capability Registry Authority

The Runtime Capability Registry shall be the single authoritative source for all supported capabilities.

Capabilities not registered in the registry shall not be granted.

### Related Functional Requirements

- FR-302
- FR-303
- FR-318

---

## BR-403 Least Privilege Assignment

Plugins shall receive only the minimum capabilities necessary to perform their declared functionality.

Additional capabilities shall require explicit administrative approval.

### Related Functional Requirements

- FR-306
- FR-312
- FR-314

---

## BR-404 Capability Dependency

Capability dependencies shall be explicitly defined and validated before activation.

Circular capability dependencies are prohibited.

### Related Functional Requirements

- FR-307
- FR-308

---

## BR-405 Authorization Before Execution

Every protected operation shall undergo capability evaluation before execution.

Execution shall not begin until authorization succeeds.

### Related Functional Requirements

- FR-304
- FR-305
- FR-309
- FR-602

---

## BR-406 Capability Revocation

Capability revocation shall take effect immediately.

Previously granted capabilities shall not remain active after revocation.

### Related Functional Requirements

- FR-310
- FR-311
- FR-516

---

## BR-407 Tenant Capability Isolation

Capability assignments shall remain isolated between tenants.

Capabilities assigned within one tenant shall not be visible or reusable by another tenant.

### Related Functional Requirements

- FR-312
- FR-705
- FR-512

---

## BR-408 Capability Auditability

Every capability grant, denial, modification and revocation shall generate an immutable audit record.

Capability evaluation decisions shall be traceable throughout the execution lifecycle.

### Related Functional Requirements

- FR-315
- FR-319
- FR-320
- FR-417
- FR-519

---

# Summary

| Rule Group | Rules |
|------------|------:|
| Capability | 8 |

---

# Related Documents

- FR-300 Capability
- FR-500 Runtime
- FR-600 Execution
- UC-300 Capability
- NFR-001 Non-Functional Requirements