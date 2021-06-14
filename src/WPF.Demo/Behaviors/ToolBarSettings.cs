using System;

namespace Trading.UI.Demo.Behaviors
{
    public class ToolBarSettings
    {
        #region Properties

        public string Name { get; set; }

        public int BandIndex { get; set; }

        public int Band { get; set; }

        public double Width { get; set; } = double.NaN;

        #endregion Properties

        #region Methods

        public static bool operator !=(ToolBarSettings obj1, ToolBarSettings obj2)
        {
            if (ReferenceEquals(obj1, null))
            {
                return !ReferenceEquals(obj2, null);
            }

            return !obj1.Equals(obj2);
        }

        public static bool operator ==(ToolBarSettings obj1, ToolBarSettings obj2)
        {
            if (ReferenceEquals(obj1, null))
            {
                return ReferenceEquals(obj2, null);
            }

            return obj1.Equals(obj2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ToolBarSettings))
            {
                return false;
            }

            return Equals((ToolBarSettings)obj);
        }

        public bool Equals(ToolBarSettings other)
        {
            if (other == null)
            {
                return false;
            }

            return Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            var hash = 17;

            hash += (hash * 31) + (Name == null ? 0 : Name.GetHashCode());

            return hash;
        }

        #endregion Methods
    }
}