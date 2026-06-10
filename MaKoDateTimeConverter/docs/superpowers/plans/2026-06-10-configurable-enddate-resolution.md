# Configurable End-Date Resolution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a required `TimeSpan? Resolution` property to `DateTimeConfiguration` that governs how inclusive end dates are converted to/from exclusive ones, replacing the hard-coded one-German-day assumption.

**Architecture:** A new `TimeSpanJsonConverter` (ISO 8601 duration via `XmlConvert`) handles serialisation. `DateTimeConfiguration.IsValid` gains two new rules enforcing that `Resolution` is set iff `EndDateTimeKind == Inclusive`. The converter's end-date block replaces `AddGermanDay`/`SubtractGermanDay` with resolution-based arithmetic (1-day resolution still delegates to those DST-aware helpers).

**Tech Stack:** C# (.NET 8/9/10), NUnit 4, AwesomeAssertions, `System.Xml.XmlConvert` for ISO 8601 duration strings.

**Spec:** `docs/superpowers/specs/2026-06-10-configurable-enddate-resolution-design.md`

**Branch:** `feature/configurable-enddate-resolution`

**Baseline:** 67 tests pass. Run from solution root: `dotnet test`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `MaKoDateTimeConverter/TimeSpanJsonConverter.cs` | `JsonConverter<TimeSpan?>` using ISO 8601 via `XmlConvert` |
| Modify | `MaKoDateTimeConverter/DateTimeConfiguration.cs` | Add `Resolution` property + update `IsValid` |
| Modify | `MaKoDateTimeConverter/MaKoDateTimeConverter.cs` | Replace end-date conversion block (lines 313–331) |
| Modify | `MaKoDateTimeConverterTests/DateTimeConfigurationTests.cs` | Serialisation tests + new validation test cases |
| Modify | `MaKoDateTimeConverterTests/MaKoDateTimeConverterTests.cs` | Migrate existing tests; add DST and sub-day tests |
| Modify | `MaKoDateTimeConverterTests/DateTimeConversionConfigurationTests.cs` | Migrate existing test; add `GetInverted()` test |
| Modify | `MaKoDateTimeConverterTests/MinimalWorkingExample.cs` | Migrate existing test |

---

## Task 1: Add `TimeSpanJsonConverter`

**Files:**
- Create: `MaKoDateTimeConverter/MaKoDateTimeConverter/TimeSpanJsonConverter.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConfigurationTests.cs`

- [ ] **Step 1.1: Write failing serialisation tests**

Add the following two test methods to `DateTimeConfigurationTests.cs` (inside the existing `DateTimeConfigurationTests` class, after the existing `Test_Validation` method):

```csharp
[Test]
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"PT1S\"}", "00:00:01")]
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"P1D\"}", "1.00:00:00")]
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\"}", null)]
public void Test_Resolution_Deserialization(string json, string? expectedResolutionString)
{
    var config = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(json);
    config.Should().NotBeNull();
    if (expectedResolutionString is null)
        config!.Resolution.Should().BeNull();
    else
        config!.Resolution.Should().Be(TimeSpan.Parse(expectedResolutionString));
}

[Test]
[TestCase("PT1S")]
[TestCase("P1D")]
[TestCase("PT0.001S")]
public void Test_Resolution_Serialization_RoundTrip(string iso8601Duration)
{
    var expected = System.Xml.XmlConvert.ToTimeSpan(iso8601Duration);
    var config = new DateTimeConfiguration
    {
        IsEndDate = true,
        EndDateTimeKind = EndDateTimeKind.Inclusive,
        IsGas = false,
        Resolution = expected,
    };
    var json = System.Text.Json.JsonSerializer.Serialize(config);
    json.Should().Contain("\"resolution\":"); // key is present in JSON
    var deserialized = System.Text.Json.JsonSerializer.Deserialize<DateTimeConfiguration>(json);
    deserialized!.Resolution.Should().Be(expected); // round-trip equality is what matters
}
```

- [ ] **Step 1.2: Run tests — verify they fail to compile**

```
dotnet test
```

Expected: compile error — `Resolution` does not exist on `DateTimeConfiguration`.

- [ ] **Step 1.3: Create `TimeSpanJsonConverter.cs`**

Create `MaKoDateTimeConverter/MaKoDateTimeConverter/TimeSpanJsonConverter.cs`:

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace MaKoDateTimeConverter;

