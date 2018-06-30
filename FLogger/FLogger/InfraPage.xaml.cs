using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Runtime.InteropServices;
using Windows.Devices.Perception;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace flogger
{
    [ComImport]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IBufferByteAccess
    {
        unsafe void Buffer(out byte* pByte);
    }
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public sealed partial class BlankPage1 : Page
    {

        public BlankPage1()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }


        async void OnLoaded(object sender, RoutedEventArgs args)
        {
            await InitIrFrame();
        }

        private async System.Threading.Tasks.Task InitIrFrame()
        {
            var access = await PerceptionInfraredFrameSource.RequestAccessAsync();
            if (access == PerceptionFrameSourceAccessStatus.Allowed)
            {
                var possibleSources = await PerceptionInfraredFrameSource.FindAllAsync();

                var firstSource = possibleSources.First();

                this.bitmap = new WriteableBitmap(
                    (int)firstSource.AvailableVideoProfiles.First().Width,
                    (int)firstSource.AvailableVideoProfiles.First().Height);

                this.myImage.Source = this.bitmap;

                this.reader = firstSource.OpenReader();
                this.reader.FrameArrived += HandleFrameArrivedAsync;
            }
        }

        async void HandleFrameArrivedAsync(PerceptionInfraredFrameReader sender,
          PerceptionInfraredFrameArrivedEventArgs args)
        {
            // We move the whole thing to the dispatcher thread for now because we need to  
            // get back to the writeable bitmap and that's got affinity. We could probably  
            // do a lot better here.  
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
              () =>
              {
                  this.HandleFrameArrivedDispatcherThread(args);
              }
            );
        }

        unsafe void HandleFrameArrivedDispatcherThread(PerceptionInfraredFrameArrivedEventArgs args)
        {
            using (var frame = args.TryOpenFrame())
            {
                if (frame != null)
                {
                    unsafe
                    {
                        using (var bufferSource = frame.VideoFrame.SoftwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
                        using (var sourceReference = bufferSource.CreateReference())
                        {
                            var sourceByteAccess = sourceReference as IMemoryBufferByteAccess;
                            var pSourceBits = (byte*)null;
                            uint capacity = 0;
                            sourceByteAccess.GetBuffer(out pSourceBits, out capacity);

                            var destByteAccess = bitmap.PixelBuffer as IBufferByteAccess;
                            var pDestBits = (byte*)null;
                            destByteAccess.Buffer(out pDestBits);

                            var bufferStart = pSourceBits;

                            for (int i = 0; i < (capacity / 2); i++)
                            {
                                // scaling RGB value of 0...Uint16.Max into 0...byte.MaxValue which  
                                // probably isn't the best idea but it's a start.  
                                float pct = (float)(*(UInt16*)pSourceBits / (float)UInt16.MaxValue);
                                byte val = (byte)(pct * byte.MaxValue);

                                *pDestBits++ = val;
                                *pDestBits++ = val;
                                *pDestBits++ = val;
                                *pDestBits++ = 0xFF;

                                pSourceBits += 2; //sizeof(Uint16)  
                            }
                        }
                    }
                    this.bitmap.Invalidate();
                }
            }
        }

        PerceptionInfraredFrameReader reader;
        WriteableBitmap bitmap;

    }
}
