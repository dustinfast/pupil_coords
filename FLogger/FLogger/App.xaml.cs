
using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using Windows.ApplicationModel;


namespace FLogger
{
    /// Application-specific behavior supplementing default Application class.
    sealed partial class App : Application
    {
        /// main()
        public App()
        {
            this.InitializeComponent();
        }

        /// Invoked when the application is launched normally by the user. 
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // TODO Test behavior and possibly use to display accuracy
            if (System.Diagnostics.Debugger.IsAttached)
                this.DebugSettings.EnableFrameRateCounter = true;

            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create root frame and navigate to it
                rootFrame = new Frame();
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
                rootFrame.Navigate(typeof(MainPage), e.Arguments);  // Nav to first page if nowhere else to go
            
            Window.Current.Activate(); // Ensure the current window is active
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
