using Avalonia.Controls;

namespace Omnius.Lxna.Ui.Desktop.Helpers;

public static class ConvertHelper
{
    public static GridLength DoubleToGridLength(double value)
    {
        return new GridLength(value);
    }

    public static double GridLengthToDouble(GridLength gridLength)
    {
        return gridLength.Value;
    }
}
