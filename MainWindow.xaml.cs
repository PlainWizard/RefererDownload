﻿using System;
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
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Txt_dir.Text = Environment.CurrentDirectory;
            Init();
        }
        string filename = "";
        private void Button_Click_Dir(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "All Files|*.*",
                FileName = filename
            };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                Txt_dir.Text = dialog.FileName;
            }
        }

        private void Button_Click_Down(object sender, RoutedEventArgs e)
        {
            DownFile(Txt_url.Text, Txt_dir.Text);
        }
        void Init()
        {
            try
            {
                if (Txt_url.Text == "")
                {
                    var str = File.ReadAllText(Path.Combine(Config.AppDataPath, "config.data"));
                    var arr = str.Split(';');
                    Txt_url.Text = arr[0];
                    Txt_ref.Text = arr[1];
                    Txt_dir.Text = arr[2];
                }
                else
                {
                    string str=$"{Txt_url.Text};{Txt_ref.Text};{Txt_dir.Text}";
                    File.WriteAllText(Path.Combine(Config.AppDataPath, "config.data"),str);
                }
            }
            catch { }
        }
        void DownFile(string address, string fileName)
        {
            app.IsEnabled = false;
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
                        Btn_Stop.Visibility = Visibility.Hidden;
                        FunStop = null;
                        app.IsEnabled = true;
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
                    app.IsEnabled = true;
                }
            }

        }
        /// <summary>
        /// wpf解码UrlEncode
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string UrlEncode(string str)
        {
            try
            {
                List<byte> bs = new List<byte>();
                byte[] byStr = Encoding.UTF8.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
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
            var u = Regex.Match(UrlEncode(Txt_url.Text), "filename.{1,9}([^']+?)&");
            if (u.Success)
            {
                filename = u.Groups[1].Value;
                Txt_dir.Text = Path.Combine(Environment.CurrentDirectory, filename);
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
    }
}