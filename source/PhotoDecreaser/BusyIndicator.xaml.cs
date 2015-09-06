using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public Boolean IsBusy
        {
            get
            {
                return this.Visibility == Visibility.Collapsed;
            }
            set
            {
                this.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Double PercentCompleted
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

        public void SetCustomMessage( String message )
        {
            textBlock.Text = message;
        }
    }
}
