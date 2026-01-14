Metrics UI Razor Class Library

Contains the Razor Pages and supporting services for the metrics dashboard. Host applications reference this project and register the services via `AddBravellianMetricsUi()`.

Basic usage (host app)
- Add project/package reference to `Bravellian.InfraMonitor.Metrics.Ui`.
- Register services:
  - `services.AddBravellianMetricsUi();`
  - `services.AddSingleton<IMetricsSetupStore, YourSetupStore>();`
- Ensure a metrics scrape endpoint exists (for example via `Bravellian.InfraMonitor.Metrics.AspNetCore`).
- Map Razor Pages (`app.MapRazorPages()`).

The host provides an `IMetricsSetupStore` implementation to supply endpoints, refresh interval, and pinned metrics.
