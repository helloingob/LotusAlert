using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Xceed.Wpf.Toolkit;

namespace LotusAlert
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly string CoordsFile = AppDomain.CurrentDomain.BaseDirectory + @"\coords.txt";
        private readonly string ColorFile = AppDomain.CurrentDomain.BaseDirectory + @"\color.txt";
        private readonly string SoundFile = AppDomain.CurrentDomain.BaseDirectory + @"\gong.mp3";

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            TbxCoords.AcceptsReturn = true;
            TbxCoords.TextWrapping = TextWrapping.Wrap;

            if (File.Exists(CoordsFile))
            {
                TbxCoords.Text = File.ReadAllText(CoordsFile, System.Text.Encoding.UTF8);
            }
            if (File.Exists(ColorFile))
            {
                TbxColor.Text = File.ReadAllText(ColorFile, System.Text.Encoding.UTF8);
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Started.");
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            cancellationTokenSource = new CancellationTokenSource();
            WatchLotusSpawn(TbxCoords.Text, TbxColor.Text);
        }

        private void WatchLotusSpawn(string coords, string hexColor)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Debug.WriteLine("Executing.");
                    foreach (Point coord in GetCoords(coords))
                    {
                        if (CompareColor(coord, hexColor))
                        {
                            Debug.WriteLine("Found!");
                            PlayGong();
                            System.Windows.MessageBox.Show("At " + coord.X + "/" + coord.Y + " at " + DateTime.Now.ToString("HH:mm:ss"), "Lotus found!", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    Thread.Sleep(5000);

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private void PlayGong()
        {
            if (File.Exists(SoundFile))
            {
                MediaPlayer mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(SoundFile));
                mediaPlayer.Play();
            }
        }

        private List<Point> GetCoords(string input)
        {
            var coords = new List<Point>();
            var separator = new string[] { "\r\n" };

            foreach (var line in input.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var rawCoords = line.Split('/');
                    int inputX = Convert.ToInt32(rawCoords[0]);
                    int inputY = Convert.ToInt32(rawCoords[1]);
                    coords.Add(new Point(inputX, inputY));
                }
                catch (Exception)
                {
                    //Invalid coord, ignore
                }
            }

            return coords;
        }

        private static bool CompareColor(Point coord, string hexColor)
        {
            try
            {
                if (!hexColor.StartsWith("#"))
                {
                    hexColor = "#" + hexColor;
                }
                Color colorInput = ColorTranslator.FromHtml(hexColor);
                Color colorCoord = GetColorAt(coord.X, coord.Y);

                if (colorInput.ToArgb() == colorCoord.ToArgb())
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop.");
            cancellationTokenSource.Cancel();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(CoordsFile))
            {
                File.Delete(CoordsFile);
            }
            if (File.Exists(ColorFile))
            {
                File.Delete(ColorFile);
            }
            File.WriteAllText(CoordsFile, TbxCoords.Text);
            File.WriteAllText(ColorFile, TbxColor.Text);
        }
    }
}