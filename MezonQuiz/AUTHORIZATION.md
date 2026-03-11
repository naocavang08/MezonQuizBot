# Authorization Guide

This document describes how authorization is implemented in the backend and how to extend it safely.

## Model Overview

- Authentication uses JWT bearer tokens.
- Permission policies are dynamic and based on permission names in the format `resource.action`.
- Controllers use `PermissionAuthorize(...)` to bind endpoints to permission checks.
- Resource ownership checks are enforced in controller guards for sensitive operations.

## Permission Policy

- Policy prefix: `perm:`
- Effective policy name example: `perm:quizzes.update`
- Dynamic policy provider resolves policy names at runtime.

Core files:
- `src/WebApp/Authorization/PermissionPolicy.cs`
- `src/WebApp/Authorization/PermissionPolicyProvider.cs`
- `src/WebApp/Authorization/PermissionAuthorizationHandler.cs`
- `src/WebApp/Authorization/PermissionNames.cs`
- `src/WebApp/Authorization/PermissionAuthorizeAttribute.cs`

## Endpoint Security Rules

### Public endpoints
- `POST /api/Login`
- `POST /api/Login/mezon-callback`
- `GET /api/PublicQuiz`

### Permission-protected endpoints
- All other API endpoints are permission-protected using `PermissionAuthorize(...)`.

### Ownership-protected endpoints
Additional ownership checks are enforced even when permission is granted.

- Quiz mutate endpoints in `QuizController`:
  - update/delete quiz
  - question/option add-update-delete
  - settings update
- User resource endpoints in `UserController`:
  - get/update/delete user
  - get user roles

Rule summary:
- Quiz mutate: requester must be quiz creator or have a system role.
- User sensitive resource: requester must be target user or have a system role.

## Trust Boundary Rules

Never trust identity IDs from query/body for authorization.

- Session host actions derive host identity from JWT `ClaimTypes.NameIdentifier`.
- Session host actions do not accept `hostId` query parameter anymore.
- Session create derives host identity from JWT in controller and passes it as trusted input to service.

## Seeder and Default Roles

`Seeder` provisions:
- Permissions (idempotent by `resource + action`).
- `super_admin` with all permissions.

If additional default roles are added, keep assignment idempotent:
- create role if missing,
- update role metadata if changed,
- add only missing `role_permissions`.

## Logging and Auditing

Controllers log warnings for:
- invalid/missing identity claim (unauthorized requests),
- forbidden ownership violations.

Review logs for repeated patterns indicating abuse attempts.

## Safe Extension Checklist

When adding a new endpoint:
1. Add/confirm permission constant in `PermissionNames`.
2. Add/confirm seed permission in `Seeder`.
3. Annotate endpoint with `PermissionAuthorize(...)` or `AllowAnonymous`.
4. If endpoint mutates user-owned data, add ownership guard.
5. Ensure frontend does not send identity fields used for authorization.
6. Add/adjust tests for expected `401`, `403`, and success paths.

## Manual Verification Matrix

Run these checks after authorization changes:

1. Anonymous can access only public endpoints.
2. Authenticated user without permission gets `403`.
3. Authenticated user with permission but not owner gets `403` for ownership-protected actions.
4. Owner can mutate own resources.
5. System role can perform ownership-protected actions.
6. Session host actions reject attempts that do not match JWT identity.
