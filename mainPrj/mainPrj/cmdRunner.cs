using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cmdRunner
{
    /// <summary>  
    /// 执行CMD命令，或以进程的形式打开应用程序（d:\*.exe）  
    /// </summary>  
    public class Cmd
    {

        public static bool useBatMode = false;  // 是否使用.bat模式运行工具  
        public static bool singleBat = true;    // 是否使用单个bat文件执行操作  


        /// <summary>  
        /// 定义委托接口处理函数，用于实时处理cmd输出信息  
        /// </summary>  
        public delegate void Method();

        /// <summary>  
        /// 在新的线程中执行method逻辑  
        /// </summary>  
        public static void ThreadRun(Method method, Form form = null, Button button = null, String Text = "执行中", bool useThread = true)
        {
            if (useThread)
            {
                Thread thread = new Thread(delegate ()
                {
                    // 允许不同线程间的调用  
                    Control.CheckForIllegalCrossThreadCalls = false;

                    // 设置按钮和界面按钮不可用  
                    String text = "";
                    if (form != null) form.ControlBox = false;

                    if (button != null)
                    {
                        text = button.Text;
                        button.Text = Text;
                        button.Enabled = false;
                    }

                    // 执行method逻辑  
                    if (method != null) method();


                    if (button != null)
                    {
                        button.Text = text;
                        button.Enabled = true;
                    }
                    if (form != null) form.ControlBox = true;
                });

                thread.Priority = ThreadPriority.AboveNormal;           // 设置子线程优先级  
                Thread.CurrentThread.Priority = ThreadPriority.Highest; // 设置当前线程为最高优先级  
                thread.Start();
            }
            else
            {
                // 设置按钮和界面按钮不可用  
                String text = "";
                if (form != null) form.ControlBox = false;

                if (button != null)
                {
                    text = button.Text;
                    button.Text = Text;
                    button.Enabled = false;
                }

                // 执行method逻辑  
                if (method != null) method();


                if (button != null)
                {
                    button.Text = text;
                    button.Enabled = true;
                }
                if (form != null) form.ControlBox = true;
            }
        }

        /// <summary>  
        /// 以后台进程的形式执行应用程序（d:\*.exe）  
        /// </summary>  
        public static Process newProcess(String exe)
        {
            Process P = new Process();
            P.StartInfo.CreateNoWindow = true;
            P.StartInfo.FileName = exe;
            P.StartInfo.UseShellExecute = false;
            P.StartInfo.RedirectStandardError = true;
            P.StartInfo.RedirectStandardInput = true;
            P.StartInfo.RedirectStandardOutput = true;
            //P.StartInfo.WorkingDirectory = @"C:\windows\system32";  
            P.Start();
            return P;
        }

        /// <summary>  
        /// 创建指定命令的bat文件  
        /// </summary>  
        public static string createTmpBat(String cmd)
        {
            String filePath = AppDomain.CurrentDomain.BaseDirectory + @"tools\" + (singleBat ? "CMD_FILE" : DateTime.Now.Ticks.ToString()) + ".bat";
            FileProcess.SaveProcess(cmd, filePath, Encoding.Default);

            return filePath;
        }

        /// <summary>  
        /// 创建包含cmd命令的.bat文件，并执行  
        /// </summary>  
        public static string Run_bat(string cmd)
        {
            String bat = createTmpBat(cmd);

            Process P = newProcess(bat);
            string outStr = P.StandardOutput.ReadToEnd();
            P.Close();

            if (File.Exists(bat)) File.Delete(bat);
            return outStr;
        }

        /// <summary>  
        /// 执行CMD命令  
        /// </summary>  
        public static string Run(string cmd, bool useBatMode = true)
        {
            if (useBatMode) return Run_bat(cmd);    // 使用.bat文件模式执行cmd命令  
            else
            {
                Process P = newProcess("cmd.exe");
                P.StandardInput.WriteLine(cmd);
                P.StandardInput.WriteLine("exit");
                string outStr = P.StandardOutput.ReadToEnd();
                P.Close();
                return outStr;
            }
        }

        /// <summary>  
        /// 定义委托接口处理函数，用于实时处理cmd输出信息  
        /// </summary>  
        public delegate void Callback(String line);

        ///// <summary>  
        ///// 此函数用于实时显示cmd输出信息, Callback示例  
        ///// </summary>  
        //private void Callback1(String line)  
        //{  
        //    textBox1.AppendText(line);  
        //    textBox1.AppendText(Environment.NewLine);  
        //    textBox1.ScrollToCaret();  

        //    richTextBox1.SelectionColor = Color.Green;  
        //    richTextBox1.AppendText(line);  
        //    richTextBox1.AppendText(Environment.NewLine);  
        //    richTextBox1.ScrollToCaret();  
        //}  


        /// <summary>  
        /// 执行CMD语句,实时获取cmd输出结果，输出到call函数中  
        /// </summary>  
        /// <param name="cmd">要执行的CMD命令</param>  
        public static string Run(string cmd, Callback call/*, bool useBatMode = true*/)
        {
            //string cmd_exe = DependentFiles.curDir() + "tools\\cmd.exe";  

            String CMD_FILE = useBatMode ? createTmpBat(cmd) : "cmd.exe"; // 使用.bat文件模式执行cmd命令  

            Process P = newProcess(CMD_FILE);
            if (!useBatMode)
            {
                P.StandardInput.WriteLine(cmd);
                P.StandardInput.WriteLine("exit");
            }

            string outStr = "";
            string line = "", error = "";
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');

            try
            {
                for (int i = 0; i < 3; i++) P.StandardOutput.ReadLine();

                while ((line = P.StandardOutput.ReadLine()) != null || ((line = P.StandardError.ReadToEnd()) != null && !line.Trim().Equals("")))
                {
                    // cmd运行输出信息  
                    if (!line.EndsWith(">exit") && !line.Equals(""))
                    {
                        if (line.StartsWith(baseDir + ">")) line = line.Replace(baseDir + ">", "cmd>\r\n"); // 识别的cmd命令行信息  
                        line = ((line.Contains("[Fatal Error]") || line.Contains("ERROR:") || line.Contains("Exception")) ? "【E】 " : "") + line;
                        if (call != null) call(line);
                        outStr += line + "\r\n";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            P.WaitForExit();
            P.Close();

            // 使用bat模式，非单个bat，执行逻辑后删除bat文件  
            if (useBatMode && !singleBat && File.Exists(CMD_FILE)) File.Delete(CMD_FILE);

            return outStr;
        }


        /// <summary>  
        /// 以进程的形式打开应用程序（d:\*.exe）,并执行命令  
        /// </summary>  
        public static void RunProgram(string programName, string cmd)
        {
            Process P = newProcess(programName);
            if (cmd.Length != 0)
            {
                P.StandardInput.WriteLine(cmd);
            }
            P.Close();
        }


        /// <summary>  
        /// 正常启动window应用程序（d:\*.exe）  
        /// </summary>  
        public static void Open(String exe)
        {
            System.Diagnostics.Process.Start(exe);
        }

        /// <summary>  
        /// 正常启动window应用程序（d:\*.exe）,并传递初始命令参数  
        /// </summary>  
        public static void Open(String exe, String args)
        {
            System.Diagnostics.Process.Start(exe, args);
        }
    }


    public class FileProcess
    {
        #region 文件读取与保存  

        /// <summary>  
        /// 获取文件中的数据串  
        /// </summary>  
        public static string fileToString(String filePath)
        {
            string str = "";

            //获取文件内容  
            if (System.IO.File.Exists(filePath))
            {
                bool defaultEncoding = filePath.EndsWith(".txt");

                System.IO.StreamReader file1;

                file1 = new System.IO.StreamReader(filePath);                  //读取文件中的数据  
                                                                               //if (defaultEncoding) file1 = new System.IO.StreamReader(filePath, Encoding.Default);//读取文件中的数据  
                                                                               //else file1 = new System.IO.StreamReader(filePath);                  //读取文件中的数据  

                str = file1.ReadToEnd();                                            //读取文件中的全部数据  

                file1.Close();
                file1.Dispose();
            }
            return str;
        }

        /// <summary>  
        /// 保存数据data到文件处理过程，返回值为保存的文件名  
        /// </summary>  
        public static String SaveProcess(String data, String filePath, Encoding encoding = null)
        {
            //不存在该文件时先创建  
            System.IO.StreamWriter file1 = null;
            if (encoding == null) file1 = new System.IO.StreamWriter(filePath, false/*, System.Text.Encoding.UTF8*/);     //文件已覆盖方式添加内容  
            else file1 = new System.IO.StreamWriter(filePath, false, Encoding.Default);     // 使用指定的格式进行保存  

            file1.Write(data);                                                              //保存数据到文件  

            file1.Close();                                                                  //关闭文件  
            file1.Dispose();                                                                //释放对象  

            return filePath;
        }

        /// <summary>  
        /// 获取当前运行目录  
        /// </summary>  
        public static string CurDir()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        #endregion
    }
}