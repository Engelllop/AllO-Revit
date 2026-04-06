namespace AllO.Helpers;

/// <summary>
/// Centralized unit conversion utilities for Revit operations.
/// Revit uses internal units (feet), this helper converts to/from metric.
/// </summary>
public static class UnitConverter
{
    // Conversion constants
    private const double FeetToMeters = 0.3048;
    private const double MetersToFeet = 1.0 / 0.3048;
    private const double FeetToMillimeters = 304.8;
    private const double MillimetersToFeet = 1.0 / 304.8;

    /// <summary>
    /// Converts from Revit internal units (feet) to meters.
    /// </summary>
    public static double ToMeters(double feet) => feet * FeetToMeters;

    /// <summary>
    /// Converts from meters to Revit internal units (feet).
    /// </summary>
    public static double ToFeet(double meters) => meters * MetersToFeet;

    /// <summary>
    /// Converts from Revit internal units (feet) to millimeters.
    /// </summary>
    public static double ToMillimeters(double feet) => feet * FeetToMillimeters;

    /// <summary>
    /// Converts from millimeters to Revit internal units (feet).
    /// </summary>
    public static double ToFeetFromMm(double millimeters) => millimeters * MillimetersToFeet;

    /// <summary>
    /// Formats a length in meters with appropriate precision.
    /// </summary>
    public static string FormatMeters(double feet, int decimals = 3) 
        => ToMeters(feet).ToString($"F{decimals}");

    /// <summary>
    /// Formats a length in millimeters with appropriate precision.
    /// </summary>
    public static string FormatMillimeters(double feet, int decimals = 0) 
        => ToMillimeters(feet).ToString($"F{decimals}");
}
