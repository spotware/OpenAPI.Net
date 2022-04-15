using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Trading.UI.Sample.Controls
{
    /// <summary>
    /// Interaction logic for RectangleTextBlock.xaml
    /// </summary>
    public partial class RectangleTextBlock : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty TextProperty
            = DependencyProperty.Register(
                "Text",
                typeof(string),
                typeof(RectangleTextBlock),
                new PropertyMetadata(string.Empty)
            );

        public static readonly DependencyProperty BackgroundColorProperty
        = DependencyProperty.Register(
          "BackgroundColor",
          typeof(Brush),
          typeof(RectangleTextBlock),
          new PropertyMetadata(new SolidColorBrush(Colors.Transparent))
        );

        public static readonly DependencyProperty TextHorizontalAlignmentProperty
        = DependencyProperty.Register(
          "TextHorizontalAlignment",
          typeof(HorizontalAlignment),
          typeof(RectangleTextBlock),
          new PropertyMetadata(HorizontalAlignment.Center)
        );

        public static readonly DependencyProperty TextVerticalAlignmentProperty
        = DependencyProperty.Register(
          "TextVerticalAlignment",
          typeof(VerticalAlignment),
          typeof(RectangleTextBlock),
          new PropertyMetadata(VerticalAlignment.Center)
        );

        #endregion Dependency Properties

        #region Constructor

        public RectangleTextBlock()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Properties

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public HorizontalAlignment TextHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(TextHorizontalAlignmentProperty);
            set => SetValue(TextHorizontalAlignmentProperty, value);
        }

        public VerticalAlignment TextVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(TextVerticalAlignmentProperty);
            set => SetValue(TextVerticalAlignmentProperty, value);
        }

        #endregion Properties
    }
}