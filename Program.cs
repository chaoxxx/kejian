using System;
using System.Threading;
using System.Windows.Forms;
using KeJian.Forms;

namespace KeJian
{
    /// <summary>
    /// 程序入口
    /// </summary>
    internal static class Program
    {
        private static Mutex _mutex;

        /// <summary>
        /// 应用程序主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 防止重复启动 - 确保只有一个实例
            bool createdNew;
            _mutex = new Mutex(true, "KeJian_SingleInstance", out createdNew);

            if (!createdNew)
            {
                // 已有实例在运行，激活它
                MessageBox.Show("日记本已在运行中！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 设置高 DPI 支持
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                var mainForm = new MainForm();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序启动失败：{ex.Message}\n\n{ex.StackTrace}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }
    }
}
