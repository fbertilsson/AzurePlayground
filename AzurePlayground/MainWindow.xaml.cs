using System.Windows;
using Microsoft.Azure;

namespace AzurePlayground
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private KeyVaultPlayground _keyVaultPlayground;

        public MainWindow()
        {
            InitializeComponent();

            _keyVaultPlayground = new KeyVaultPlayground();
        }

        private void CreateOrUpdateKeyVaultClick(object sender, RoutedEventArgs e)
        {
            _keyVaultPlayground.CreateOrUpdateKeyVault();
        }

        private void CreateSecretClick(object sender, RoutedEventArgs e)
        {
            _keyVaultPlayground.CreateSecret();
        }
    }
}
