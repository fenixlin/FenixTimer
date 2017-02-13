using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FenixTimer
{

    // Version 0.5

    public partial class Form1 : Form
    {
        const int beatLimit = 11;
        const int beatDeviation = 3;

        const double baseProb = 1.0/(25*25); // 计算方法：x天后概率为1，则base是1/x^2。理念：越到后面越难也越要坚持
        double coefProb = 0;
        int[] beatCount = new int[180];
        int[] lockCount = new int[180];
        int extended = 2;
        bool locked = false;
        int nDays = 0;
        int ctnDays = 0;
        int ctnStat = 0;
        DateTime start_dt = DateTime.Now;

        private static class FlashWindow
        {
            // To support flashing.
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

            //Flash both the window caption and taskbar button.
            //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
            public const UInt32 FLASHW_ALL = 3;

            // Flash continuously until the window comes to the foreground. 
            public const UInt32 FLASHW_TIMERNOFG = 12;

            [StructLayout(LayoutKind.Sequential)]
            public struct FLASHWINFO
            {
                public UInt32 cbSize;
                public IntPtr hwnd;
                public UInt32 dwFlags;
                public UInt32 uCount;
                public UInt32 dwTimeout;
            }

            // Do the flashing - this does not involve a raincoat.
            public static bool FlashWindowEx(Form form)
            {
                IntPtr hWnd = form.Handle;
                FLASHWINFO fInfo = new FLASHWINFO();

                fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
                fInfo.hwnd = hWnd;
                fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
                fInfo.uCount = UInt32.MaxValue;
                fInfo.dwTimeout = 0;

                return FlashWindowEx(ref fInfo);
            }
        }
        
        private double getAvg(int span)
        {
            if (nDays <= 0) return 0;
            double theSum = 0;
            for (int i=1; i<=Math.Min(nDays, span); i++)
            {
                theSum += beatCount[i];
            }
            return theSum / Math.Min(nDays, span);
        }

        private void updateAvg()
        {
            label3.Text = String.Format("3/7/14 Avg: {0:F1}/ {1:F1}/ {2:F1}", getAvg(3), getAvg(7), getAvg(14));
        }

        private void updateTitle()
        {
            if (extended == 0 && locked)
            {
                this.Text = "Locked";
            }
            else
            {
                this.Text = String.Format("Hope Generator");
            }
        }
         
        private double getProb()
        {
            return baseProb * coefProb * coefProb;
        }

        private void updateCtnLabel()
        {
            if (ctnStat>=0)
            {
                label5.Text = String.Format("已连续{0}天,目前概率{1:F3}‰", ctnDays, getProb() * 1000);
            }
            else
            {
                label5.Text = String.Format("已连续{0}天,目前概率{1:F3}‰", ctnStat, getProb() * 1000);
            }            
        }

        private void updateMainLabel()
        {
            if (locked)
            {
                label2.Text = "Locked";
                label2.ForeColor = Color.Red;
            }
            else
            {
                label2.Text = String.Format("{0} / {1}", beatCount[0], beatLimit);
                label2.ForeColor = Color.Black;
            }
        }

        public Form1()
        {            
            InitializeComponent();
            beatCount[0] = 0;

            if (File.Exists("log.txt"))
            {
                StreamReader sr = new StreamReader("log.txt");
                DateTime dt = DateTime.Now;
                String line;               
                 
                if ((line = sr.ReadLine()) != null)
                {
                    String[] key = line.Split(' ');
                    ctnDays = Convert.ToInt32(key[0]);
                    ctnStat = Convert.ToInt32(key[1]);
                    coefProb = Convert.ToDouble(key[2]);
                }
                if ((line = sr.ReadLine()) != null)
                {
                    String[] key = line.Split(' ');

                    TimeSpan ts = dt - DateTime.Parse(key[0]);
                    beatCount[ts.Days] = Convert.ToInt32(key[1]);
                    lockCount[ts.Days] = Convert.ToInt32(key[2]);
                    if (ts.Days>0 && beatCount[1] < beatLimit)
                    {
                        ctnStat = Math.Max(ctnStat - 1, -1);
                        ctnDays = 0;
                        if (ctnStat < 0)
                        {
                            if (coefProb - 1 > 0) coefProb -= 1;
                            else coefProb = 0;
                        }
                    }
                }
                while ((line = sr.ReadLine())!=null)
                {
                    String[] key = line.Split(' ');
                    
                    TimeSpan ts = dt - DateTime.Parse(key[0]);
                    beatCount[ts.Days] = Convert.ToInt32(key[1]);
                    lockCount[ts.Days] = Convert.ToInt32(key[2]);

                    if (ts.Days > nDays) nDays = ts.Days;
                }
                sr.Close();

                updateTitle();
                updateAvg();
                updateMainLabel();
                updateCtnLabel();
                if (beatCount[0] >= beatLimit)
                {
                    button5.Enabled = true;
                    button5.ForeColor = Color.Red;
                }
                else
                {
                    button5.Enabled = false;
                }

                chart1.Series.Clear();
                Series ser1 = new Series("Expectation");
                ser1.ChartType = SeriesChartType.Spline;
                ser1.Color = Color.Red;
                ser1.BorderWidth = 2;

                Series ser2 = new Series("BeatCount");
                ser2.ChartType = SeriesChartType.Column;
                ser2.Color = Color.FromArgb(200,0xFF,0xB0,0x00);
                //ser2.CustomProperties = "PixelPointWidth = 15";

                Series ser3 = new Series("LockCount");
                ser3.ChartType = SeriesChartType.Column;                
                ser3.Color = Color.FromArgb(200, 0x00, 0x00, 0x00);
                //ser3.CustomProperties = "PixelPointWidth = 15";

                for (int i = 14; i >= 1; i--)
                {
                    ser1.Points.AddXY(-i, beatLimit);
                }
                for (int i = Math.Min(14, nDays); i >= 1; i--)
                {       
                    ser2.Points.AddXY(-i, beatCount[i]);
                    ser3.Points.AddXY(-i, lockCount[i]);
                }                
                chart1.Series.Add(ser2);
                chart1.Series.Add(ser3);
                chart1.Series.Add(ser1);

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            FileStream fs = new FileStream("log.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
                
            sw.WriteLine(ctnDays.ToString() + " " + ctnStat.ToString() + " " + coefProb.ToString());
            for (int i = 0; i <= nDays; i++)
            {
                sw.WriteLine(start_dt.AddDays(-i).ToShortDateString().ToString() + " " + beatCount[i] + " " + lockCount[i]);
            }

            sw.Close();
            fs.Close();
        }
        
        private void timer3_Tick(object sender, EventArgs e)
        {
            // frequency: 5 min
            int nowmin = int.Parse(DateTime.Now.Minute.ToString());
            if (nowmin>=0 && nowmin<5 || nowmin>=30 && nowmin<35) FlashWindow.FlashWindowEx(this); // send warnings on specific time
        }

        private void button2_Click(object sender, EventArgs e)
        {
            locked = !locked;
            button2.Text = locked?(extended > 0 ? "Unlock" : "U") : (extended > 0 ? "Lock" : "L");
            updateMainLabel();
            updateTitle();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (extended==0)
            {
                extended = 1;
                button3.Text = "↓";
                label2.Visible = true;
                label4.Visible = true;
                button2.Text = locked? "Unlock" : "Lock";
                button2.Width = 50;
                button2.Location = new Point(64, 61);
                button4.Text = "Beat";
                button4.Width = 61;
                button4.Location = new Point(123, 61);
                button3.Width = 22;
                button3.Location = new Point(215, 61);
                updateTitle(); 
                this.Opacity = 1;
                this.Width = 266;
                this.Height = 130;
            }
            else if (extended==1)
            {
                extended = 2;
                button3.Text = "↑";
                this.Height = 369;
            }
            else if (extended==2)
            {
                extended = 0;
                button3.Text = "＋";
                label2.Visible = false;
                label4.Visible = false;
                button2.Text = locked? "U" : "L";
                button2.Width = 25;
                button2.Location = new Point(40, 0);
                button4.Text = "B";
                button4.Width = 35;
                button4.Location = new Point(70, 0);
                button3.Location = new Point(130, 0);
                updateTitle();
                this.Opacity = 0.3;
                this.Width = 190;
                this.Height = 70;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            beatCount[0] += 1;
            if (locked)
            {
                lockCount[0] += 1;
            }
            if (beatCount[0] == beatLimit+beatDeviation || beatCount[0] == beatLimit-beatDeviation)
            {
                coefProb += 0.4;
                updateCtnLabel();
            }
            if (beatCount[0] == beatLimit)
            {
                coefProb += 0.4;
                ctnStat = 1;
                ctnDays += 1;
                updateCtnLabel();
                button5.Enabled = true;
                button5.ForeColor = Color.Red;
            }
            updateMainLabel();
            if (extended > 0)
            {
                button4.Text = "Beat";
            }
            else
            {
                button4.Text = "B";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();
            int top = 10000000;
            int limit = Convert.ToInt32(Math.Ceiling(getProb() * top));
            int dice = rnd.Next(top);
            if (dice<limit)
            {
                MessageBox.Show(String.Format("抽中了抽中了！！！Dice result {0} (中奖率{1}/{2})", dice, limit, top), "Prize Drawing Result", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show(String.Format("明天也要继续加油呐！Dice result {0} (中奖率{1}/{2})", dice, limit, top), "Prize Drawing Result", MessageBoxButtons.OK);
            }
            button5.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            coefProb += 0.2;
            updateCtnLabel();
        }
    }
}