internal sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
{
    public override bool HandleNull => true;

    public override TimeSpan? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return XmlConvert.ToTimeSpan(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan? value,
        JsonSerializerOptions options
    )
    {
        if (!value.HasValue)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(XmlConvert.ToString(value.Value));
    }
}
```

- [ ] **Step 1.4: Add `Resolution` stub to `DateTimeConfiguration` (compile-only, no validation yet)**

In `MaKoDateTimeConverter/MaKoDateTimeConverter/DateTimeConfiguration.cs`, add this property after the `EndDateTimeKind` property (line 19):

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

Do NOT change `IsValid` yet.

- [ ] **Step 1.5: Run tests — verify serialisation tests pass, all others still green**

```
dotnet test
```

Expected: 73 passed (67 original + 3 deserialization + 3 round-trip), 0 failed.

- [ ] **Step 1.6: Commit**

```
git add MaKoDateTimeConverter/MaKoDateTimeConverter/TimeSpanJsonConverter.cs MaKoDateTimeConverter/MaKoDateTimeConverter/DateTimeConfiguration.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConfigurationTests.cs
git commit -m "feat: add TimeSpanJsonConverter (ISO 8601) and Resolution property stub"
```

---

## Task 2: Update `IsValid` and add validation tests

**Files:**
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverter/DateTimeConfiguration.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConfigurationTests.cs`

- [ ] **Step 2.1: Add failing validation test cases**

In `DateTimeConfigurationTests.cs`, add the following rows to the existing `[TestCase]` list on `Test_Validation`:

```csharp
// New cases for Resolution validation:
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\"}", false)] // inclusive, no resolution
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"PT1S\"}", true)] // inclusive with 1s
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"EXCLUSIVE\", \"resolution\": \"PT1S\"}", false)] // exclusive must not have resolution
[TestCase("{\"isGas\":false, \"isEndDate\": false, \"resolution\": \"PT1S\"}", false)] // non-end-date must not have resolution
[TestCase("{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\": \"INCLUSIVE\", \"resolution\": \"-PT1S\"}", false)] // negative resolution
```

- [ ] **Step 2.2: Run tests — verify new cases fail**

```
dotnet test
```

Expected: 5 of the new `Test_Validation` cases fail. All other 73 tests remain green.

- [ ] **Step 2.3: Update `IsValid` in `DateTimeConfiguration.cs`**

Replace the existing `IsValid` property:

```csharp
// BEFORE:
[JsonIgnore]
public bool IsValid =>
    (
        (IsEndDate == false && !EndDateTimeKind.HasValue)
        || (IsEndDate && EndDateTimeKind.HasValue)
    ) && ((IsGas == false && !IsGasTagAware.HasValue) || (IsGas && IsGasTagAware.HasValue));
```

With:

```csharp
[System.Text.Json.Serialization.JsonIgnore]
public bool IsValid =>
    (
        (IsEndDate == false && !EndDateTimeKind.HasValue && !Resolution.HasValue)
        || (
            IsEndDate
            && EndDateTimeKind.HasValue
            && (EndDateTimeKind != MaKoDateTimeConverter.EndDateTimeKind.Inclusive
                || (Resolution.HasValue && Resolution.Value > TimeSpan.Zero))
            && (EndDateTimeKind != MaKoDateTimeConverter.EndDateTimeKind.Exclusive
                || !Resolution.HasValue)
        )
    )
    && ((IsGas == false && !IsGasTagAware.HasValue) || (IsGas && IsGasTagAware.HasValue));
```

- [ ] **Step 2.4: Run tests — verify new validation cases pass, count failing tests**

```
dotnet test
```

Expected: the 5 new `Test_Validation` cases now pass. Total test count: 78. However, several existing converter tests will now FAIL because they use `EndDateTimeKind.Inclusive` without `Resolution`. Record the count of failures — you will fix them all in Task 3.

Typical failure message: `ArgumentException: The configuration is invalid`.

- [ ] **Step 2.5: Commit (with failing tests — intentional, keep local until Task 3 is done)**

> **Note:** Do NOT push this commit yet. Pushing a commit with failing tests will mark the CI run red. Complete Task 3 first, then push the two commits together.

```
git add MaKoDateTimeConverter/MaKoDateTimeConverter/DateTimeConfiguration.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConfigurationTests.cs
git commit -m "feat: enforce Resolution required for Inclusive end-date configurations (breaking change)"
```

---

## Task 3: Migrate existing tests + add DST test cases

This task fixes all tests broken by Task 2's `IsValid` change. Every `DateTimeConfiguration` with `EndDateTimeKind.Inclusive` needs `Resolution = TimeSpan.FromDays(1)` added.

