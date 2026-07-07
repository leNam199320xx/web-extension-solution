# 🔄 Plugin Lifecycle - Metadata-Driven Runtime (.NET 10)

---

# 1. 🎯 MỤC TIÊU

Định nghĩa toàn bộ vòng đời của một plugin trong hệ thống:

- Từ lúc upload → kiểm duyệt → ký số → lưu trữ → runtime execution → unload
- Đảm bảo an toàn zero-trust trong mọi giai đoạn
- Không có plugin nào được “tự do” trong hệ thống

---

# 2. 🧠 OVERVIEW FLOW

```
Developer Upload
      ↓
Validation Pipeline
      ↓
Security Scanning (SAST + dependency check)
      ↓
Manual / Auto Approval
      ↓
Manifest Signing (KMS / HSM)
      ↓
Storage in Repository
      ↓
Runtime Load (AssemblyLoadContext)
      ↓
Execution
      ↓
Monitoring + Logging
      ↓
Unload / Update / Revoke
```

---

# 3. 📦 PHASE 1 - UPLOAD

## Input:

- Plugin package (.dll / .zip / .nupkg)
- Manifest file (unsigned)

---

## Actions:

- Receive file via API
- Generate temporary plugin ID
- Store raw artifact in quarantine zone

---

## Security rule:

👉 Plugin chưa được verify = KHÔNG được trust

---

# 4. 🔍 PHASE 2 - VERIFICATION

Automated verification via the Verification Engine (see `docs/architecture/verification-engine-spec.md`).

## Pipeline stages:

1. Structure Validation — package format, file existence, size limits
2. Manifest Validation — schema, fields, permission format
3. Static Analysis (IL Scan) — forbidden APIs, reflection, threading abuse
4. Dependency Audit — blacklisted packages, CVE vulnerabilities
5. Security Scan — secrets detection, credential patterns
6. Standards Compliance — naming, docs, test coverage
7. Sandbox Execution (optional) — load + execute in isolated environment

## Permission extraction:

- Parse `manifest.json` permissions array
- Parse `permissions.json` (if included) for justifications
- Auto-classify risk level per permission (Low / Medium / High / Critical)
- Generate Permission Review Summary

See `docs/architecture/permission-review-spec.md` for details.

## Result:

- PASS → move to approval (with permission summary attached)
- PASS WITH WARNINGS → move to approval (warnings flagged for reviewer)
- FAIL → rejected immediately with detailed report sent to developer

Standards enforced: see `docs/standards/extension-development-standard.md`.

---

# 5. 🧾 PHASE 3 - APPROVAL

## Permission Review:

Reviewer sees structured permission summary:
- List of all requested permissions with risk levels
- Justification for each permission (from `permissions.json`)
- Auto-generated flags (write-access, external-api, pii-access, wildcard, etc.)
- Permission diff vs previous version (if upgrade)
- Verification engine result summary

See `docs/architecture/permission-review-spec.md` for full reviewer interface.

## Actions:

- **Approve** — all permissions accepted
- **Approve with conditions** — accepted with requirements for next version
- **Reject** — specify which permissions are denied and why
- **Request info** — ask developer for clarification on specific permissions

## Auto-approval (if enabled):

- All permissions Low risk + trusted publisher → auto-approve
- Same permissions as previous approved version → auto-approve
- Any Critical permission → NEVER auto-approve

## Security enforcement:

- MFA required for admin approval
- Immutable audit log
- Review decision stored in `permission_reviews` table

---

# 6. 🔐 PHASE 4 - SIGNING

## Process:

- Manifest is signed using:
  - RSA / ECDSA
  - KMS / HSM key storage

---

## Output:

Signed Manifest contains:

- Plugin ID
- SHA-256 hash
- Permissions (capabilities)
- Version constraints
- Signature

---

## Rule:

👉 Unsigned plugin = cannot exist in runtime system

---

# 7. 🗄 PHASE 5 - STORAGE

## Stored in:

- Plugin Repository (DB + file storage)

---

## Features:

- Versioning support
- Immutable history
- Revocation tracking

---

# 8. 🚀 PHASE 6 - LOAD (RUNTIME)

## Trigger:

- API request
- Scheduled preload
- Warm-up process

---

## Process:

1. Validate manifest again
2. Verify signature
3. Verify SHA-256 hash
4. Check revocation list
5. Resolve capabilities
6. Load assembly into AssemblyLoadContext

---

## Important:

👉 Each plugin loads into isolated context

---

# 9. ⚙️ PHASE 7 - EXECUTION

## Execution pipeline:

```
Plugin Request
   ↓
Capability Injection
   ↓
Execution Start
   ↓
Timeout Monitor
   ↓
Resource Tracking
   ↓
Return Result
```

---

## Rules:

- Must respect timeout
- Must respect capability restrictions
- Must NOT access system directly

---

# 10. ⏱ PHASE 8 - RUNTIME CONTROL

## Enforced limits:

- Execution timeout
- Memory monitoring
- Cancellation token

---

## Behavior:

- Timeout → terminate execution
- Overflow → abort plugin
- Crash → isolate failure only

---

# 11. 📊 PHASE 9 - OBSERVABILITY

## Logged data:

- TraceId
- PluginId
- Execution duration
- Status
- Error details (if any)

---

## Output format:

- Structured JSON logs
- Sent to centralized logging system

---

# 12. 🔁 PHASE 10 - UPDATE / HOT RELOAD

## Flow:

```
Stop new requests
↓
Wait active execution finish
↓
Unload old AssemblyLoadContext
↓
Load new version
↓
Warm-up
↓
Resume traffic
```

---

## Rule:

👉 No request interruption allowed

---

# 13. 🧯 PHASE 11 - UNLOAD / REVOKE

## Trigger:

- Admin revoke
- Security incident
- Version update

---

## Actions:

- Remove from active registry
- Stop execution
- Unload runtime context
- Mark as revoked in DB

---

# 14. 🔐 SECURITY GUARANTEE ACROSS LIFECYCLE

## Always enforced:

- Validation before load
- Signature verification before execution
- Capability enforcement during runtime
- Timeout enforcement
- Fail-closed behavior

---

## Never allowed:

- Execute unsigned plugin
- Skip validation pipeline
- Bypass capability system
- Persist revoked plugin in runtime

---

# 15. 🎯 LIFECYCLE PRINCIPLE

> A plugin is never trusted at any stage — only continuously verified.

---

# 🏁 END OF PLUGIN LIFECYCLE