using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace RefererDownload
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// PlainWizard
    /// https://github.com/PlainWizard/RefererDownload
    /// Bug反馈群:476304388
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pathdir= Environment.CurrentDirectory;
            Txt_dir.Text = pathdir;
            Init();
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 2) InitUrlArg(args[1], args[2]);
        }
        public MainWindow(string url,string referer) : this()
        {
            InitUrlArg(url, referer);
        }
        void InitUrlArg(string url, string referer)
        {
            if (!string.IsNullOrEmpty(url))
            {
                var r = Regex.Match(Regex.Replace(url, "\\s+$", ""), "\\S+$");
                if (r.Success) url = r.Value;
                Txt_url.Text = url;
            }
            Txt_ref.Text = referer;
        }
        string pathdir = "";
        string filename = "";
        private void Button_Click_Dir(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "All Files|*.*",
                FileName = filename
            };
            if (dialog.ShowDialog().GetValueOrDefault()) Txt_dir.Text = dialog.FileName;
        }

        private void Button_Click_Down(object sender, RoutedEventArgs e)
        {
            DownFile(Txt_url.Text, Txt_dir.Text);
        }
        void Init()
        {
            try
            {
                var dir = Path.Combine(Config.AppDataPath, "config.data");
                if (Txt_url.Text == "")
                {
                    var str = File.ReadAllText(dir);
                    var arr = str.Split(';');
                    Txt_url.Text = arr[0];
                    Txt_ref.Text = arr[1];
                    Txt_dir.Text = arr[2];
                }
                else
                {
                    string str = $"{Txt_url.Text};{Txt_ref.Text};{Txt_dir.Text}";
                    File.WriteAllText(dir, str);
                }
            }
            catch { }
        }
        void ChangeStatus(bool IsEnabled=true)
        {
            if (IsEnabled)
            {
                app.IsEnabled = true;
                FunStop = null;
                Btn_Stop.Visibility = Visibility.Hidden;
            }
            else
            {
                app.IsEnabled = false;
                Btn_Stop.Visibility = Visibility.Visible;
            }
        }
        void DownFile(string address, string fileName)
        {
            if (File.Exists(fileName))
            {
                var r = MessageBox.Show("文件已存在,是否替换?", "操作提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (r != MessageBoxResult.Yes)
                {
                    Txb_tip.Text = "取消替换下载,清修改文件名";
                    return;
                }
            }
            ChangeStatus(false);
            using (WebClient c = new WebClient())
            {
                try
                {
                    c.Headers.Set("referer", Txt_ref.Text);
                    c.Headers.Set("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                    c.Headers.Set("accept-language", "en-US,en;q=0.9,zh-CN;q=0.8,zh;q=0.7");
                    c.Headers.Set("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/597.21 (KHTML, like Gecko) Chrome/89.0.3389.114 Safari/657.63");
                    c.DownloadProgressChanged += (sender, e) =>
                    {
                        Prb_down.Minimum = 0;
                        Prb_down.Maximum = e.TotalBytesToReceive;
                        Prb_down.Value = e.BytesReceived;
                        Txb_tip.Text = e.ProgressPercentage + "%";
                    };
                    c.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Cancelled)
                        {
                            Txb_tip.Text = "取消下载";
                        }

                        if (e.Error != null)
                        {
                            Exception ex = e.Error;
                            while (ex.InnerException != null) ex = ex.InnerException;
                            Txb_tip.Text = ex.Message;
                        }
                        else
                        {
                            Txb_tip.Text = "下载完成";
                            Init();
                        }
                        ChangeStatus();
                    };
                    FunStop = () =>
                    {
                        c.CancelAsync();
                    };
                    Txb_tip.Text = "开始下载...";
                    Btn_Stop.Visibility = Visibility.Visible;
                    c.DownloadFileAsync(new Uri(address), fileName);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    Txb_tip.Text = ex.Message;
                    ChangeStatus();
                }
            }
        }
        /// <summary>
        /// wpf解码
        /// PlainWizard
        /// https://github.com/PlainWizard/RefererDownload
        /// Bug反馈群:476304388
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string UrlEncode(string str)
        {
            try
            {
                List<byte> bs = new List<byte>();
                byte[] byStr = Encoding.UTF8.GetBytes(str);
                for (int i = 0; i < byStr.Length; i++)
                {
                    byte b = byStr[i];
                    if (b == 37)
                    {
                        b = Convert.ToByte(Encoding.UTF8.GetString(byStr, i + 1, 2), 16);
                        i += 2;
                    }
                    bs.Add(b);
                }
                return Encoding.UTF8.GetString(bs.ToArray());
            }
            catch (Exception)
            {
                return str;
            }
        }
        private void Txt_url_TextChanged(object sender, TextChangedEventArgs e)
        {
            //filename*=UTF-8''mmexport1616406087249.jpg&
            var str = UrlEncode(Txt_url.Text);
            var u = Regex.Match(str, "filename.{1,9}([^']+?)&");
            if (u.Success)
            {
                filename = u.Groups[1].Value;
            }
            else
            {
                var r = Regex.Match(Regex.Replace(str, "\\s+$",""), "\\S+$");
                if (r.Success) filename = r.Value;
                filename = filename.Substring(filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            }
            try
            {
                Txt_dir.Text = Path.Combine(pathdir, filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_OpenDir(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new FileInfo(Txt_dir.Text).DirectoryName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        Action FunStop;
        private void Button_Click_Stop(object sender, RoutedEventArgs e)
        {
            FunStop?.Invoke();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                var str = Clipboard.GetText();
                var r = Regex.Match(Regex.Replace(str, "\\s+$", ""), "\\S+$");
                if (r.Success) str = r.Value;
                Txt_url.Text = str;
            }
        }

        private void Txt_dir_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var f = new FileInfo(Txt_dir.Text);
                pathdir = f.DirectoryName;
                filename = f.Name;

            }
            catch { }
        }
    }
}
