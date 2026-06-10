# Design: Configurable End-Date Resolution (v2.0.0 Breaking Change)

**Date:** 2026-06-10  
**Branch:** feature branch off `main`  
**Target release:** v2.0.0  
**Reference:** [chronomeleon ChronoAssumption](https://github.com/Hochfrequenz/chronomeleon/blob/22d9b860cab91b8ebb6802212697d2a2e3f46138/src/chronomeleon/models/chrono_assumption.py#L16-L24), [chronomeleon mapping.py](https://github.com/Hochfrequenz/chronomeleon/blob/22d9b860cab91b8ebb6802212697d2a2e3f46138/src/chronomeleon/mapping.py#L75)

---

## Problem

When converting an inclusive end date to an exclusive one, the converter currently always adds exactly one German calendar day (`AddGermanDay`). This is only correct for date-level (day-resolution) data. For systems that store end datetimes with sub-day precision (e.g. `2022-10-31T23:59:59` at 1-second resolution, or `2022-10-31T23:59:59.999` at 1-millisecond resolution), the right conversion is to add the smallest representable time unit — the *resolution* — to the inclusive value to obtain the exclusive value.

Without a configurable resolution the library cannot correctly handle these cases, and callers have no way to express what precision their system uses.

---

## Solution

Add a `TimeSpan? Resolution` property to `DateTimeConfiguration`. When `EndDateTimeKind == Inclusive`, `Resolution` becomes **required** (validated in `IsValid`). The converter uses it in place of the hard-coded `AddGermanDay`/`SubtractGermanDay` calls in the end-date conversion branch.

This is modelled directly after [chronomeleon's `ChronoAssumption.resolution`](https://github.com/Hochfrequenz/chronomeleon/blob/22d9b860cab91b8ebb6802212697d2a2e3f46138/src/chronomeleon/models/chrono_assumption.py#L16-L24).

---

## Model Changes — `DateTimeConfiguration`

### New property

```csharp
/// <summary>
/// The smallest representable time unit in this system's end-date field.
/// Must be set if and only if <see cref="EndDateTimeKind"/> is <see cref="EndDateTimeKind.Inclusive"/>.
/// Adding one resolution unit to an inclusive end yields the equivalent exclusive end.
/// Typical values: TimeSpan.FromDays(1), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1).
/// </summary>
[System.Text.Json.Serialization.JsonPropertyName("resolution")]
public TimeSpan? Resolution { get; set; }
```

### Updated `IsValid`

The existing check already requires `EndDateTimeKind != null` when `IsEndDate == true`. The new rule adds:

- `EndDateTimeKind == Inclusive` → `Resolution != null`
- `EndDateTimeKind == Exclusive` → `Resolution == null`
- `IsEndDate == false` → `Resolution == null`

In code:

```csharp
public bool IsValid =>
    (
        (IsEndDate == false && !EndDateTimeKind.HasValue && !Resolution.HasValue)
        || (
            IsEndDate
            && EndDateTimeKind.HasValue
            && (EndDateTimeKind != MaKoDateTimeConverter.EndDateTimeKind.Inclusive || Resolution.HasValue)
            && (EndDateTimeKind != MaKoDateTimeConverter.EndDateTimeKind.Exclusive || !Resolution.HasValue)
        )
    )
    && ((IsGas == false && !IsGasTagAware.HasValue) || (IsGas && IsGasTagAware.HasValue));
```

**Breaking change:** any existing `DateTimeConfiguration` with `EndDateTimeKind.Inclusive` and no `Resolution` becomes `IsValid == false`.

---

## Converter Changes — `MaKoDateTimeConverter.Convert`

Replace the end-date conversion block (currently lines 313–331) with:

```csharp
if (
    conversionConfiguration.Source.IsEndDate
    && conversionConfiguration.Target.IsEndDate
    && conversionConfiguration.Source.EndDateTimeKind!.Value
        != conversionConfiguration.Target.EndDateTimeKind!.Value
)
{
    if (conversionConfiguration.Source.EndDateTimeKind == EndDateTimeKind.Inclusive) // target is exclusive
    {
        var resolution = conversionConfiguration.Source.Resolution!.Value;
        result = resolution == TimeSpan.FromDays(1)
            ? AddGermanDay(result.UtcDateTime)
            : result.UtcDateTime + resolution;
    }

    if (conversionConfiguration.Source.EndDateTimeKind == EndDateTimeKind.Exclusive) // target is inclusive
    {
        var resolution = conversionConfiguration.Target.Resolution!.Value;
        result = resolution == TimeSpan.FromDays(1)
            ? SubtractGermanDay(result.UtcDateTime)
            : result.UtcDateTime - resolution;
    }
}
```

### Notes

- `Source.Resolution` is used when converting **away from** an inclusive source (the resolution describes the source system's precision).
- `Target.Resolution` is used when converting **to** an inclusive target (we must express the result in the target system's precision).
- The `!.Value` null-forgiving dereferences are safe because `IsValid` guarantees `Resolution != null` for `Inclusive` configurations, and the converter checks `IsValid` before proceeding.
- **1-day special case:** `TimeSpan.FromDays(1)` delegates to the existing DST-aware `AddGermanDay`/`SubtractGermanDay` helpers. All other resolutions apply arithmetic UTC addition/subtraction. This is correct because sub-day units are DST-independent, while day-boundary crossings in the German energy market must respect Berlin local-time midnight.
- `AddGermanDay`, `SubtractGermanDay`, and all gas-related conversion logic are **unchanged**.

---

## Testing Plan

### 3a. `DateTimeConfigurationTests` — validation

New `[TestCase]` rows in the existing `Test_Validation` test:

| JSON snippet | `isValid` | Reason |
|---|---|---|
| `isEndDate:true, endDateTimeKind:INCLUSIVE` (no resolution) | `false` | Inclusive without resolution |
| `isEndDate:true, endDateTimeKind:INCLUSIVE, resolution:"00:00:01"` | `true` | Inclusive with 1s resolution |
| `isEndDate:true, endDateTimeKind:EXCLUSIVE, resolution:"00:00:01"` | `false` | Exclusive must not have resolution |
| `isEndDate:false, resolution:"00:00:01"` | `false` | Non-end-date must not have resolution |

### 3b. Conversion — sub-day resolutions

New `[TestCase]` rows covering inclusive→exclusive and exclusive→inclusive:

| Source (UTC) | Source kind | Resolution | Target kind | Expected (UTC) |
|---|---|---|---|---|
| `2022-10-31T22:59:59Z` (Berlin 23:59:59) | Inclusive | 1 second | Exclusive | `2022-10-31T23:00:00Z` (Berlin 00:00:00 Nov 1) |
| `2022-10-31T22:59:59.999Z` | Inclusive | 1 ms | Exclusive | `2022-10-31T23:00:00Z` (Berlin 00:00:00 Nov 1) |
| `2022-10-31T23:00:00Z` | Exclusive | — | Inclusive (1s) | `2022-10-31T22:59:59Z` |
| `2022-10-31T23:00:00Z` | Exclusive | — | Inclusive (1ms) | `2022-10-31T22:59:59.999Z` |

### 3c. Conversion — 1-day resolution (DST correctness)

Verify DST-aware path is preserved:

| Source (UTC) | Direction | Expected (UTC) | Why |
|---|---|---|---|
| `2023-03-25T23:00:00Z` (Berlin midnight Mar 26, spring-forward eve) | Incl→Excl, 1 day | `2023-03-26T22:00:00Z` (Berlin midnight Mar 27) | DST: only 23h difference |
| `2023-10-28T22:00:00Z` (Berlin midnight Oct 29, fall-back eve) | Incl→Excl, 1 day | `2023-10-29T23:00:00Z` (Berlin midnight Oct 30) | DST: 25h difference |
| `2022-10-31T23:00:00Z` (Berlin midnight Nov 1, no DST) | Incl→Excl, 1 day | `2022-11-01T23:00:00Z` | Normal 24h |

### 3d. `DateTimeConversionConfigurationTests` — `GetInverted()` round-trip

One test asserting that `GetInverted()` on a config with `Resolution = TimeSpan.FromSeconds(1)` on the inclusive side produces a config where the formerly-inclusive side retains its `Resolution`.

---

## Branch & Release Notes

- Work on a dedicated feature branch (e.g. `feature/configurable-enddate-resolution`)
- Publish as **v2.0.0** (SemVer major bump — breaking change)
- Changelog entry: "Resolution is now required for inclusive end-date configurations. Set `Resolution = TimeSpan.FromDays(1)` to preserve the previous day-level behaviour."
