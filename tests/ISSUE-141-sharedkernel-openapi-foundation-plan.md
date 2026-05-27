# Issue 141 SharedKernel OpenAPI Foundation Plan

Issue link: [`#141`](https://github.com/danigutsch/ViajantesTurismo/issues/141)

- [x] Confirm scope stays OpenAPI-focused and does not turn into a generic API framework
- [x] Add `src/SharedKernel/SharedKernel.OpenApi`
- [x] Move reusable OpenAPI helpers into the new shared project:
    - boundary document definition
    - reusable document registration extensions
    - build-time OpenAPI generation detection helper
- [x] Update the Admin API to consume the shared OpenAPI foundation for boundary document registration

## PR Breakdown

### PR 1: SharedKernel.OpenApi foundation

Branch name: `feature/issue-141-sharedkernel-openapi-foundation`

- [x] Create `SharedKernel.OpenApi` project
- [x] Add reusable OpenAPI document registration helper types
- [x] Add reusable build-time OpenAPI generation detection helper
- [x] Add the new project to the solution
- [x] Update the Admin API to consume the shared registration helper

Suggested commit split:

- [ ] Commit 1: scaffold `SharedKernel.OpenApi`
- [ ] Commit 2: add reusable OpenAPI helper types
- [ ] Commit 3: wire the Admin API to consume the shared foundation

## Notes

- This issue is a prerequisite for the canonical OpenAPI artifact work in `#115`.
- Do not add Admin-specific artifact publishing into `Admin.Contracts/OpenApi` in this PR.
