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



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Get the handle of the window to capture (e.g., the currently active window)
            //IntPtr hwnd = GetForegroundWindow();
            var hwnd = selectedWindowHandle;

            // Capture the screenshot
            Bitmap screenshot = CaptureWindow(hwnd);

            // Save the screenshot to a file
            string directoryPath = @"D:\Work\IA-Mbot\screenshots";
            string fileName = "screenshot.png";
            string filePath = System.IO.Path.Combine(directoryPath, fileName);

            // Ensure the directory exists
            Directory.CreateDirectory(directoryPath);

            try
            {
                screenshot.Save(filePath, ImageFormat.Png);
                Console.WriteLine($"Screenshot saved to {filePath}");
            }
            catch (ExternalException ex)
            {
                Console.WriteLine($"An error occurred while saving the screenshot: {ex.Message}");
            }
            finally
            {
                // Optional: Dispose of the bitmap to free resources
                screenshot.Dispose();
            }


            List<WindowInfo> windows = GetOpenWindows();

            foreach (var window in windows)
            {
                Console.WriteLine($"Title: {window.Title}");
            }
        }

        static Bitmap CaptureWindow(IntPtr hwnd)
        {
            // Get the window rectangle
            GetWindowRect(hwnd, out RECT rect);

            // Calculate the width and height
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // Create a bitmap to hold the screenshot
            Bitmap bmp = new Bitmap(width, height);

            // Get the device context (DC) of the window
            IntPtr hdcWindow = GetWindowDC(hwnd);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdcMemDC = g.GetHdc();

                // BitBlt the window DC to the bitmap's DC
                BitBlt(hdcMemDC, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY);

                // Release the device contexts
                g.ReleaseHdc(hdcMemDC);
            }

            ReleaseDC(hwnd, hdcWindow);

            return bmp;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedWindowHandle = windowHandles[comboBoxWindows.SelectedItem.ToString()];
        }
    }
}