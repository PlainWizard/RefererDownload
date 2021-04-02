using System;
using System.IO;
using System.Reflection;

namespace RefererDownload
{
    public class Config
    {
        public static object lock_obj { set; get; } = new object();
        public static string AppDataPath { set; get; }
        static Config()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyProductAttribute product = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            AppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlainWizard",
                product.Product
                 );
            if (Directory.Exists(AppDataPath) == false)
            {
                Directory.CreateDirectory(AppDataPath);
            }
            Console.WriteLine("用户目录:{0}",AppDataPath);
        }
    }
}