**Files:**
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/MaKoDateTimeConverterTests.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConversionConfigurationTests.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/MinimalWorkingExample.cs`

- [ ] **Step 3.1: Fix `Test_Strom_InclusiveEnd_To_ExclusiveEnd` — add `Resolution` + DST cases**

In `MaKoDateTimeConverterTests.cs`, find `Test_Strom_InclusiveEnd_To_ExclusiveEnd`. **Replace the entire method** with the following (adds 2 DST `[TestCase]` rows and `Resolution = TimeSpan.FromDays(1)` on `Source`):

```csharp
[TestCase("2023-05-30T22:00:00Z", "2023-05-31T22:00:00Z")]
[TestCase("2023-05-31T22:00:00Z", "2023-06-01T22:00:00Z")]
[TestCase("2023-12-31T23:00:00Z", "2024-01-01T23:00:00Z")]
[TestCase("2023-12-01T23:00:00Z", "2023-12-02T23:00:00Z")]
[TestCase("2023-03-25T23:00:00Z", "2023-03-26T22:00:00Z")] // spring-forward DST: 23h gap
[TestCase("2023-10-28T22:00:00Z", "2023-10-29T23:00:00Z")] // fall-back DST: 25h gap
public void Test_Strom_InclusiveEnd_To_ExclusiveEnd(
    string dateTimeString,
    string expectedResultString
)
{
    var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
    var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
    var conversion = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromDays(1), // ADD THIS
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
    };
    var actual = dt.Convert(conversion);
    actual.Should().Be(expected);

    var invertedConfig = conversion.GetInverted();
    expected.Convert(invertedConfig).Should().Be(dt);
}
```

- [ ] **Step 3.2: Fix `Test_Gas_InclusiveEnd_To_ExclusiveEnd`**

Add `Resolution = TimeSpan.FromDays(1)` to `Source`:

```csharp
Source = new DateTimeConfiguration
{
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
    IsGas = true,
    IsGasTagAware = true,
},
```

- [ ] **Step 3.3: Fix `Test_Gas_InclusiveEnd_To_ExclusiveEnd_And_Make_GasTagAware`**

Add `Resolution = TimeSpan.FromDays(1)` to `Source`:

```csharp
Source = new DateTimeConfiguration
{
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
    IsGas = true,
    IsGasTagAware = false,
},
```

- [ ] **Step 3.4: Fix `Test_Identity`**

Add `Resolution = TimeSpan.FromDays(1)` to BOTH `Source` AND `Target`:

```csharp
Source = new DateTimeConfiguration
{
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
    IsGas = false,
},
Target = new DateTimeConfiguration
{
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
    IsGas = false,
},
```

- [ ] **Step 3.5: Fix `DateTime_With_Unspecified_Kind_Shall_Raise_ArgumentException`**

Add `Resolution = TimeSpan.FromDays(1)` to `Target`:

```csharp
Target = new DateTimeConfiguration
{
    IsGas = false,
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
},
```

- [ ] **Step 3.6: Fix `MinimalWorkingExample.cs` — `Test_Mwe`**

In `MinimalWorkingExample.cs`, find the `Target` configuration and add `Resolution`:

```csharp
Target = new DateTimeConfiguration
{
    IsEndDate = true,
    EndDateTimeKind = EndDateTimeKind.Inclusive,
    Resolution = TimeSpan.FromDays(1), // ADD THIS
    IsGas = true,
    IsGasTagAware = false,
},
```

- [ ] **Step 3.7: Fix `DateTimeConversionConfigurationTests.cs` — `Test_Validation` INCLUSIVE case**

In `DateTimeConversionConfigurationTests.cs`, find the `Test_Validation` method. The existing `[TestCase]` row with `INCLUSIVE` source and no resolution currently expects `isValid: true`. Change its expected value to `false`, and add a new row with resolution:

```csharp
// Change this existing row from true to false:
[TestCase(
    "{\"source\":{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\":\"INCLUSIVE\"}, \"target\":{\"isGas\":false,\"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}}",
    false  // was: true — now invalid because Source.Resolution is missing
)]
// Add this new row (with resolution → valid):
[TestCase(
    "{\"source\":{\"isGas\":false, \"isEndDate\": true, \"endDateTimeKind\":\"INCLUSIVE\", \"resolution\":\"P1D\"}, \"target\":{\"isGas\":false,\"isEndDate\": true, \"endDateTimeKind\":\"EXCLUSIVE\"}}",
    true
)]
```

- [ ] **Step 3.8: Run tests — all green**

```
dotnet test
```

Expected: all tests pass. Count should be 81 (78 after Task 2 + 2 DST cases + 1 new Test_Validation row).

- [ ] **Step 3.9: Commit**

```
git add MaKoDateTimeConverter/MaKoDateTimeConverterTests/MaKoDateTimeConverterTests.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConversionConfigurationTests.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/MinimalWorkingExample.cs
git commit -m "test: migrate existing tests to provide Resolution=1day on Inclusive configs; add DST test cases"
```

---

## Task 4: Update converter logic + add sub-day and round-trip tests

**Files:**
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverter/MaKoDateTimeConverter.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/MaKoDateTimeConverterTests.cs`
- Modify: `MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConversionConfigurationTests.cs`

