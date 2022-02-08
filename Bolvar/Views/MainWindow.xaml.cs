using System.Collections.ObjectModel;
using System.Windows;

namespace Bolvar.Models
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel(this);
        }
    }
}
