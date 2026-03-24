using System.Windows;

namespace CSharpSpeedRush
{
    /// <summary>
    /// The entry point of the WPF application.
    /// WPF automatically looks for this class and calls its constructor
    /// which in turn opens the StartupUri window (MainWindow).
    /// </summary>
    public partial class App : Application
    {
        // Nothing needed here – WPF handles startup automatically via App.xaml StartupUri
    }
}
