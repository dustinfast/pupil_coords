
using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using Windows.ApplicationModel;


namespace flogger
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
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }

            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
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
