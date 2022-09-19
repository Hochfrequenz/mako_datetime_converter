# mako_datetime_converter

.NET nuget package to convert date times between German "Gastag" and "Stromtag", between inclusive and exclusive end dates and combinations of all them.
This is relevant for German Marktkommunikation ("MaKo").

## Rationale

The German Marktkommunikation ("MaKo") defines some rules regarding date times:

- you shall communicate end dates as exclusive (which is [generally a good idea](https://hf-kklein.github.io/exclusive_end_dates.github.io/))
- you shall use UTC date times with a specified UTC offset (which is a good idea)
- and you shall always use UTC-offset 0 (which makes things unnecessary complicated)
- in electricity all days start and end at midnight of German local time
- but in gas all days start and end at 6am German local time ("Gas-Tag")

Now imagine there is an interface between two systems:

- one of your systems obeys all of the above rules
- but another one works differently (e.g. models end dates inclusively or is unaware of the differences between electricity and gas)

Then you need a conversion logic for your date times.
This library does the conversion for you.

This library does _not_ convert date times to/from UTC.
It expects your application to work with `DateTimeKind.Utc` only, because everything else is doomed to fail and fixing your timezone problems is out of scope for this library.

## How to use this Library

See the [minimal working example](MaKoDateTimeConverter/MaKoDateTimeConverterTests/MinimalWorkingExample.cs).

## Nuget Packages

This repository is available as nuget package:

- [`MaKoDateTimeConverter`](https://www.nuget.org/packages/MaKoDateTimeConverter/) for the core functionalities
  - ![Nuget Package](https://badgen.net/nuget/v/MaKoDateTimeConverter)
  - ![Nuget Prerelease](https://badgen.net/nuget/v/MaKoDateTimeConverter/pre)

## Code Quality / Production Readiness

- The code has [a 100%](https://github.com/Hochfrequenz/mako_datetime_converter/blob/main/.github/workflows/unittests_and_coverage.yml#L34) unit test coverage. ✔️
- The package has no dependencies itself. ✔️

## Release Workflow

To create a **pre-release** nuget package, create a tag of the form `prerelease-vx.y.z` where `x.y.z` is the semantic version of the pre-release.
This will create and push nuget packages with the specified version `x.y.z` and a `-betaYYYYMMDDHHmmss` suffix.

To create a **release** nuget package, create a tag of the form `vx.y.z` where `x.y.z` is the semantic version of the release.
This will create and push nuget packages with the specified version `x.y.z`.
