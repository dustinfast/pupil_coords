/// MainPage.xaml.cs
//  The main module for FLogger. 
//  Handles:
//       REPL input/ouput
//       Camera/MediaCapture setup
//
//
// Author: Dustin Fast, 2018

using System;
using System.Linq;
using Windows.System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Sensors;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
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
        // TODO: private raeadonly ProximitySensor _prixSensor = ProximitySensor.GetDefault();

        // State Flags
        private bool _videoStreamOn = false;
        private bool _videoNoStreamOn = false;
        private bool _dataLogOn = false;
        private bool _previewOn = false;

        // Container for aggregated data
        private List<object> _data = new List<object>();

        // Misc
        private MediaFrameSourceKind _camMode = MediaFrameSourceKind.Infrared;  // initial cam mode
        private MediaCapture _mediaCapture;
        private IMediaEncodingProperties _previewProperties;
        private FaceDetectionEffect _faceDetectionEffect;
        private ReadEvalPrintLoop _repl;

        /// Page main()
        public MainPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            // Register handlers for camera init and cleanup on suspend/resume
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;

            // Init REPL
            _repl = new ReadEvalPrintLoop(OutputList);
            _repl.AddCmd("stream", StreamMode);
            _repl.AddCmd("nostream", NoStreamMode);
            _repl.AddCmd("ir", CamMode); 
            _repl.AddCmd("exit", Exit);

            // Inital cmd to run
            _repl.DoCmd("stream on");
        }


        /////////////////////////////////////////////////////////////////////
        /// REPL Commands
        ////////////////////////////////////////////////////////////////////
        
        // The 'stream' command. Args = ( 'on' | 'off')
        // Starts/Stops sensors in streaming mode, for preview window content
        // _videoStreamOn is set here
        private async Task StreamMode(String args)
        {
            if (args == "on")
            {
                if (_videoNoStreamOn)
                    OutputList.Items.Add("ERROR: Cannot start streaming while nostream is active.");
                else if (_videoStreamOn)
                    OutputList.Items.Add("Already streaming... Chill."); 
                else
                {
                    await InitCamAsync();
                    await FaceDetectOnAsync();
                    await StartPreviewAsync();
                    _videoStreamOn = true;
                }
            }
            else if (args == "off")
            {
                if (_videoNoStreamOn)
                    OutputList.Items.Add("ERROR: Nostream mode is currently active.");
                else if (!_videoStreamOn)
                    OutputList.Items.Add("Wasn't streaming... Chill.");
                else
                {
                    await StopPreviewAsync();
                    await FaceDetectOffAsync();
                    await ReleaseCamAsync();
                    _videoStreamOn = false;
                }
            }
            else
                throw new System.InvalidOperationException("ERROR - Invalid argument: " + args);
        }

        // The 'nostream' command. Args = ( 'start' | 'stop')
        // Starts/Stops sensors in nostream mode, for on-demand data with no preview
        // _videoNoStreamOn is set here
        private async Task NoStreamMode(String args)
        {
            if (args == "on")
            {
                if (_videoStreamOn)
                    OutputList.Items.Add("ERROR: Cannot start nostream while streaming is active.");
                else if (_videoNoStreamOn)
                    OutputList.Items.Add("Already in nostream mode... Chill.");
                else
                {
                    await InitCamAsync();
                    await FaceDetectOnAsync();
                    await StartPreviewAsync();
                    _videoNoStreamOn = true;
                }
            }
            else if (args == "off")
            {
                if (_videoStreamOn)
                    OutputList.Items.Add("ERROR: Streaming mode is currently active.");
                else if (!_videoNoStreamOn)
                    OutputList.Items.Add("Wasn't in nostream mode... Chill.");
                else
                {
                    await StopPreviewAsync();
                    await FaceDetectOffAsync();
                    await ReleaseCamAsync();
                    _videoStreamOn = false;
                }
            }
            else
                throw new System.InvalidOperationException("ERROR - Invalid argument: " + args);
        }

        // The 'ir' command.
        // Toggles the camera mode between color and IR mode.
        private async Task CamMode(string args=null)
        {
            // Toggle mode
            if (_camMode != MediaFrameSourceKind.Infrared)
                _camMode = MediaFrameSourceKind.Infrared;
            else
                _camMode = MediaFrameSourceKind.Color;

            // Restart 
            if (_videoStreamOn)
            {
                await StreamMode("off");
                await StreamMode("on");
            }
            else if (_videoNoStreamOn)
            {
                await NoStreamMode("off");
                await NoStreamMode("on");
            }
        }

        private void ShowStatus()
        {
            //TODO: Outputs status to console
        }

        // The 'exit' command. Args = null
        // Does cleanp and exits the application.
        private async Task Exit(String args)
        {
            //Do cleanup and exit
            if (_videoStreamOn)
                await ReleaseCamAsync();
            if (_previewOn)
                await StopPreviewAsync();

            CoreApplication.Exit();
        }

        // REPL command input textbox. On Enter clicked, moves cmd from the
        // input box to the output/history box and sends it for processing.
        private void CmdInputTxt_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                TextBox txtBox = (TextBox)sender;
                String input = txtBox.Text;
                txtBox.Text = "";
                _repl.DoCmd(input);
            }
        }


        /////////////////////////////////////////////////////////////////////
        /// Stream mode handlers
        ////////////////////////////////////////////////////////////////////

        // Setup the camera and init MediaCapture in the desired mode.
        // _videoStreamOn = true on success.
        private async Task InitCamAsync()
        {
            if (_camMode != MediaFrameSourceKind.Color && _camMode != MediaFrameSourceKind.Infrared)
                throw new System.InvalidOperationException("ERROR: An unsupported camera mode was requested.");

            if (_videoStreamOn)
                await ReleaseCamAsync();
            _videoStreamOn = false;

            var frameGroups = await MediaFrameSourceGroup.FindAllAsync();

            // For each source kind, find all sources of the kind we're interested in
            // TODO: Move this out and do it only once.
            var eligibleGroups = frameGroups.Select(g => new
            {
                Group = g,
                SourceInfos = new MediaFrameSourceInfo[]
                {
                    g.SourceInfos.FirstOrDefault(info => info.SourceKind == _camMode)
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
                _videoStreamOn = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
                return;
            }
        }

        // Release camera and MediaCapture
        // _videoStreamOn = false on success.
        private async Task ReleaseCamAsync()
        {
            Debug.WriteLine("ReleaseCamAsync");
            if (_videoStreamOn)
            {
                if (_faceDetectionEffect != null)
                    await FaceDetectOffAsync();

                if (_previewProperties != null)
                    await StopPreviewAsync();
            }

            if (_mediaCapture != null)
                _mediaCapture.Dispose();

            _videoStreamOn = false;
            _mediaCapture = null;

        }

        // Start preview stream from _mediaCapture
        // _previewOn = true on success.
        private async Task StartPreviewAsync()
        {
            PreviewControl.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
            _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            _previewOn = true;
        }

        // Stop preview stream from _mediaCapture
        // _previewOn = false on success.
        private async Task StopPreviewAsync()
        {
            _previewProperties = null;
            await _mediaCapture.StopPreviewAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            { PreviewControl.Source = null; });
            _previewOn = false;
        }


        /////////////////////////////////////////////////////////////////////
        /// Face Detection
        ////////////////////////////////////////////////////////////////////

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


        /// Takes face rectangle in preview coords and returns one in UI coords.
        /// faceBoxInPreviewCoordinates = FaceBox.DetectedFace, in preview coordinates.
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

        /// Calculates size/coords of preview stream rect within the preview control. 
        /// When scaling mode is Uniform:
        ///      prevRes = Resolution at which the preview is running
        ///      prevCtrl = Control displaying the preview(has Uniform as the scaling mode)
        /// # TODO: Adapted from: MS Article
        public Rect GetPreviewStreamRectInControl(VideoEncodingProperties prevRes, CaptureElement prevCtrl)
        {
            var result = new Rect();

            // If not initizalied, return empty result
            if (prevCtrl == null || prevCtrl.ActualHeight < 1 || prevCtrl.ActualWidth < 1 ||
                prevRes == null || prevRes.Height == 0 || prevRes.Width == 0)
                return result;

            // Set result to actual h/w of preview control
            result.Width = prevCtrl.ActualWidth;
            result.Height = prevCtrl.ActualHeight;

            // Adjust result if UI is wider or taller than preview stream
            var streamWidth = prevRes.Width;
            var streamHeight = prevRes.Height;
            if ((prevCtrl.ActualWidth / prevCtrl.ActualHeight > streamWidth / (double)streamHeight))
            {
                // Taller. Letterboxing will be on sides.
                var scale = prevCtrl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;
                result.X = (prevCtrl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else 
            {
                // Wider. Letterboxing will be on top+bottom
                var scale = prevCtrl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;
                result.Y = (prevCtrl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }


        /////////////////////////////////////////////////////////////////////
        /// Utility Functions
        ////////////////////////////////////////////////////////////////////

        /// Gets device orientation (roll, pitch, yaw)
        private InclinometerReading GetOrientation()
        {
            InclinometerReading reading = _inclineSensor.GetCurrentReading();
            String ostr = "Pitch: " + String.Format("{0,5:0.00}", reading.PitchDegrees) + ", " +
                           "Roll: " + String.Format("{0,5:0.00}", reading.RollDegrees) + ", " +
                           "Yaw: " + String.Format("{0,5:0.00}", reading.YawDegrees) + ". " +
                           "Conf: " + reading.YawAccuracy.ToString();

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


        /////////////////////////////////////////////////////////////
        /// Resume/Suspend/NavigateTo/NavigateFrom Handlers
        /// On Suspend, stop active modes. On Resume, restart them.
        /////////////////////////////////////////////////////////////
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            Debug.WriteLine("Suspending...");
            var deferral = e.SuspendingOperation.GetDeferral();
            if (_videoStreamOn)
                await StreamMode("off");
            else if (_videoNoStreamOn)
                await NoStreamMode("off");
            deferral.Complete();
        }
        private async void Application_Resuming(object sender, object o)
        {
            Debug.WriteLine("Resumed from suspend");
            if (_videoStreamOn)
                await StreamMode("on");
            else if (_videoNoStreamOn)
                await NoStreamMode("on");
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Navigated To");
            //if (_videoStreamOn)
            //    await StreamMode("on");
            //else if (_videoNoStreamOn)
            //    await NoStreamMode("on");
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("Navigated From");
            //if (_videoStreamOn)
            //    await StreamMode("off");
            //else if (_videoNoStreamOn)
            //    await NoStreamMode("off");
        }
    }
}

//PreviewFrame
//https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/CameraGetPreviewFrame/cs/MainPage.xaml.cs
//private static readonly SemaphoreSlim _mediaCaptureLifeLock = new SemaphoreSlim(1);
//await _mediaCaptureLifeLock.WaitAsync(); ?
//var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
//var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

//using (var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
//{
//    // Collect the resulting frame and show text on page
//    SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;
//    FrameOutputText.Text = String.Format("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);
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


// /// Saves a photo of the preview frame
// private async Task TakePhotoAsync()
// {
//     // Img save location
//     var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
//     StorageFolder _captureFolder = ApplicationData.Current.LocalFolder;
//     var file = await _captureFolder.CreateFileAsync("PreviewFrame.jpg", CreationCollisionOption.GenerateUniqueName);

//     // TODO: Get frame with overlay instead of without
//     var stream = new InMemoryRandomAccessStream();
//     await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

//     using (var inputStream = stream)
//     {
//         var decoder = await BitmapDecoder.CreateAsync(inputStream);
//         using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
//         {
//             var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
//             var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) } };
//             await encoder.BitmapProperties.SetPropertiesAsync(properties);
//             await encoder.FlushAsync();
//         }
//     }
//     Debug.WriteLine("Photo saved to " + file.Path);
// }




//// Takes a media frame as a bitmp and the face rect as BitmapBounds and Returns
//// a rectangle. If both pupils found, rectangle is pupil x, y, h. Else, rect is 0x0x0
//private void getPupilCoords(Bitmap bmp, BitmapBounds faceBox)
//{
//    System.Drawing.Bitmap aq = (Bitmap)pictureBox1.Image; //take the image

//    // Invert the image, if color?
//    Invert a = new Invert();
//    aq = a.Apply(aq);
//    AForge.Imaging.Image.FormatImage(ref aq);

//    // apply grayscale, if color?
//    IFilter filter = Grayscale.CommonAlgorithms.BT709;
//    aq = filter.Apply(aq);

//    // Change to binary
//    Threshold th = new Threshold(220);
//    aq = th.Apply(aq);

//    // Divide the facebox into four quadrants:
//    // -----------
//    // | q1 | q2 |       
//    // -----------
//    // | q3 | q4 |       
//    // -----------


//    // For q1 and q2, because that's where the eyes are located:

//    // find the biggest object using BlobCounter
//    BlobCounter bl = new BlobCounter(aq);

//    /// If at least one blob, find the pupil start position and height
//    int x, y, h = 0;
//    if (bl.ObjectsCount > 0)
//    {
//        ExtractBiggestBlob fil2 = new ExtractBiggestBlob();
//        fil2.Apply(aq);
//        x = fil2.BlobPosition.X;
//        y = fil2.BlobPosition.Y;
//        h = fil2.Apply(aq).Height;
//    }

//    return new Rectangle(new Point(x - h, y - h), new Size(3 * h, 3 * h));
//}

//public Bitmap CropImage(Bitmap source, Rectangle section)
//{
//    Bitmap bmp = new Bitmap(section.Width, section.Height);
//    Graphics g = Graphics.FromImage(bmp);
//    g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
//    return bmp;
//}