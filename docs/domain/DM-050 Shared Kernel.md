# DM-050 Shared Kernel

---

# Overview

The Shared Kernel contains business concepts shared across all domains of the Runtime.

It defines common vocabulary, identifiers, value objects and lifecycle concepts used consistently throughout the platform.

The Shared Kernel reduces duplication and ensures semantic consistency between domains.

---

# Purpose

The Shared Kernel provides:

- Common identifiers
- Versioning concepts
- Lifecycle definitions
- Time concepts
- Correlation concepts
- Error concepts
- Result concepts
- Audit concepts

---

# Shared Concepts

| Concept | Description |
|----------|-------------|
| Identifier | Globally unique business identity |
| Version | Semantic version |
| Timestamp | Point in time |
| Correlation ID | End-to-end request identifier |
| Status | Business state |
| Metadata | Business metadata |
| Result | Business outcome |
| Error | Business failure |
| Audit Info | Immutable audit information |

---

# Shared Value Objects

## Identifier

Represents a globally unique business identity.

Used by:

- Plugin
- Manifest
- Execution
- Policy
- Audit Record

---

## Version

Represents semantic versioning.

Used by:

- Plugin
- Runtime
- Manifest
- SDK

---

## Timestamp

Represents an immutable point in time.

Used by every domain.

---

## Correlation ID

Associates all activities belonging to one request.

Used by:

- Runtime
- Execution
- Audit
- Observability

---

## Status

Represents lifecycle state.

Each domain may specialize its own status model while sharing common semantics.

---

## Metadata

Represents descriptive information associated with a business object.

Metadata is immutable after publication.

---

## Result

Represents the outcome of a business operation.

Possible outcomes include:

- Success
- Failure
- Partial Success

---

## Error

Represents a business failure.

Errors contain:

- Error Code
- Error Message
- Error Category

---

## Audit Information

Represents immutable audit metadata.

Contains:

- Created By
- Created At
- Modified By
- Modified At

---

# Shared Principles

- Every business object has an Identifier.
- Every published object has a Version.
- Every operation has a Correlation ID.
- Every state transition records a Timestamp.
- Every business failure produces an Error.
- Every modification generates Audit Information.

---

# Related Domains

The Shared Kernel is referenced by all domains.

No business rules belong exclusively to the Shared Kernel.

---

# Related Documents

- DM-000 Domain Overview
- All Domain Documents