using Prism.Mvvm;

namespace CustomControls.Models
{
    public class SelectableObject : BindableBase
    {
        private bool _isSelected;

        public SelectableObject(object obj, string displayText)
        {
            Object = obj;

            DisplayText = displayText;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public object Object { get; }

        public string DisplayText { get; }
    }
}