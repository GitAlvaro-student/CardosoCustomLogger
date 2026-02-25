# Copilot Instructions

## General Guidelines
- Ensure tests are independent and do not alter production code.
- Avoid using reflection in tests.
- When tests fail due to attempting to subclass sealed types, avoid subclassing sealed production types; prefer using public API and existing mocks and wrappers.

## Testing Framework
- Use xUnit for unit testing in .NET projects.