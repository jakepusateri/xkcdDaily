using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Xkcd_Reader
{
    public sealed partial class SettingsFlyout : UserControl
    {
        public SettingsFlyout()
        {
            this.InitializeComponent();
            SettingsPane.GetForCurrentView().CommandsRequested += CommandsRequested;

        }
        private void CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(new SettingsCommand("a", "About", (p) => { About.IsOpen = true; }));
            args.Request.ApplicationCommands.Add(new SettingsCommand("privacyPref", "Privacy Policy", async (uiCommand) =>
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("http://jakepusateri.azurewebsites.net/xkcddaily-privacy-policy/"));
            }));

        }

        private async void OnMailTo(object sender, RoutedEventArgs args)
        {
            var hyperlinkButton = sender as HyperlinkButton;
            if (hyperlinkButton != null)
            {
                var uri = new Uri("mailto:" + hyperlinkButton.Content);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        

        private async void HyperlinkButton_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("http://www.xkcd.com"));
        }
        private async void HyperlinkButton_Tapped_2(object sender, TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("http://store.xkcd.com"));
        }
    }
}
