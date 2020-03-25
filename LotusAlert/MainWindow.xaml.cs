using System;
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
        private readonly string SoundFile = AppDomain.CurrentDomain.BaseDirectory + @"\gong.mp3";

        private readonly string[] NodeColorArray = { "#E1B", "#E2B", "#E3B", "#E4B", "#E5B", "#E6B", "#E7B", "#E8B", "#E9B", "#EAB" };

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
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            WriteLog("Started.");
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            cancellationTokenSource = new CancellationTokenSource();
            WatchLotusSpawn(TbxCoords.Text);
        }

        private void WatchLotusSpawn(string coords)
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
    }
}