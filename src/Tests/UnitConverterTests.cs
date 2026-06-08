using System.Globalization;
using AllO.Helpers;
using Xunit;

namespace AllO.Tests;

public class UnitConverterTests
{
    private const double Tol = 1e-9;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0.3048)]
    [InlineData(10, 3.048)]
    [InlineData(-2.5, -0.762)]
    public void ToMeters_ConvierteFeetAMetros(double feet, double expected)
        => Assert.Equal(expected, UnitConverter.ToMeters(feet), Tol);

    [Theory]
    [InlineData(1, 304.8)]
    [InlineData(0.5, 152.4)]
    public void ToMillimeters_ConvierteFeetAMm(double feet, double expected)
        => Assert.Equal(expected, UnitConverter.ToMillimeters(feet), Tol);

    [Theory]
    [InlineData(0)]
    [InlineData(3.5)]
    [InlineData(-12.34)]
    public void Meters_RoundTrip(double feet)
        => Assert.Equal(feet, UnitConverter.ToFeet(UnitConverter.ToMeters(feet)), 1e-9);

    [Theory]
    [InlineData(0)]
    [InlineData(7.7)]
    public void Millimeters_RoundTrip(double feet)
        => Assert.Equal(feet, UnitConverter.ToFeetFromMm(UnitConverter.ToMillimeters(feet)), 1e-9);

    [Fact]
    public void FormatMeters_RespetaDecimales()
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.Equal("0.30", UnitConverter.FormatMeters(1, 2));
            Assert.Equal("3.048", UnitConverter.FormatMeters(10, 3));
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void FormatMillimeters_PorDefectoSinDecimales()
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try { Assert.Equal("305", UnitConverter.FormatMillimeters(1)); }
        finally { CultureInfo.CurrentCulture = prev; }
    }
}