- [ ] **Step 4.1: Add failing sub-day conversion tests**

Add four new test methods to `MaKoDateTimeConverterTests.cs` (after `Test_Strom_InclusiveEnd_To_ExclusiveEnd`):

```csharp
[Test]
[TestCase("2022-10-31T22:59:59Z", "2022-10-31T23:00:00Z")] // Berlin 23:59:59 + 1s = Berlin 00:00:00 Nov 1
public void Test_Strom_InclusiveEnd_To_ExclusiveEnd_1SecondResolution(
    string dateTimeString,
    string expectedResultString
)
{
    var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
    var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
    var conversion = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromSeconds(1),
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
    };
    dt.Convert(conversion).Should().Be(expected);
}

[Test]
[TestCase("2022-10-31T22:59:59.999Z", "2022-10-31T23:00:00Z")] // Berlin 23:59:59.999 + 1ms = Berlin 00:00:00 Nov 1
public void Test_Strom_InclusiveEnd_To_ExclusiveEnd_1MsResolution(
    string dateTimeString,
    string expectedResultString
)
{
    var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
    var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
    var conversion = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromMilliseconds(1),
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
    };
    dt.Convert(conversion).Should().Be(expected);
}

[Test]
[TestCase("2022-10-31T23:00:00Z", "2022-10-31T22:59:59Z")] // Berlin 00:00:00 Nov 1 - 1s = Berlin 23:59:59 Oct 31
public void Test_Strom_ExclusiveEnd_To_InclusiveEnd_1SecondResolution(
    string dateTimeString,
    string expectedResultString
)
{
    var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
    var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
    var conversion = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromSeconds(1),
            IsGas = false,
        },
    };
    dt.Convert(conversion).Should().Be(expected);
}

[Test]
[TestCase("2022-10-31T23:00:00Z", "2022-10-31T22:59:59.999Z")] // Berlin 00:00:00 Nov 1 - 1ms = Berlin 23:59:59.999 Oct 31
public void Test_Strom_ExclusiveEnd_To_InclusiveEnd_1MsResolution(
    string dateTimeString,
    string expectedResultString
)
{
    var dt = DateTimeOffset.Parse(dateTimeString).UtcDateTime;
    var expected = DateTimeOffset.Parse(expectedResultString).UtcDateTime;
    var conversion = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromMilliseconds(1),
            IsGas = false,
        },
    };
    dt.Convert(conversion).Should().Be(expected);
}
```

- [ ] **Step 4.2: Add `GetInverted()` round-trip test**

Add this test to `DateTimeConversionConfigurationTests.cs`:

```csharp
[Test]
public void Test_GetInverted_Preserves_Resolution()
{
    var original = new DateTimeConversionConfiguration
    {
        Source = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Inclusive,
            Resolution = TimeSpan.FromSeconds(1),
            IsGas = false,
        },
        Target = new DateTimeConfiguration
        {
            IsEndDate = true,
            EndDateTimeKind = EndDateTimeKind.Exclusive,
            IsGas = false,
        },
    };

    var inverted = original.GetInverted();

    inverted.Source.EndDateTimeKind.Should().Be(EndDateTimeKind.Exclusive);
    inverted.Source.Resolution.Should().BeNull();
    inverted.Target.EndDateTimeKind.Should().Be(EndDateTimeKind.Inclusive);
    inverted.Target.Resolution.Should().Be(TimeSpan.FromSeconds(1));
}
```

- [ ] **Step 4.3: Run tests — verify new tests fail**

```
dotnet test
```

Expected: 4 sub-day conversion tests fail (converter still uses `AddGermanDay`, adds a full day instead of 1s/1ms). The `GetInverted` test should pass already (JSON roundtrip preserves all properties). All 81 prior tests remain green.

- [ ] **Step 4.4: Update the converter in `MaKoDateTimeConverter.cs`**

Locate the end-date conversion block (currently around lines 313–331). Replace it with:

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

- [ ] **Step 4.5: Run tests — all green**

```
dotnet test
```

Expected: all tests pass. Final count: 86 tests (81 + 4 sub-day + 1 GetInverted).

- [ ] **Step 4.6: Commit**

```
git add MaKoDateTimeConverter/MaKoDateTimeConverter/MaKoDateTimeConverter.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/MaKoDateTimeConverterTests.cs MaKoDateTimeConverter/MaKoDateTimeConverterTests/DateTimeConversionConfigurationTests.cs
git commit -m "feat: use Resolution for inclusive end-date conversion; support sub-day resolutions"
```

---

## Done

Push and update the draft PR:

```
git push
```

All 86 tests should pass across net9.0 and net10.0. The implementation is complete and ready for final PR review before merging as v2.0.0.
