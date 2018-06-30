﻿using System;
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
using Windows.ApplicationModel.Core;
using Windows.Media.Capture.Frames;


namespace FLogger
{
    public sealed partial class MainPage : Page
    {
        // Sensors
        private readonly Inclinometer _inclineSensor = Inclinometer.GetDefault();
        // TODO: private readonly LightSensor _lightSensor = LightSensor.GetDefault();
        // TODO" private raeadonly ProximitySensor _prixSensor = ProximitySensor.GetDefault();

        // Misc
        private MediaCapture _mediaCapture;
        private IMediaEncodingProperties _previewProperties;
        private FaceDetectionEffect _faceDetectionEffect = null;
        private MediaFrameSourceKind _camMode = MediaFrameSourceKind.Infrared;

        // State Flags
        private bool _videoOn = false;
        private bool _dataLogOn = false;
        private bool _previewOn = false;

        // Containers
        private List<object> _data = new List<object>();

        /// Page main()
        public MainPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            // Register handlers for camera init and cleanup on suspend/resume
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
        }


        /// Button click handlers /////

        // IR camera mode toggle
        private async void IRModeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_camMode != MediaFrameSourceKind.Infrared)
            {
                _camMode = MediaFrameSourceKind.Infrared;
                await InitCamAsync(_camMode);
            }
            else
            {
                _camMode = MediaFrameSourceKind.Color;
                await InitCamAsync(_camMode);

            }

            UpdateUIControls();
        }

        // Preview stream toggle
        private async void PreviewStreamToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!_videoOn)
                return;

            // if (_faceDetectionEffect == null || !_faceDetectionEffect.Enabled)
            if (!_previewOn)
            {
                await StartPreviewAsync();
                _previewOn = true;
            }
            else
            {
                await StopPreviewAsync();
                _previewOn = false;
            }

            UpdateUIControls();
        }

        // Frame Capture btn click
        private async void PhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_videoOn)
                return;

            await TakePhotoAsync();
        }

        // Data logging toggle
        private void DataLogToggle_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LoggerBtnClicked");
            if (!_dataLogOn)
                _dataLogOn = true;
            else
                _dataLogOn = false;

            UpdateUIControls();
        }

        // Exit Button
        private async void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            //Do cleanup and exit
            await ReleaseCamAsync();
            await StopPreviewAsync();

            CoreApplication.Exit();
        }


        /// State and Mode toggle handlers /////
         
        // Setup the camera and init MediaCapture in the desired mode
        private async Task InitCamAsync(MediaFrameSourceKind mode)
        {
            if (mode != MediaFrameSourceKind.Color && mode != MediaFrameSourceKind.Infrared)
                throw new System.InvalidOperationException("ERROR: An unsupported camera mode was requested.");

            if (_videoOn)
                await ReleaseCamAsync();

            var frameGroups = await MediaFrameSourceGroup.FindAllAsync();

            // For each source kind, find all sources of the kind we're interested in
            var eligibleGroups = frameGroups.Select(g => new
            {
                Group = g,
                SourceInfos = new MediaFrameSourceInfo[]
                {
                    g.SourceInfos.FirstOrDefault(info => info.SourceKind == mode)
                }
            }).Where(g => g.SourceInfos.Any(info => info != null)).ToList();

            // Error if no cam 
            if (eligibleGroups.Count == 0)
                throw new System.InvalidOperationException("ERROR: Could not find a supported camera.");

            MediaFrameSourceGroup selectedGroup = eligibleGroups[0].Group;

            var settings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = selectedGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(settings);
                _videoOn = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
                return;
            }
        }

        /// Release camera and MediaCapture
        private async Task ReleaseCamAsync()
        {
            Debug.WriteLine("ReleaseCamAsync");
            if (_videoOn)
            {
                if (_faceDetectionEffect != null)
                    await FaceDetectOffAsync();

                if (_previewProperties != null)
                    await StopPreviewAsync();

                _videoOn = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        /// Start preview stream from MediaCapture
        private async Task StartPreviewAsync()
        {
            PreviewControl.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
            _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            Debug.WriteLine("Preview Started.");
        }

        /// Stop preview stream
        private async Task StopPreviewAsync()
        {
            _previewProperties = null;
            await _mediaCapture.StopPreviewAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null; // UI Cleanup
            });
            Debug.WriteLine("Preview Stopped.");
        }


        /// Face detection /////

        /// Called on _faceDetectionEffect.FaceDetected and instructs the UI to add FaceRects to preview overlay
        private async void OnFaceDetectedAsync(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AddFaceRects(args.ResultFrame.DetectedFaces));
        }

        /// Adds Rectangles around the given faces to the PreviewOverlay as a face bounding boxes
        private void AddFaceRects(IReadOnlyList<DetectedFace> faces)
        {
            PreviewOverlay.Children.Clear();  // Remove any previos rects from canvas

            // Iterate faces
            for (int i = 0; i < faces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary
                Rectangle faceBoundingBox = ConvertPreviewToUiRectangle(faces[i].FaceBox);

                // Set bounding box stroke properties and color
                faceBoundingBox.StrokeThickness = 2;
                faceBoundingBox.Stroke = (i == 0 ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.DeepSkyBlue));

                PreviewOverlay.Children.Add(faceBoundingBox);  // Add rects to canvas
            }

            int rotationDegrees = 0;
            var transform = new RotateTransform { Angle = rotationDegrees };
            PreviewOverlay.RenderTransform = transform;

            var previewArea = GetPreviewStreamRectInControl(_previewProperties as VideoEncodingProperties, PreviewControl);

            PreviewOverlay.Width = previewArea.Width;
            PreviewOverlay.Height = previewArea.Height;

            Canvas.SetLeft(PreviewOverlay, previewArea.X);
            Canvas.SetTop(PreviewOverlay, previewArea.Y);
        }

        /// Enables face detection - registers for its events, and gets the FaceDetectionEffect instance
        /// Enables face detection - registers for its events,and gets the FaceDetectionEffect instance
        private async Task FaceDetectOnAsync()
        {
            // Init effect
            var def = new FaceDetectionEffectDefinition();
            def.SynchronousDetectionEnabled = false;                // do not delay incoming samples (for smoother preview)
            def.DetectionMode = FaceDetectionMode.HighPerformance;  // speed over accuracy

            // Add ffect to the preview stream
            _faceDetectionEffect = (FaceDetectionEffect)await _mediaCapture.AddVideoEffectAsync(def, MediaStreamType.VideoPreview);
            // Register for face detection events
            _faceDetectionEffect.FaceDetected += OnFaceDetectedAsync;
            // Choose shortest interval between detection events
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);
            // Start detecting faces
            _faceDetectionEffect.Enabled = true;
        }

        ///  Disables and removes the face detection effect, and unregisters the event handler for face detection
        private async Task FaceDetectOffAsync()
        {
            // Disable detection
            _faceDetectionEffect.Enabled = false;
            // Unregister the event handler
            _faceDetectionEffect.FaceDetected -= OnFaceDetectedAsync;
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


        /// Utility Functions /////

        /// Gets device orientation (roll, pitch, yaw)
        private InclinometerReading GetOrientation()
        {
            InclinometerReading reading = _inclineSensor.GetCurrentReading();
            String ostr = "Pitch: " + String.Format("{0,5:0.00}", reading.PitchDegrees) + ", " +
                           "Roll: " + String.Format("{0,5:0.00}", reading.RollDegrees) + ", " +
                           "Yaw: " + String.Format("{0,5:0.00}", reading.YawDegrees) + ". " +
                           "Conf: " + reading.YawAccuracy.ToString();
            Debug.WriteLine(ostr);

            // // Calibrate Magnetometer if no confidence
            // var acc = _inclineSensor.GetCurrentReading().YawAccuracy;
            // Debug.WriteLine("Mag Accuracy: " + acc.ToString());
            // if (acc != MagnetometerAccuracy.High)
            // {
            //     CalibrationBar calibrationBar = new CalibrationBar();
            //     calibrationBar.RequestCalibration(MagnetometerAccuracy.Unreliable);  // TODO: Not working
            //     calibrationBar.Hide();
            //     acc = _inclineSensor.GetCurrentReading().YawAccuracy;
            //     Debug.WriteLine("New Mag Accuracy: " + acc.ToString());
            // }

            return reading;
        }
        
        /// Saves a photo of the preview frame
        private async Task TakePhotoAsync()
        {
            // Img save location
            var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            StorageFolder _captureFolder = ApplicationData.Current.LocalFolder;
            var file = await _captureFolder.CreateFileAsync("PreviewFrame.jpg", CreationCollisionOption.GenerateUniqueName);

            // TODO: Get frame with overlay instead of without
            var stream = new InMemoryRandomAccessStream();
            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);
                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) } };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }
            Debug.WriteLine("Photo saved to " + file.Path);


            //BMP
            //https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/CameraGetPreviewFrame/cs/MainPage.xaml.cs
            //private static readonly SemaphoreSlim _mediaCaptureLifeLock = new SemaphoreSlim(1);
            //await _mediaCaptureLifeLock.WaitAsync(); ?
            //var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            //var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            //using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
            //{
            //    // Collect the resulting frame and show text on page
            //    SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;
            //    FrameInfoTextBlock.Text = String.Format("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);
            //    // Create a SoftwareBitmapSource to display the SoftwareBitmap to the user
            //    var sbSource = new SoftwareBitmapSource();
            //    await sbSource.SetBitmapAsync(previewFrame);
            //    PreviewFrameImage.Source = sbSource;

            //    using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            //    {
            //        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);

            //        // Grab the data from the SoftwareBitmap
            //        encoder.SetSoftwareBitmap(previewFrame);
            //        await encoder.FlushAsync();
            //    }
            //}
            //await _mediaCaptureLifeLock.Release(); ?
        }

        /// Get Preview Frame as a 3d Surface
        private async Task GetPreviewFrameAsD3DSurfaceAsync()
        {
            // Capture the preview frame as a D3D surface
            using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync())
            {
                // Check that the Direct3DSurface isn't null. It's possible that the device does not support getting the frame
                // as a D3D surface
                if (currentFrame.Direct3DSurface != null)
                {
                    // Collect the resulting frame
                    var surface = currentFrame.Direct3DSurface;

                    // Show the frame information
                    InfoTextBlock.Text = String.Format("{0}x{1} {2}", surface.Description.Width, surface.Description.Height, surface.Description.Format);
                    Debug.WriteLine("Got 3D Frame");
                }
                else // Fall back to software bitmap
                {
                    Debug.WriteLine("Could not get 3D Frame");

                    SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;
                    InfoTextBlock.Text = String.Format("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);
                }
               
                // Clear the image
                PreviewControl.Source = null;
            }
        }

        private void UpdateUIControls()
        {
            // Clear everything from the preview overlay
            PreviewOverlay.Children.Clear();

            // Enabled/disabled buttons based on capabilities 
            //PhotoButton.IsEnabled = _previewProperties != null;

            // Update toggle icons based on current mode
            //PreviewStreamOnIcon.Visibility = (_faceDetectionEffect == null || !_faceDetectionEffect.Enabled) ? Visibility.Visible : Visibility.Collapsed;
            //PreviewStreamOffIcon.Visibility = (_faceDetectionEffect != null && _faceDetectionEffect.Enabled) ? Visibility.Visible : Visibility.Collapsed;
            ////StartRecordingIcon.Visibility = _isRecording ? Visibility.Collapsed : Visibility.Visible;
            //StopRecordingIcon.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;

            // Hide the face detection canvas and clear it
            //FacesCanvas.Visibility = (_faceDetectionEffect != null && _faceDetectionEffect.Enabled) ? Visibility.Visible : Visibility.Collapsed;

            //PhotoButton.Opacity = PhotoButton.IsEnabled ? 1 : 0;
        }

        ///////////////////////////////
        /// Resume/Suspend Helpers
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                Debug.WriteLine("Suspending...");
                var deferral = e.SuspendingOperation.GetDeferral();
                await ReleaseCamAsync();
                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                await InitCamAsync(_camMode);
                Debug.WriteLine("Resumed from suspend");
            }
        }

        /////////////////////////////////
        /// Navigation Helpers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Navigated To");
            await InitCamAsync(_camMode);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("Navigated From");
            await ReleaseCamAsync();
        }

        
    }
}


///// In the event of the app being minimized, handles media property change events. If the app receives a mute
///// notification, it is no longer in the foregroud.
// Use:
//private SystemMediaTransportControls _systemMediaControls = null;
//_systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
//
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
//                await ReleaseCamAsync();
//            }
//            else if (!_videoOn)
//            {
//                await InitSensorsAsync();
//            }
//        }
//    });
//}
