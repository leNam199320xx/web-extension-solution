# BR-300 Manifest Business Rules

## Overview

This document defines the business rules governing the Plugin Manifest within the Metadata-Driven Secure Plugin Runtime.

The Manifest is the authoritative metadata contract between a plugin and the Runtime. Every plugin shall provide a valid Manifest before it can be registered, deployed or executed.

---

# Scope

This document applies to:

- Manifest Schema
- Plugin Metadata
- Versioning
- Dependencies
- Capability Declaration
- Extension Points
- Compatibility
- Validation

---

## BR-301 Manifest Required

Every plugin package shall contain exactly one Manifest document.

Plugin packages without a Manifest shall be rejected.

Multiple Manifest documents within the same package are prohibited.

### Related Functional Requirements

- FR-201
- FR-203
- FR-204

---

## BR-302 Manifest Schema Compliance

The Manifest shall conform to the official Runtime Manifest Schema.

Schema validation shall occur before plugin registration.

### Related Functional Requirements

- FR-202
- FR-205
- FR-223
- FR-803

---

## BR-303 Mandatory Metadata

Every Manifest shall include all mandatory metadata defined by the platform.

Mandatory metadata shall include at minimum:

- Plugin Identifier
- Plugin Name
- Plugin Version
- Publisher
- Runtime Version
- Entry Point

Missing mandatory metadata shall invalidate the Manifest.

### Related Functional Requirements

- FR-203
- FR-204
- FR-206

---

## BR-304 Metadata Integrity

Manifest metadata shall accurately describe the packaged plugin.

Metadata shall not contain conflicting, misleading or duplicated information.

### Related Functional Requirements

- FR-204
- FR-205
- FR-207

---

## BR-305 Capability Declaration

Every privileged operation required by a plugin shall be explicitly declared within the Manifest.

Capabilities shall not be granted implicitly by the Runtime.

### Related Functional Requirements

- FR-210
- FR-301
- FR-306
- FR-815

---

## BR-306 Dependency Declaration

Every external dependency shall be explicitly declared within the Manifest.

Undeclared dependencies shall not be resolved by the Runtime.

### Related Functional Requirements

- FR-207
- FR-307
- FR-507
- FR-807

---

## BR-307 Compatibility Declaration

The Manifest shall declare the minimum supported Runtime version.

The Runtime shall reject plugins targeting unsupported platform versions.

### Related Functional Requirements

- FR-212
- FR-223
- FR-317
- FR-815

---

## BR-308 Extension Point Declaration

Every Runtime extension implemented by a plugin shall be declared within the Manifest.

Undeclared extension points shall not be loaded.

### Related Functional Requirements

- FR-219
- FR-220
- FR-221
- FR-532

---

## BR-309 Manifest Versioning

Manifest schema versions shall follow Semantic Versioning.

Backward compatibility shall be maintained according to the published compatibility policy.

### Related Functional Requirements

- FR-223
- FR-224
- FR-317

---

## BR-310 Manifest Immutability

A published Manifest shall become immutable.

Any modification to Manifest content shall require publication of a new plugin version.

### Related Functional Requirements

- FR-225
- FR-106
- FR-111

---

# Summary

| Rule Group | Rules |
|------------|------:|
| Manifest | 10 |

---

# Related Documents

- FR-200 Manifest
- FR-300 Capability
- FR-800 SDK
- UC-200 Manifest
- NFR-001 Non-Functional Requirements