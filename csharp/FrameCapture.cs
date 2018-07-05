/// GetFrame.cs
//  Gets and returns a frame from the given MadiaCapture and SourceGroup. Handles
//  MediaCapture objects with multiple FrameReaders.
//
//
// Author: Dustin Fast, 2018

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Xaml.Controls;

namespace Flogger
{
    public sealed partial class FrameCapture
    {
        // The Media Capture and source group supplied to constructor.
        MediaCapture _mediaCapture;
        MediaFrameSourceGroup _sourceGroup;

        // For signaling when we have a frame to return
        TaskCompletionSource<bool> _captureDone;

        // Container to hold the frame readers of the given media capture
        private List<MediaFrameReader> _frameReaders = new List<MediaFrameReader>();

        // Public containers to hold captured frames and their image representations. 
        public List<MediaFrameReference> _capturedFrames = new List<MediaFrameReference>();
        public List<Image> _capturedImages = new List<Image>();

        // Number of frames to capture and the current frame count
        private int _maxFrames;
        private int _frameCount;


        // Constructor
        public FrameCapture(MediaCapture mediaCapture, MediaFrameSourceGroup sourceGroup)
        {
            _mediaCapture = mediaCapture;
            _sourceGroup = sourceGroup;
        }

        // Gets and returns a frame
        public async Task GetFramesAsync(int frameCount = 1)
        {
           // Setup num frames to capture
            _frameCount = 0;
            _maxFrames = frameCount;

            // Populate _capturedFrames and _capturedImages with num frames, as available
            _captureDone = new TaskCompletionSource<bool>();    // setup done signal
            await InitFrameCapture();              // Init frame readers
            await _captureDone.Task;                            // await done signal
        }

        /// Sets up the readers and registers for FrameArrived events
        private async Task InitFrameCapture()
        {
            // Set up frame readers and event handlers for each frame source kind
            var startedKinds = new HashSet<MediaFrameSourceKind>();
            foreach (MediaFrameSource source in _mediaCapture.FrameSources.Values)
            {
                MediaFrameSourceKind kind = source.Info.SourceKind;

                if (startedKinds.Contains(kind)) // Ignore duplicate source kinds
                    continue;

                // Look for a format FrameRenderer can render.
                string requestedSubtype = null;
                foreach (MediaFrameFormat format in source.SupportedFormats)
                {
                    requestedSubtype = FrameRender.GetSubtypeForFrameReader(kind, format);
                    if (requestedSubtype != null)
                    {
                        await source.SetFormatAsync(format);  // Tell source to use this format
                        break;
                    }
                }

                // If no acceptable format found, ignore this source
                if (requestedSubtype == null)
                    continue;

                // Init a FrameReader for this kind TODO: Move this to constructor?
                MediaFrameReader frameReader = await _mediaCapture.CreateFrameReaderAsync(source, requestedSubtype);

                frameReader.FrameArrived += FrameArrived;
                _frameReaders.Add(frameReader);

                MediaFrameReaderStartStatus status = await frameReader.StartAsync();
                if (status == MediaFrameReaderStartStatus.Success)
                    throw new System.Exception("Could not initialize frame reader.");
            }

            // At this point, everything is setup and we're waiting on FrameArrived to be fired.
        }


        private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // Returns latest unacquired frame. Null if no such frame or reader not started.
            using (var frame = sender.TryAcquireLatestFrame())
                if (frame != null)
                    _capturedFrames.Add(frame);


            // Increase frame count and if at max, do cleanup and signal done.
            _frameCount += 1;
            if (_frameCount >= _maxFrames)
            {
                CleanupFrameCapture();
                _captureDone?.TrySetResult(true);
            }
        }

        /// Stops frame readers
        private async Task CleanupFrameCapture()
        {
            //Shutdown frame reader
            foreach (var reader in _frameReaders)
                if (_frameReaders != null)
                {
                    reader.FrameArrived -= FrameArrived;
                    await reader.StopAsync();
                    reader.Dispose();
                }
            _frameReaders.Clear();
        }
    };
}
