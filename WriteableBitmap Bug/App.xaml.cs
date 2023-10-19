using Microsoft.Maui.Platform;
using System.Diagnostics;
using System.Reflection;

#if WINDOWS
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
#endif
namespace WriteableBitmap_Bug {
    public partial class App : Application {
        Image image;
        public App() {
            InitializeComponent();

            ContentPage mainPage = new();
            mainPage.BackgroundColor = Colors.AliceBlue;
            MainPage = mainPage;

            AbsoluteLayout abs = new();
            mainPage.Content = abs;

            image = new();
            abs.Add(image);

            image.HandlerChanged += delegate {
#if WINDOWS
                bool loadViaWriteable = true;
                if (loadViaWriteable) {
                    addBitmap();
                }
                else {
                    image.Source = ImageSource.FromResource("WriteableBitmap_Bug.Resources.Images.cat.jpg");
                }
#endif
            };

            mainPage.SizeChanged += delegate {
                abs.WidthRequest = mainPage.Width;
                abs.HeightRequest = mainPage.Height;
                image.WidthRequest = mainPage.Width;
                image.HeightRequest = mainPage.Height;
            };
        }
        async Task addBitmap() {
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            //code as per https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.writeablebitmap.pixelbuffer?view=winrt-22621

            using (Stream stream = assembly.GetManifestResourceStream("WriteableBitmap_Bug.Resources.Images.cat.jpg")) {
                using (var randomStream = stream.AsRandomAccessStream()) {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomStream);
                    BitmapTransform transform = new BitmapTransform() {
                    };
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, // WriteableBitmap uses BGRA format 
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation, // This sample ignores Exif orientation 
                        ColorManagementMode.DoNotColorManage
                    );

                    byte[] rawPixelBytes = pixelData.DetachPixelData();

                    uint imgWidth = decoder.PixelWidth;
                    uint imgHeight = decoder.PixelHeight;

                    Debug.WriteLine("PIXEL BUFFER GOT FOR IMAGE " + imgWidth + " " + imgHeight + " LENGTH " + rawPixelBytes.Length + " 0 " + rawPixelBytes[0].ToString());

                    WriteableBitmap wBitmap = new((int)imgWidth, (int)imgHeight);
                    
                    using (Stream wBitmapStream = wBitmap.PixelBuffer.AsStream()) {
                        try {
                            await stream.WriteAsync(rawPixelBytes, 0, rawPixelBytes.Length);
                        }
                        catch (Exception e){
                            Debug.WriteLine(e.Message); // gives error "Stream does not support writing."
                        }
                    }
                
                    var nativeImageView = image.ToPlatform(image.Handler.MauiContext) as Microsoft.UI.Xaml.Controls.Image;
                    nativeImageView.Source = wBitmap;

                }
            }
        }
    }
}