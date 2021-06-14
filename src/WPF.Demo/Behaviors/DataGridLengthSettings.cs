using System.Windows.Controls;

namespace Trading.UI.Demo.Behaviors
{
    public class DataGridLengthSettings
    {
        public double Value { get; set; }

        public DataGridLengthUnitType UnitType { get; set; }

        public double DesiredValue { get; set; }

        public double DisplayValue { get; set; }
    }
}