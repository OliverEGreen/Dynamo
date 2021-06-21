using System;
using System.Windows;
using System.Windows.Controls;

namespace Dynamo.GraphMetadata.Controls
{
    /// <summary>
    /// Interaction logic for RequiredPropertyControl.xaml
    /// </summary>
    public partial class RequiredPropertyControl : UserControl
    {
        public RequiredPropertyControl()
        {
            InitializeComponent();
        }

        public event EventHandler PropertyChanged;

        #region DependencyProperties

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == nameof(RequiredPropertyValue) && e.OldValue != e.NewValue)
            {
                PropertyChanged?.Invoke(this, null);
            }
        }

        public string RequiredPropertyName
        {
            get => (string)GetValue(RequiredPropertyNameProperty);
            set => SetValue(RequiredPropertyNameProperty, value);
        }

        public static readonly DependencyProperty RequiredPropertyNameProperty = DependencyProperty.Register(
            nameof(RequiredPropertyName),
            typeof(string),
            typeof(RequiredPropertyControl)
        );

        public string RequiredPropertyValue
        {
            get => (string)GetValue(RequiredPropertyValueProperty);
            set => SetValue(RequiredPropertyValueProperty, value);
        }

        public static readonly DependencyProperty RequiredPropertyValueProperty = DependencyProperty.Register(
            nameof(RequiredPropertyValue),
            typeof(string),
            typeof(RequiredPropertyControl)
        );

        #endregion
    }
}
