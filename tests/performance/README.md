# Performance Testing

This folder contains repository-owned performance and load testing assets.

## Structure

- `k6/` contains the first implementation based on k6.

Repo terminology stays generic:

- `scenario`: the flow under test
- `profile`: the workload shape applied to the scenario
- `performance testing`: umbrella capability
- `load testing`: one subset of performance testing

## Current scope

The first thin slice is a smoke scenario for Admin API reliability investigation.

Current implementation path:

- `k6/scenarios/admin-smoke.js`

Current wrapper:

- `../../scripts/run-admin-performance-smoke.sh`

## Intent

This initial slice is for:

- repeatable local repro support
- lightweight API-level reliability checks
- validating the toolchain and conventions before broader profiles are added

This is not yet a full load-testing suite.
