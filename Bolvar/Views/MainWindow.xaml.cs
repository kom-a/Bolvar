using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Bolvar.Models
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_AutoScroll = true;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel(this);
        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (ConsoleScrollViewer.VerticalOffset == ConsoleScrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    m_AutoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    m_AutoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (m_AutoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                ConsoleScrollViewer.ScrollToVerticalOffset(ConsoleScrollViewer.ExtentHeight);
            }
        }
    }
}
