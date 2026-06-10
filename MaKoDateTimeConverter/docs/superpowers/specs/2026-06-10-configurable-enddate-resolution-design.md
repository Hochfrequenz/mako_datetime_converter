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
/// Must be positive (greater than TimeSpan.Zero).
/// </summary>
[System.Text.Json.Serialization.JsonPropertyName("resolution")]
[System.Text.Json.Serialization.JsonConverter(typeof(TimeSpanJsonConverter))]
public TimeSpan? Resolution { get; set; }
```

`System.Text.Json` has no built-in `TimeSpan` converter (net8/net9/net10). A `TimeSpanJsonConverter` must be added to the library. It serialises using `TimeSpan.ToString("c")` / `TimeSpan.ParseExact(s, "c", CultureInfo.InvariantCulture)`, producing human-readable strings such as `"00:00:01"` (1 second), `"00:00:00.0010000"` (1 ms), `"1.00:00:00"` (1 day). This converter must be applied to `TimeSpan?` (nullable) as well.

This is consistent with the existing `JsonStringEnumConverter` pattern already in use for `EndDateTimeKind`.

### Updated `IsValid`

Full validity conditions (complete list, new rules emphasised):

- `IsEndDate == false` → `EndDateTimeKind == null` AND `Resolution == null`
- `IsEndDate == true` → `EndDateTimeKind != null`
  - AND **`EndDateTimeKind == Inclusive` → `Resolution != null` AND `Resolution > TimeSpan.Zero`** *(new)*
  - AND **`EndDateTimeKind == Exclusive` → `Resolution == null`** *(new)*
- `IsGas == true` → `IsGasTagAware != null`
- `IsGas == false` → `IsGasTagAware == null`

In code:

```csharp
public bool IsValid =>
    (
        (IsEndDate == false && !EndDateTimeKind.HasValue && !Resolution.HasValue)
        || (
            IsEndDate
            && EndDateTimeKind.HasValue
            && (EndDateTimeKind != MaKoDateTimeConverter.EndDateTimeKind.Inclusive
                || (Resolution.HasValue && Resolution.Value > TimeSpan.Zero))
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
    else if (conversionConfiguration.Source.EndDateTimeKind == EndDateTimeKind.Exclusive) // target is inclusive
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
- The inner `if`/`else if` pair (rather than two `if`s) is intentional: the outer guard already ensures the two branches are mutually exclusive, but `else if` makes this explicit.
- The `!.Value` null-forgiving dereferences are safe because `IsValid` guarantees `Resolution != null` for `Inclusive` configurations, and the converter checks `IsValid` before proceeding.
- **1-day special case:** `TimeSpan.FromDays(1)` delegates to the existing DST-aware `AddGermanDay`/`SubtractGermanDay` helpers. All other resolutions apply arithmetic UTC addition/subtraction. This is correct because sub-day units are DST-independent, while day-boundary crossings in the German energy market must respect Berlin local-time midnight.
- `AddGermanDay`, `SubtractGermanDay`, and all gas-related conversion logic are **unchanged**.
- **Same-kind identity case:** when Source and Target both have `EndDateTimeKind.Inclusive` (possibly with different `Resolution` values), the outer `EndDateTimeKind != EndDateTimeKind` guard is false and no end-date conversion is applied. This is intentional — no resolution adjustment is needed when both sides use the same inclusivity convention.

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
| `isEndDate:true, endDateTimeKind:INCLUSIVE, resolution:"-00:00:01"` | `false` | Resolution must be positive |

### 3b. Conversion — sub-day resolutions

New `[TestCase]` rows covering Inclusive→Exclusive and Exclusive→Inclusive. **Source Resolution** applies for Inclusive→Exclusive; **Target Resolution** applies for Exclusive→Inclusive.

| Source (UTC) | Source kind | Source Res. | Target kind | Target Res. | Expected (UTC) |
|---|---|---|---|---|---|
| `2022-10-31T22:59:59Z` (Berlin 23:59:59) | Inclusive | 1 second | Exclusive | — | `2022-10-31T23:00:00Z` (Berlin 00:00:00 Nov 1) |
| `2022-10-31T22:59:59.999Z` | Inclusive | 1 ms | Exclusive | — | `2022-10-31T23:00:00Z` (Berlin 00:00:00 Nov 1) |
| `2022-10-31T23:00:00Z` | Exclusive | — | Inclusive | 1 second | `2022-10-31T22:59:59Z` |
| `2022-10-31T23:00:00Z` | Exclusive | — | Inclusive | 1 ms | `2022-10-31T22:59:59.999Z` |

### 3c. Conversion — 1-day resolution (DST correctness)

Verify that `Resolution = TimeSpan.FromDays(1)` still uses the DST-aware `AddGermanDay`/`SubtractGermanDay` path:

| Source (UTC) | Direction | Source Res. | Expected (UTC) | Why |
|---|---|---|---|---|
| `2023-03-25T23:00:00Z` (Berlin midnight Mar 26, spring-forward eve) | Incl→Excl | 1 day | `2023-03-26T22:00:00Z` (Berlin midnight Mar 27) | DST: only 23h difference |
| `2023-10-28T22:00:00Z` (Berlin midnight Oct 29, fall-back eve) | Incl→Excl | 1 day | `2023-10-29T23:00:00Z` (Berlin midnight Oct 30) | DST: 25h difference |
| `2022-10-31T23:00:00Z` (Berlin midnight Nov 1, no DST) | Incl→Excl | 1 day | `2022-11-01T23:00:00Z` | Normal 24h |

### 3d. `DateTimeConversionConfigurationTests` — `GetInverted()` round-trip

One new test verifying that `GetInverted()` preserves `Resolution` on the correct side. Concrete config:

```
Source: IsEndDate=true, EndDateTimeKind=Inclusive, Resolution=TimeSpan.FromSeconds(1), IsGas=false
Target: IsEndDate=true, EndDateTimeKind=Exclusive, IsGas=false
```

After `GetInverted()`:

```
Source: IsEndDate=true, EndDateTimeKind=Exclusive, IsGas=false        // formerly Target
Target: IsEndDate=true, EndDateTimeKind=Inclusive, Resolution=1s, IsGas=false  // formerly Source
```

Assert: `invertedConfig.Target.Resolution == TimeSpan.FromSeconds(1)` and `invertedConfig.Source.Resolution == null`.

### 3e. Migration of existing tests (required by breaking change)

Every existing test that constructs a `DateTimeConfiguration` with `EndDateTimeKind.Inclusive` must add `Resolution = TimeSpan.FromDays(1)` to remain valid. The affected tests are:

| File | Test method | Fix |
|---|---|---|
| `MaKoDateTimeConverterTests.cs` | `Test_Strom_InclusiveEnd_To_ExclusiveEnd` | Add `Resolution = TimeSpan.FromDays(1)` to `Source` |
| `MaKoDateTimeConverterTests.cs` | `Test_Gas_InclusiveEnd_To_ExclusiveEnd` | Add `Resolution = TimeSpan.FromDays(1)` to `Source` |
| `MaKoDateTimeConverterTests.cs` | `Test_Gas_InclusiveEnd_To_ExclusiveEnd_And_Make_GasTagAware` | Add `Resolution = TimeSpan.FromDays(1)` to `Source` |
| `MaKoDateTimeConverterTests.cs` | `Test_Identity` | Add `Resolution = TimeSpan.FromDays(1)` to both `Source` and `Target` |
| `MaKoDateTimeConverterTests.cs` | `DateTime_With_Unspecified_Kind_Shall_Raise_ArgumentException` | Add `Resolution = TimeSpan.FromDays(1)` to `Target` |
| `MinimalWorkingExample.cs` | `Test_Mwe` | Add `Resolution = TimeSpan.FromDays(1)` to `Target` |
| `DateTimeConversionConfigurationTests.cs` | `Test_Validation` (INCLUSIVE JSON case) | Add `"resolution":"1.00:00:00"` to the source JSON, or split into two cases: one valid (with resolution) and one invalid (without) |
| `DateTimeConversionConfigurationTests.cs` | `Test_Deserialization` | Add `"resolution":"1.00:00:00"` to the source object in the JSON fixture (source has `endDateTimeKind:INCLUSIVE`) |

---

## Branch & Release Notes

- Work on a dedicated feature branch (e.g. `feature/configurable-enddate-resolution`)
- Publish as **v2.0.0** (SemVer major bump — breaking change)
- Changelog entry: "Resolution is now required for inclusive end-date configurations. Set `Resolution = TimeSpan.FromDays(1)` to preserve the previous day-level behaviour."
