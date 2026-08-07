# Florida Hurricane Landfall Event Tracker

A desktop application for identifying hurricanes that made landfall in Florida using NOAA HURDAT2 track data.

## Purpose

The application parses HURDAT2 storm records, identifies inferred Florida hurricane landfalls from 1900 onward, and presents the results in a desktop interface. Each result includes the storm name, inferred landfall date, interpolated landfall wind, and the highest qualifying hurricane wind found while the storm remains over Florida.

## Current Scope

- Parse Atlantic HURDAT2 storm headers and track observations.
- Process events occurring in or after 1900.
- Infer Florida landfalls from storm-track geometry instead of using HURDAT2's landfall indicator.
- Preserve multiple Florida landfalls from the same storm as separate events.
- Return summary counts and event details through an ASP.NET Core API.
- Display the report in an Electron desktop interface with loading, error, summary, and table states.

## How It Works

1. `HurdatParser` reads the HURDAT2 file into `Storm` and `TrackPoint` objects.
2. `FloridaBoundaryLoader` loads the Census Florida GeoJSON boundary.
3. `FloridaLandfallDetector` examines consecutive track points, finds where an outside track segment first intersects Florida, and interpolates the landfall time and wind.
4. `LandfallAnalysisService` coordinates the pipeline and caches the generated report for later requests.
5. The ASP.NET Core API exposes the report at `GET /api/landfalls`.
6. The Electron renderer fetches the report and displays the results.

## Landfall Rule

The HURDAT2 `L` landfall indicator is intentionally not used. A landfall is inferred when:

- the starting track point is outside Florida;
- the segment between the points intersects the Florida boundary; and
- both observations surrounding the crossing classify the storm as a hurricane.

Events before 1900 are excluded. Multiple crossings by the same storm remain separate report entries.
The interpolated landfall wind is the primary value. The detector also scans later observations while the storm remains over Florida and classified as a hurricane, so a higher inland wind can be reported separately when present.

## Technology

- C# and ASP.NET Core for parsing, business logic, caching, and the API
- NetTopologySuite for spatial geometry operations
- TypeScript, HTML, CSS, and Electron for the desktop interface
- xUnit for automated tests
- NOAA HURDAT2 for hurricane-track data
- U.S. Census cartographic boundary data for the Florida geometry

## Running the Application

From the repository root, start the API:

```powershell
dotnet run --project .\RiskDesk.Api\RiskDesk.Api.csproj
```

In a second terminal, start Electron:

```powershell
cd .\RiskDesk.Desktop
npm start
```

Click **Load Florida Landfalls** in the desktop window.

## Testing

Run the API tests from the repository root:

```powershell
dotnet test .\RiskDesk.Api.Tests\RiskDesk.Api.Tests.csproj
```

Run the Electron lint check:

```powershell
cd .\RiskDesk.Desktop
npm run lint
```

## Working Assumptions and Limitations

- Landfall time and wind are linearly interpolated between the observations surrounding the boundary crossing.
- `LandfallWindSpeedKnots` is the inferred wind where the track segment first intersects Florida.
- `MaxFloridaWindSpeedKnots` is the highest qualifying hurricane wind observed after the landfall crossing while the storm remains over Florida.
- Historical storms may have the name `UNNAMED` in the source data.
- The current analysis loads the dataset on the first request and caches the report in memory for subsequent requests.
