# Changelog

## [2.0.0] — Unreleased

### Breaking Changes

- `DateTimeConfiguration.Resolution` (`TimeSpan?`) is now required when `EndDateTimeKind` is `Inclusive`.
  Any existing configuration with `EndDateTimeKind.Inclusive` and no `Resolution` is now `IsValid == false`.
  **Migration:** Set `Resolution = TimeSpan.FromDays(1)` to preserve the previous day-level behaviour.

### Added

- `DateTimeConfiguration.Resolution` property: configures the smallest representable time unit for inclusive end-date systems.
  Supports any positive `TimeSpan` — typical values: `TimeSpan.FromDays(1)`, `TimeSpan.FromSeconds(1)`, `TimeSpan.FromMilliseconds(1)`.
- ISO 8601 duration serialization for `Resolution` (e.g. `"PT1S"`, `"P1D"`, `"PT0.001S"`) via `System.Xml.XmlConvert`.
- DST-aware day arithmetic (`AddGermanDay`/`SubtractGermanDay`) continues to be used when `Resolution == TimeSpan.FromDays(1)`.
- Sub-day resolution support: for resolutions other than 1 day, plain UTC arithmetic is used (DST-independent).
