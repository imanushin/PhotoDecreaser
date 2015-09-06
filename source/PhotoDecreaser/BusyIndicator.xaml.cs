using System.Windows;
using System.Windows.Controls;

namespace PhotoDecreaser
{
    /// <summary>
    /// Interaction logic for BusyIndicator.xaml
    /// </summary>
    public partial class BusyIndicator : UserControl
    {
        public BusyIndicator()
        {
            InitializeComponent();

            Canvas.SetZIndex( this, 1 );
        }

        public bool IsBusy
        {
            get
            {
                return Visibility == Visibility.Collapsed;
            }
            set
            {
                Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public double PercentCompleted
        {
            get
            {
                return progressBar.Value;
            }
            set
            {
                progressBar.Value = value;
                textBlock.Text = "Пожалуйста, ждите...";
            }
        }

        public void SetCustomMessage( string message )
        {
            textBlock.Text = message;
        }
    }
}
