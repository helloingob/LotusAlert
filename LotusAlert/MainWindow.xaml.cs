using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace LotusAlert
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly string CoordsFile = AppDomain.CurrentDomain.BaseDirectory + @"\coords.txt";
        private readonly string ColorFile = AppDomain.CurrentDomain.BaseDirectory + @"\colors.txt";
        private readonly string SoundFile = AppDomain.CurrentDomain.BaseDirectory + @"\gong.mp3";

        private List<string> NodeColorArray = new List<string>()
        {
            "#E1B", "#E2B", "#E3B", "#E4B", "#E5B", "#E6B", "#E7B", "#E8B", "#E9B", "#EAB", "#EBB"
        };

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            if (File.Exists(CoordsFile))
            {
                TbxCoords.Text = File.ReadAllText(CoordsFile, System.Text.Encoding.UTF8);
            }

            if (File.Exists(ColorFile))
            {
                TbxColors.Text = File.ReadAllText(ColorFile, System.Text.Encoding.UTF8);
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (TbxCoords.Text.Length > 0)
            {
                WriteLog("Started.");
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
                cancellationTokenSource = new CancellationTokenSource();
                WatchLotusSpawn(TbxCoords.Text, TbxColors.Text);
            } else
            {
                MessageBox.Show("Enter dimensions for rectangle as shown in the tooltip!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TbxCoords.Focus();
            }
        }

        private void WatchLotusSpawn(string coords, string colors)
        {
            var parts = coords.Split('/');
            int xCoord = Convert.ToInt32(parts[0]);
            WriteLog("X: " + xCoord);
            int yCoord = Convert.ToInt32(parts[1]);
            WriteLog("Y: " + yCoord);
            int width = Convert.ToInt32(parts[2]);
            WriteLog("Width: " + width);
            int height = Convert.ToInt32(parts[3]);
            WriteLog("Height: " + height);

            if (colors.Length > 0)
            {
                foreach (string newColor in colors.Split(','))
                {
                    WriteLog("New color added: " + newColor);
                    NodeColorArray.Add(newColor);
                }
            }

            Task.Run(() =>
            {
                while (true)
                {
                    WriteLog("Searching for node ...");
                    if (FindNode(xCoord, yCoord, width, height))
                    {
                        WriteLog("Node found.");
                        PlayGong();
                        MessageBox.Show("Node found at " + DateTime.Now.ToString("HH:mm:ss"), " Node found!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        WriteLog("No node found.");
                    }
                    Thread.Sleep(1000);

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

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            WriteLog("Stop.");
            cancellationTokenSource.Cancel();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(CoordsFile, TbxCoords.Text);
            File.WriteAllText(ColorFile, TbxColors.Text);
        }

        private bool FindNode(int x, int y, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(x, y, 0, 0, bitmap.Size);
                //bitmap.Save(path, ImageFormat.Bmp);
                //Bitmap savedBitmap = new Bitmap(path);
                return FindColors(bitmap);
            }
        }

        private bool FindColors(Bitmap bitmap)
        {
            for (int i = 0; i <= bitmap.Width - 1; i++)
            {
                for (int b = 0; b <= bitmap.Height - 1; b++)
                {
                    var foundColor = HexConverter(bitmap.GetPixel(i, b));
                    if (NodeColorArray.Any(foundColor.StartsWith))
                    {
                        WriteLog("Found color (" + foundColor + ") at " + i + "/" + b);
                        //bitmap.Save("z:\\node_spawn_" + foundColor + "_" + DateTime.Now.ToString("HHmmssfff") + ".bmp", ImageFormat.Bmp);
                        return true;
                    }
                }
            }
            return false;
        }

        private static String HexConverter(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void WriteLog(string input)
        {
            var date = DateTime.Now.ToString("dd.MM.yy HH:mm:ss.fff");
            Debug.WriteLine("[" + date + "] " + input);
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            var parts = TbxCoords.Text.Split('/');
            int xCoord = Convert.ToInt32(parts[0]);
            int yCoord = Convert.ToInt32(parts[1]);
            int width = Convert.ToInt32(parts[2]);
            int height = Convert.ToInt32(parts[3]);

            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(xCoord, yCoord, 0, 0, bitmap.Size);
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "node_spawn_" + DateTime.Now.ToString("HHmmssfff") + ".bmp",
                Filter = "Bitmap Image (.bmp)|*.bmp"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                bitmap.Save(saveFileDialog.FileName, ImageFormat.Bmp);
                WriteLog("Screenshot saved to file: " + saveFileDialog.FileName);
            }
        }
    }
}