# RiskDesk

A desktop application for identifying hurricanes that made landfall in Florida using NOAA HURDAT2 track data.

## Purpose

The application parses HURDAT2 storm records, identifies Florida hurricane landfalls since 1900, and presents a report containing the storm name, landfall date, wind speed at landfall, and lifetime peak wind speed.

## Current Scope

- Parse Atlantic HURDAT2 storm and track records.
- Consider events occurring in or after 1900.
- Identify Florida landfalls from storm track geometry.
- Display each Florida landfall as a separate report entry.
- Provide a simple desktop interface for reviewing the results.

## Approach

Landfalls will be inferred by detecting when a storm track crosses from water onto Florida land. The HURDAT2 landfall indicator will not be used to decide whether a landfall occurred.

## Technology

- C# and ASP.NET Core for data parsing, logic, and the API
- TypeScript and Electron for the desktop interface
- NOAA HURDAT2 as the hurricane-track data source

## Working Assumptions

- A storm must be classified as a hurricane when it crosses the Florida coastline.
- Multiple Florida landfalls by the same hurricane are separate events.
- Wind speed at landfall is the primary reported value.
- Lifetime maximum sustained wind is included as supplemental context.