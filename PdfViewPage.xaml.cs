using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PdfViewPage : Page
    {
        const int WrongPassword = unchecked((int)0x8007052b); // HRESULT_FROM_WIN32(ERROR_WRONG_PASSWORD)
        const int GenericFail = unchecked((int)0x80004005);   // E_FAIL


        private IStorageFile? File = null;
        private PdfDocument? pdfDocument = null;

        private BitmapImage[] pageImages = [];

        private uint pageCount = 1;
        private uint currentPageIndex = 0;

        public PdfViewPage()
        {
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (_, file) = ((List<FolderItem> folder, FolderItem file))e.Parameter;
            File = file.Item as IStorageFile;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.ButtonsChanged += OnGamepadButtonChanged;

            try
            {
                pdfDocument = await PdfDocument.LoadFromFileAsync(File);
            }
            catch (Exception ex)
            {
                switch (ex.HResult)
                {
                    case WrongPassword:
//                        rootPage.NotifyUser("Document is password-protected and password is incorrect.", NotifyType.ErrorMessage);
                        break;

                    case GenericFail:
//                        rootPage.NotifyUser("Document is not a valid PDF.", NotifyType.ErrorMessage);
                        break;

                    default:
                        // File I/O errors are reported as exceptions.
//                        rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                        break;
                }
            }
            if (pdfDocument != null)
            {
                pageCount = pdfDocument.PageCount;
                currentPageIndex = 0;
                pageImages = new BitmapImage[pdfDocument.PageCount];
                Output.Source = await GetPageImage(0,pdfDocument);
            }
        }

        private async Task<BitmapImage> GetPageImage(uint index,PdfDocument pdf)
        {
            if (pageImages[index] != null)
                return pageImages[index];

            using PdfPage page = pdf.GetPage(index);

            var stream = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream);
            BitmapImage src = new();
            await src.SetSourceAsync(stream);
            pageImages[index] = src;
            return src;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.ButtonsChanged -= OnGamepadButtonChanged;
        }


        void OnGamepadButtonChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased) e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    if (currentPageIndex > 0)
                        currentPageIndex--;
                    else
                        currentPageIndex = pageCount - 1;
                    if (pdfDocument != null)
                        Output.Source = await GetPageImage(currentPageIndex, pdfDocument);

                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    if (currentPageIndex < pageCount - 1)
                        currentPageIndex++;
                    else
                        currentPageIndex = 0;
                    if (pdfDocument != null)
                        Output.Source = await GetPageImage(currentPageIndex, pdfDocument);
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    Frame.GoBack();
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                {
                }
                if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                {
                }
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
