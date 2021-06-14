using System;
using System.ComponentModel;
using System.Windows;

namespace Trading.UI.Demo.Behaviors
{
    public class DataGridColumnSettings
    {
        #region Properties

        public string Header { get; set; }

        public int DisplayIndex { get; set; }

        public DataGridLengthSettings Width { get; set; }

        public ListSortDirection? SortDirection { get; set; }

        public Visibility Visibility { get; set; }

        #endregion Properties

        #region Methods

        public static bool operator !=(DataGridColumnSettings obj1, DataGridColumnSettings obj2)
        {
            if (ReferenceEquals(obj1, null))
            {
                return !ReferenceEquals(obj2, null);
            }

            return !obj1.Equals(obj2);
        }

        public static bool operator ==(DataGridColumnSettings obj1, DataGridColumnSettings obj2)
        {
            if (ReferenceEquals(obj1, null))
            {
                return ReferenceEquals(obj2, null);
            }

            return obj1.Equals(obj2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DataGridColumnSettings))
            {
                return false;
            }

            return Equals((DataGridColumnSettings)obj);
        }

        public bool Equals(DataGridColumnSettings other)
        {
            if (other == null)
            {
                return false;
            }

            return Header.Equals(other.Header, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            var hash = 17;

            hash += (hash * 31) + (Header == null ? 0 : Header.GetHashCode());

            return hash;
        }

        #endregion Methods
    }
}