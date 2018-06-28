using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using Windows.Media.FaceAnalysis;
using Windows.UI;


namespace flogger
{
    public sealed partial class MainPage : Page
    {
        // Device and display orientation 
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        // MediaCapture and camera
        private MediaCapture _mediaCapture;
        private IMediaEncodingProperties _previewProperties;
        private bool _isInitialized = false;

        // Face detection and data logging
        private FaceDetectionEffect _faceDetectionEffect;
        private StorageFolder _captureFolder = null;
        private bool _loggingActive = false;


        /// Page main()
        public MainPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;  // Disable UI cahching on suspend/navigate

            // Register handlers for camera init/cleanup on suspend/resume
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
        }

        /// Init UI
        private async Task SetupUiAsync()
        {
            // Lock page to landscape orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            // Get current display orientation 
            _displayOrientation = _displayInformation.CurrentOrientation;
            if (_orientationSensor != null)
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();

            // Register for orientation updates # TODO: Device vs Display orientation?
            if (_orientationSensor != null)
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;

            // Init img save location
            var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;
        }

        /// Clean up UI
        private void CleanupUiAsync()
        {
            // Unregister Event Handlers
            if (_orientationSensor != null)
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;  // Unlock screen orientation
        }

        /// Init MediaCapture, get webcam, start preview, and start face detecting
        private async Task InitializeCameraAsync()
        {
            Debug.WriteLine("Initializing Camera.");
            if (_mediaCapture == null)
            {
                // Init camera, favoring the front panel cam
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var cameraDevice = allVideoDevices.FirstOrDefault();

                if (cameraDevice == null)
                    throw new System.InvalidOperationException("Application requires a webcam but none found.");

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Init MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    throw new System.InvalidOperationException("Application requires a webcam but was denied access.");
                }

                Debug.WriteLine("Preview Started.");
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
                FaceDetectionButton_Click(null, null);  // Start face detection
            }
        }

        /// Stops the preview stream
        private async Task StopPreviewAsync()
        {
            // Stop the preview
            _previewProperties = null;
            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null; // UI Cleanup
            });
        }

        /// Takes a photo to a StorageFile and adds rotation metadata to it
        private async Task TakePhotoAsync()
        {
            var stream = new InMemoryRandomAccessStream();

            Debug.WriteLine("Taking photo...");
            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            try
            {
                var file = await _captureFolder.CreateFileAsync("imgcap.jpg", CreationCollisionOption.GenerateUniqueName);
                await ReencodeAndSavePhotoAsync(stream, file, PhotoOrientation.Normal);
                Debug.WriteLine("Photo saved to " + file.Path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception writing img: " + ex.ToString());
            }
        }

        /// Cleans up the camera resources
        private async Task CleanupCameraAsync()
        {
            Debug.WriteLine("CleanupCameraAsync");

            if (_isInitialized)
            {
                if (_faceDetectionEffect != null)
                    await CleanUpFaceDetectionEffectAsync();

                if (_previewProperties != null)
                    await StopPreviewAsync();

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        /// Occurs each time the simple orientation sensor reports a new sensor reading.
        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            Debug.WriteLine("Orientation changed");
            _deviceOrientation = args.Orientation;
        }

        /// Saves the given photo stream to the given StorageFile
        private static async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, StorageFile file, PhotoOrientation photoOrientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(photoOrientation, PropertyType.UInt16) } };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }
        }

        /////////////////////////////////////////////        
        //Face detection.

        /// Ask the UI thread to render the face bounding boxes.
        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFaces(args.ResultFrame.DetectedFaces));
        }

        /// Adds Rectangles around the given faces to the FacesCanvas as a face bounding boxes
        private void HighlightDetectedFaces(IReadOnlyList<DetectedFace> faces)
        {
            FacesCanvas.Children.Clear();  // Remove any previos rects from canvas

            // Iterate faces
            for (int i = 0; i < faces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary
                Rectangle faceBoundingBox = ConvertPreviewToUiRectangle(faces[i].FaceBox);

                // Set bounding box stroke properties and color
                faceBoundingBox.StrokeThickness = 2;
                faceBoundingBox.Stroke = (i == 0 ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.DeepSkyBlue));

                FacesCanvas.Children.Add(faceBoundingBox);  // Add rects to canvas
            }

            /// Rotate face rects as necessary, according to display orientation.
            int rotationDegrees = 0;
            switch (_displayOrientation)
            {
                case DisplayOrientations.Portrait:
                    rotationDegrees = 90;
                    break;
                case DisplayOrientations.LandscapeFlipped:
                    rotationDegrees = 180;
                    break;
                case DisplayOrientations.PortraitFlipped:
                    rotationDegrees = 270;
                    break;
                default:
                    rotationDegrees = 0;
                    break;
            }

            var transform = new RotateTransform { Angle = rotationDegrees };
            FacesCanvas.RenderTransform = transform;

            var previewArea = GetPreviewStreamRectInControl(_previewProperties as VideoEncodingProperties, PreviewControl);

            FacesCanvas.Width = previewArea.Width;
            FacesCanvas.Height = previewArea.Height;

            Canvas.SetLeft(FacesCanvas, previewArea.X);
            Canvas.SetTop(FacesCanvas, previewArea.Y);
        }

        /// Adds face detection to the preview stream, registers for its events, enables it, and gets the FaceDetectionEffect instance
        private async Task CreateFaceDetectionEffectAsync()
        {
            // Create the definition, which will contain some initialization settings
            var definition = new FaceDetectionEffectDefinition();

            // To ensure preview smoothness, do not delay incoming samples
            definition.SynchronousDetectionEnabled = false;

            // In this scenario, choose detection speed over accuracy
            definition.DetectionMode = FaceDetectionMode.HighPerformance;

            // Add the effect to the preview stream
            _faceDetectionEffect = (FaceDetectionEffect)await _mediaCapture.AddVideoEffectAsync(definition, MediaStreamType.VideoPreview);

            // Register for face detection events
            _faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;

            // Choose the shortest interval between detection events
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);

            // Start detecting faces
            _faceDetectionEffect.Enabled = true;
        }

        ///  Disables and removes the face detection effect, and unregisters the event handler for face detection
        private async Task CleanUpFaceDetectionEffectAsync()
        {
            // Disable detection
            _faceDetectionEffect.Enabled = false;

            // Unregister the event handler
            _faceDetectionEffect.FaceDetected -= FaceDetectionEffect_FaceDetected;

            // Remove the effect (see ClearEffectsAsync method to remove all effects from a stream)
            await _mediaCapture.RemoveEffectAsync(_faceDetectionEffect);

            // Clear the member variable that held the effect instance
            _faceDetectionEffect = null;
        }


        /// Takes face information defined in preview coordinates and returns one in UI coordinates, taking
        /// into account the position and size of the preview control.
        /// faceBoxInPreviewCoordinates = Face coordinates as retried from the FaceBox property of a DetectedFace, in preview coordinates.
        /// Rectangle in UI (CaptureElement) coordinates, to be used in a Canvas control.
        private Rectangle ConvertPreviewToUiRectangle(BitmapBounds faceBoxInPreviewCoordinates)
        {
            var result = new Rectangle();
            var previewStream = _previewProperties as VideoEncodingProperties;

            // If no preview info, or zero h or w, return empty rectangle
            if (previewStream == null || previewStream.Width == 0 || previewStream.Height == 0) return result;

            double streamWidth = previewStream.Width;
            double streamHeight = previewStream.Height;

            // Get the rectangle occupied by the actual video feed
            var previewInUI = GetPreviewStreamRectInControl(previewStream, PreviewControl);

            // Scale the width and height from preview stream coordinates to window coordinates
            result.Width = (faceBoxInPreviewCoordinates.Width / streamWidth) * previewInUI.Width;
            result.Height = (faceBoxInPreviewCoordinates.Height / streamHeight) * previewInUI.Height;

            // Scale the X and Y coordinates from preview stream coordinates to window coordinates
            var x = (faceBoxInPreviewCoordinates.X / streamWidth) * previewInUI.Width;
            var y = (faceBoxInPreviewCoordinates.Y / streamHeight) * previewInUI.Height;
            Canvas.SetLeft(result, x);
            Canvas.SetTop(result, y);

            return result;
        }

        
        /// Calculates the size and location of the rectangle that contains the preview stream within the preview control, when the scaling mode is Uniform
        /// previewResolution = The resolution at which the preview is running
        /// previewControl = The control that is displaying the preview using Uniform as the scaling mode
        public Rect GetPreviewStreamRectInControl(VideoEncodingProperties previewResolution, CaptureElement previewControl)
        {
            var result = new Rect();

            // In case this function is called before everything is initialized correctly, return an empty result
            if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
                previewResolution == null || previewResolution.Height == 0 || previewResolution.Width == 0)
            {
                return result;
            }

            var streamWidth = previewResolution.Width;
            var streamHeight = previewResolution.Height;

            // Start by assuming the preview display spans the entire width and height 
            result.Width = previewControl.ActualWidth;
            result.Height = previewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = previewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;

                result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = previewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;

                result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }

        /////////////////////////
        /// Btn click handlers
        private async void PhotoButton_Click(object sender, RoutedEventArgs e)
        {
            await TakePhotoAsync();
            Debug.WriteLine("PhotoBtnClicked");
        }

        // Start or stop data logging
        private void LoggerButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LoggerBtnClicked");
            if (_loggingActive == false)
                _loggingActive = true;
            else
                _loggingActive = false;
        }

        // Start or stop face detection
        private async void FaceDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_faceDetectionEffect == null || !_faceDetectionEffect.Enabled)
                await CreateFaceDetectionEffectAsync();
            else
                await CleanUpFaceDetectionEffectAsync();
            FacesCanvas.Children.Clear();  // Clear any existing face rects
        }

        ///////////////////////////////
        /// Resume/Suspend Helpers
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                CleanupUiAsync();
                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                await SetupUiAsync();
                await InitializeCameraAsync();
            }
        }

        /////////////////////////////////
        /// Navigation Helpers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Navigated To");
            await SetupUiAsync();
            await InitializeCameraAsync();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("Navigated From");
            await CleanupCameraAsync();
            CleanupUiAsync();
        }
    }
}


///// In the event of the app being minimized, handles media property change events. If the app receives a mute
///// notification, it is no longer in the foregroud.
//private async void SystemMediaControls_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
//{
//    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
//    {
//         // Only handle this event if this page is currently being displayed
//         if (args.Property == SystemMediaTransportControlsProperty.SoundLevel && Frame.CurrentSourcePageType == typeof(MainPage))
//        {
//             // Check to see if the app is being muted. If so, it is being minimized.
//             // Otherwise if it is not initialized, it is being brought into focus.
//             if (sender.SoundLevel == SoundLevel.Muted)
//            {
//                await CleanupCameraAsync();
//            }
//            else if (!_isInitialized)
//            {
//                await InitializeCameraAsync();
//            }
//        }
//    });
//}
