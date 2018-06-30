using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace FLogger
{
    class ToastUser
    {
        public enum ToastType
        {
            StatusMessage,
            WarnMessage,
            ErrorMessage,
        }

        public struct Toast
        {
            private string Message;
            private string Style;
        }

        /// Calls caller.ShowToast(Toast)
        public void DoToast(string msg, ToastType type, object caller)
        {
            Style style = null;

            switch (type)
            {
                case ToastType.StatusMessage:
                    //style = Resources["StatusStyle"] as Style;
                    //caller.InfoTextBlock.StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                    // ...
            }

            //caller.DoToast(Toast(msg, style));
        }
    }
}
