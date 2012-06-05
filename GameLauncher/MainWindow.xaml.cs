using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Threading;
using System.Net;
using System.IO;
using Zip = Ionic.Zip;
using System.Diagnostics;

namespace GameLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UpdateInfo updateInfo;
        GameInfo gameInfo;
        Thread asyncWorker;

        public MainWindow()
        {
            InitializeComponent();
            asyncWorker = new Thread(AsyncWorker);
            asyncWorker.Start();
        }

        delegate void MyDelegate();

        void AsyncWorker()
        {

            Dispatcher.Invoke(new MyDelegate(() =>
                {
                    updateLabel.Content = "Подключение к серверу...";
                }));
            gameInfo = new GameInfo();
            updateInfo = new UpdateInfo(gameInfo);
            updateInfo.State.WaitOne();
            bool terminate = false;
            Dispatcher.Invoke(new MyDelegate(() =>
            {
                if (updateInfo.FileSize != 0 && MessageBox.Show("Доступен новый патч, установить?\n " + updateInfo.ChangLog, "Recoding Updater", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {                   
                    updateLabel.Content = String.Format("Скачивание патча. Размер: {0} KB", updateInfo.FileSize / 1024);
                }
                else
                {
                    Process.Start(gameInfo.GameExe);
                    Close();
                    terminate = true;
                }
            }));

            if (terminate)
            {
                return;
            }
            Dispatcher.Invoke(new MyDelegate(() =>
                {
                    progressBar.Maximum = updateInfo.FileSize;
                }));

            Stream reader = Extensions.GetFtpResponse(updateInfo.PatchFile, gameInfo.Credentials, WebRequestMethods.Ftp.DownloadFile).GetResponseStream();
            FileStream fs = new FileStream("patch.zip", FileMode.Create);

            byte[] buffer = new byte[512 * 1024];

            long time = DateTime.Now.Ticks;
            while (true)
            {
                               
                int bytesRead = reader.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    fs.Flush();
                    fs.Close();
                    break;
                }
                else
                {
                    long dt = DateTime.Now.Ticks - time;
                    fs.Write(buffer, 0, bytesRead);
                    Dispatcher.Invoke(new MyDelegate(() =>
                    {
                            progressBar.Value += bytesRead;
                            float vel = (float)progressBar.Value / dt * 10000000 / 1024;
                            updateLabel.Content = String.Format("Скачивание патча @ {2} КБ / c. Размер: {0} / {1} KB", (int)progressBar.Value / 1024, updateInfo.FileSize / 1024, (int)vel);
                        }));
                }
            }

            Dispatcher.Invoke(new MyDelegate(() =>
            {
                progressBar.Value = 0;
                updateLabel.Content = String.Format("Распаковка патча. Размер: {0} KB", updateInfo.FileSize / 1024);
            }));

            Zip.ZipFile zf = new Zip.ZipFile("patch.zip");
            zf.ExtractProgress += new EventHandler<Zip.ExtractProgressEventArgs>(zf_ExtractProgress);
            string dir;

            if (updateInfo.Type == UpdateType.Regular)
                dir = Directory.GetCurrentDirectory();
            else
                dir = Directory.GetCurrentDirectory() + "\\~tmp";

            zf.ExtractAll(dir, Zip.ExtractExistingFileAction.OverwriteSilently);
            zf.Dispose();

            File.Delete("patch.zip");

            Dispatcher.Invoke(new MyDelegate(() =>
            {
                updateLabel.Content = String.Format("Ожидание завершения...");
            }));

            string msg;
            if (updateInfo.Type == UpdateType.Regular)
                msg = "Патч установлен. Запустить программу?";
            else
                msg = "Патч скачан. Запустить его установку?";

            if (MessageBox.Show(msg, "Recoding Updater", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (updateInfo.Type == UpdateType.Regular)
                {
                    StreamWriter sw = new StreamWriter(new FileStream("version.txt", FileMode.Create));
                    sw.Write(updateInfo.LastPatch);
                    sw.Flush();
                    sw.Close();

                    Process.Start(gameInfo.GameExe);
                }
                else
                {
                    Process p = new Process();
                    p.StartInfo = new ProcessStartInfo("~tmp\\" + gameInfo.SetupExe);
                    p.Start();
                    p.WaitForExit();

                    if (p.ExitCode == 0)
                    {
                        StreamWriter sw = new StreamWriter(new FileStream("version.txt", FileMode.Create));
                        sw.Write(updateInfo.LastPatch);
                        sw.Flush();
                        sw.Close();
                        Directory.Delete("~tmp", true);
                    }
                    else
                    {
                        MessageBox.Show("Установка патча не удалась. Возможно ваш антивирус блокирует установку. В любом случае попробуйте установить патч вручную из каталога ~tmp.");
                    }
                }
            } 

            Dispatcher.Invoke(new MyDelegate(() =>
            {
                Close();
            }));
        }


        void zf_ExtractProgress(object sender, Zip.ExtractProgressEventArgs e)
        {
            Dispatcher.Invoke(new MyDelegate(() =>
            {
                progressBar.Value = e.BytesTransferred;
            }));
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            updateInfo.Dispose();
            asyncWorker.Abort();
        }
    }
}
