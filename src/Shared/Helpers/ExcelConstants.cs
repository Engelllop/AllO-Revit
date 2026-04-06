namespace AllO.Helpers;

/// <summary>
/// Excel interop constants for horizontal alignment.
/// These magic numbers come from the Excel XlHAlign enum.
/// </summary>
public enum ExcelHAlign
{
    Left = -4131,
    Center = -4108,
    Right = -4152,
    Fill = -4125,
    Justify = -4130,
    CenterAcrossSelection = -4105,
    Distributed = -4119
}

/// <summary>
/// Excel interop constants for vertical alignment.
/// These magic numbers come from the Excel XlVAlign enum.
/// </summary>
public enum ExcelVAlign
{
    Top = -4160,
    Bottom = -4107,
    Center = -4108,
    Justify = -4130,
    Distributed = -4119
}
