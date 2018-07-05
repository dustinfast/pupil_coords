using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using Windows.Media.FaceAnalysis;
using Windows.UI;

namespace FLogger
{
    class FrameStream
    {
        // MediaCapture
        private MediaCapture _mediaCapture;
        private MediaCaptureInitializationSettings _mediaCaptureSettings;

        // Preview and FaceDetect instances
        private IMediaEncodingProperties _previewProperties;
        private FaceDetectionEffect _faceDetectionEffect;

        //UI elements and dispatcher
        private CoreDispatcher _dispatcher;
        private CaptureElement _captureElement;
        private Canvas _previewOverlay;

        // State
        private bool _streamOn = false;

        // Constructor
        public FrameStream(MediaCaptureInitializationSettings captureSettings, CaptureElement captureElement, Canvas previewOverlay)
        {
            _mediaCaptureSettings = captureSettings;
            _captureElement = captureElement;
            _previewOverlay = previewOverlay;
            _dispatcher = _captureElement.Dispatcher;
        }

        // Start preview stream from _mediaCapture
        // _steamOn = true on success.
        public async Task StartStreamAsync()
        {
            if (_streamOn)
                await StopStreamAsync();

            await InitCamAsync();
            await FaceDetectOnAsync();

            _captureElement.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
            _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            _streamOn = true;
        }

        // Stop preview stream and release resources
        // _steamOn = false on success.
        public async Task StopStreamAsync()
        {
            if (_streamOn)
            {
                await FaceDetectOffAsync();

                _previewProperties = null;
                await _mediaCapture.StopPreviewAsync();

                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                { _captureElement.Source = null; });

                _mediaCapture.Dispose();
                _mediaCapture = null;

                _streamOn = false;
            }
        }

        // Init MediaCapture in with the given settings
        // _streamOn = true on success.
        private async Task InitCamAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(_mediaCaptureSettings);
                _streamOn = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MediaCapture initialization failed: " + ex.Message);
                _streamOn = false;
            }
        }


        //TODO: Returns a frame 
        public void GetCurrentFrame()
        {
        }


        /////////////////////////////////////////////////////////////////////
        /// Face Detection
        /// From https://github.com/Microsoft/Windows-universal-samples
        ////////////////////////////////////////////////////////////////////

        /// Called on _faceDetectionEffect.FaceDetected and instructs the UI to add FaceRects to preview overlay
        private async void OnFaceDetectedAsync(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AddFaceRects(args.ResultFrame.DetectedFaces));
        }

        /// Adds Rectangles around the given faces to the _previewOverlay as a face bounding boxes
        private void AddFaceRects(IReadOnlyList<DetectedFace> faces)
        {
            _previewOverlay.Children.Clear();  // Remove any previos rects from canvas

            // Iterate faces
            for (int i = 0; i < faces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary
                Rectangle faceBoundingBox = ConvertPreviewToUiRectangle(faces[i].FaceBox);

                // Set bounding box stroke properties and color
                faceBoundingBox.StrokeThickness = 2;
                faceBoundingBox.Stroke = (i == 0 ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.DeepSkyBlue));

                _previewOverlay.Children.Add(faceBoundingBox);  // Add rects to canvas
            }

            int rotationDegrees = 0;
            var transform = new RotateTransform { Angle = rotationDegrees };
            _previewOverlay.RenderTransform = transform;

            var previewArea = GetPreviewStreamRectInControl(_previewProperties as VideoEncodingProperties, _captureElement);

            _previewOverlay.Width = previewArea.Width;
            _previewOverlay.Height = previewArea.Height;

            Canvas.SetLeft(_previewOverlay, previewArea.X);
            Canvas.SetTop(_previewOverlay, previewArea.Y);
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
            var previewInUI = GetPreviewStreamRectInControl(previewStream, _captureElement);

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

    }
}
