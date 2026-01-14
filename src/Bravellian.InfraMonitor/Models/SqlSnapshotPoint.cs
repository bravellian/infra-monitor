using System;

namespace Bravellian.InfraMonitor.Models;

public sealed record SqlSnapshotPoint(DateTimeOffset CapturedAt, double Value);
