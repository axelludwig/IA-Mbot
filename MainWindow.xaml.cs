using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Threading;
using System.Windows.Interop;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;


namespace IA_Mbot
{
    public partial class MainWindow : Window
    {
        public class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
        }

        // Import necessary functions from User32.dll
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetShellWindow();

        // Delegate to filter which windows to include 
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SRCCOPY = 0x00CC0020;

        private Dictionary<string, IntPtr> windowHandles = new Dictionary<string, IntPtr>();

        private IntPtr selectedWindowHandle;

        private DispatcherTimer timer;
        private bool isTimerRunning;



        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        public MainWindow()
        {
            InitializeComponent();
            LoadOpenWindows();

            // Initialize the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50); // 10 times a second
            timer.Tick += Timer_Tick;

            isTimerRunning = false;

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Code to execute on each tick
            Console.WriteLine("Timer ticked at " + DateTime.Now);

            UpdateScreenshot();
        }

        private void LoadOpenWindows()
        {
            List<WindowInfo> windows = GetOpenWindows();

            foreach (var window in windows)
            {
                comboBoxWindows.Items.Add(window.Title);
                windowHandles[window.Title] = window.Handle;
            }

            if (comboBoxWindows.Items.Count > 0)
            {
                comboBoxWindows.SelectedIndex = 0; // Select the first item by default
            }
        }

        private static List<WindowInfo> GetOpenWindows()
        {
            IntPtr shellWindow = GetShellWindow();
            List<WindowInfo> windows = new List<WindowInfo>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

                windows.Add(new WindowInfo { Handle = hWnd, Title = builder.ToString() });
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private Bitmap CaptureWindow(IntPtr hwnd)
        {
            GetWindowRect(hwnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Release the HBitmap to avoid memory leaks
            DeleteObject(hBitmap);

            return bitmapSource;
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private void UpdateScreenshot()
        {
            // Get the handle of the window to capture (e.g., the currently active window)
            IntPtr hwnd = selectedWindowHandle;
            // IntPtr hwnd = selectedWindowHandle; // Use this if you have a specific window handle

            // Capture the screenshot
            Bitmap screenshot = CaptureWindow(hwnd);

            try
            {
                imageScreenshot.Source = BitmapToImageSource(screenshot);
            }
            catch (ExternalException ex)
            {
                Console.WriteLine($"An error occurred while displaying the screenshot: {ex.Message}");
            }
            finally
            {
                // Dispose of the bitmap to free resources
                screenshot.Dispose();
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the timer on and off and call UpdateScreenshot
            if (isTimerRunning)
            {
                timer.Stop();
                StartStopButton.Content = "Start Timer";
            }
            else
            {
                timer.Start();
                StartStopButton.Content = "Stop Timer";
            }

            isTimerRunning = !isTimerRunning;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedWindowHandle = windowHandles[comboBoxWindows.SelectedItem.ToString()];
        }


        // Add your timer setup and tick event here
    }
}