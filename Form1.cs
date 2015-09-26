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

namespace FenixTimer
{
    // Version 0.5.2

    public partial class Form1 : Form
    {
        int[] totalAvail = new int[180];
        int[] timeCount = new int[180];
        int[] outputCount = new int[180];
        int[] beatCount = new int[180];
        int[] hoursCount = new int[25];
        int[] currentmae = { 0, 0, 0 };
        int[] bestmae = { 0, 0, 0 };
        int[,] dailyAction = new int[180,3];
        int extended = 2;
        bool outputing = false;
        int maeSwitch = 0;
        int lastTimeCount = 0;
        int n = 0;
        DateTime start_dt = DateTime.Now;

        private void updateTitle()
        {
            if (extended == 0)
            {
                if (timer1.Enabled)
                {
                    this.Text = "Active";
                }
                else
                {
                    this.Text = "Inactive";
                }
            }
            else
            {
                this.Text = String.Format("{0:0%Atk},{1:0%}/{2:0%}", outputCount[0] * 1.0 / 10800, (timeCount[0] * 1.0 - totalAvail[0]) / 36000 + 1, ((DateTime.Now.Date.AddDays(1) - DateTime.Now).TotalSeconds + timeCount[0] - totalAvail[0]) / 36000 + 1);
            }
        }

        private void updateMAE()
        {
            currentmae[maeSwitch] += timeCount[0] - lastTimeCount;
            if (currentmae[maeSwitch] > bestmae[maeSwitch])
            {
                bestmae[maeSwitch] = currentmae[maeSwitch];
            }
            label1.Text = "Best M/A/E: " + timestr_short(bestmae[0]) + " / " + timestr_short(bestmae[1]) + " / " + timestr_short(bestmae[2]);
            lastTimeCount = timeCount[0];            
        }

        private int avgit(int x)
        {
            int sum=0;
            for (int i = 1; i <= x; i++) sum += timeCount[i];
            return sum/x;
        }

        private String timestr(int s)
        {
            return String.Format("{0:D2}:{1:D2}:{2:D2}", s / 3600, s / 60 % 60, s % 60);
        }
        private String timestr_short(int s)
        {
            return String.Format("{0:D2}:{1:D2}", s / 3600, s / 60 % 60);
        }

        public Form1()
        {            
            InitializeComponent();
            totalAvail[0] = 36000;
            timeCount[0] = 0;
            outputCount[0] = 0;
            beatCount[0] = 0;
            for (int i=0; i<3; i++)
            {
                dailyAction[0, i] = 0;
            }

            if (File.Exists("log.txt"))
            {
                StreamReader sr = new StreamReader("log.txt");
                DateTime dt = DateTime.Now;
                String line;                

                if ((line = sr.ReadLine()) != null)
                {
                    String[] key = line.Split(' ');
                    for (int i = 0; i < 25; i++) {
                        if (i >= key.Length-1) hoursCount[i] = 0;
                        else hoursCount[i] = Convert.ToInt32(key[i]);
                    }
                }
                if ((line = sr.ReadLine()) != null) {
                    String[] key = line.Split(' ');
                    TimeSpan ts = dt - DateTime.Parse(key[0]);
                    if (ts.Days == 0)
                    {
                        currentmae[0] = Convert.ToInt32(key[1]);
                        currentmae[1] = Convert.ToInt32(key[2]);
                        currentmae[2] = Convert.ToInt32(key[3]);
                    }
                    else hoursCount[24] += 1; // use hoursCount[24] to count valid days (+1 every new day)
                    bestmae[0] = Convert.ToInt32(key[4]);
                    bestmae[1] = Convert.ToInt32(key[5]);
                    bestmae[2] = Convert.ToInt32(key[6]);
                }

                while ((line = sr.ReadLine())!=null)
                {
                    String[] key = line.Split(' ');
                    
                    TimeSpan ts = dt - DateTime.Parse(key[0]);
                    timeCount[ts.Days] = Convert.ToInt32(key[1]);
                    totalAvail[ts.Days] = Convert.ToInt32(key[2]);
                    outputCount[ts.Days] = Convert.ToInt32(key[3]);
                    beatCount[ts.Days] = Convert.ToInt32(key[4]);
                    dailyAction[ts.Days, 0] = Convert.ToInt32(key[5]);
                    dailyAction[ts.Days, 1] = Convert.ToInt32(key[6]);
                    dailyAction[ts.Days, 2] = Convert.ToInt32(key[7]);

                    if (ts.Days > n) n = ts.Days;
                }
                sr.Close();

                button4.Text = "Beat: " + beatCount[0].ToString();
                textBox2.Text = timestr(totalAvail[0]);
                label2.Text = timestr(timeCount[0]);

                updateTitle();
                label1.Text = "Best M/A/E: " + timestr_short(bestmae[0]) + " / " + timestr_short(bestmae[1]) + " / " + timestr_short(bestmae[2]);

                if (dailyAction[0, 0]>0) button7.Enabled = false;
                if (dailyAction[0, 1]>0) button8.Enabled = false;
                if (dailyAction[0, 2]>0) button9.Enabled = false;

                chart1.Series.Clear();
                Series ser1 = new Series("Efficiency");
                ser1.ChartType = SeriesChartType.Spline;
                ser1.Color = Color.Blue;
                ser1.BorderWidth = 1;
                ser1.XAxisType = AxisType.Primary;
                ser1.YAxisType = AxisType.Primary;

                Series ser2 = new Series("BeatCount");
                ser2.ChartType = SeriesChartType.Column;
                ser2.Color = Color.FromArgb(120,0,100,0); 
                ser2.XAxisType = AxisType.Primary;
                ser2.YAxisType = AxisType.Secondary;

                Series ser3 = new Series("ValidTime");
                ser3.ChartType = SeriesChartType.Spline;
                ser3.Color = Color.Red;
                ser3.BorderWidth = 3;
                ser3.XAxisType = AxisType.Primary;
                ser3.YAxisType = AxisType.Primary;

                Series ser4 = new Series("PersonalGrowth");
                ser4.ChartType = SeriesChartType.Spline;
                ser4.Color = Color.HotPink;
                ser4.BorderWidth = 3;
                ser4.XAxisType = AxisType.Primary;
                ser4.YAxisType = AxisType.Primary;

                for (int i = n; i >= 1; i--)
                {
                    if (totalAvail[i]!=0) ser1.Points.AddY(timeCount[i] * 1.0 / totalAvail[i]);
                        else ser1.Points.AddY(0);
                    ser2.Points.AddY(beatCount[i]);
                    ser3.Points.AddY((timeCount[i] * 1.0) / 21600);
                    ser4.Points.AddY((outputCount[i] * 1.0) / 7200);
                }
                chart1.Series.Add(ser1);
                chart1.Series.Add(ser2);
                chart1.Series.Add(ser3);
                chart1.Series.Add(ser4);

                chart2.Series.Clear();
                Series ser5 = new Series("AverageSpent");
                ser5.ChartType = SeriesChartType.Column;
                ser5.Color = Color.DarkGreen;
                for (int i = 0; i < 24; i++)
                {
                    if (hoursCount[24] > 0) ser5.Points.AddY(hoursCount[i] / hoursCount[24]);
                    else ser5.Points.AddY(0);
                }
                chart2.Series.Add(ser5);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            lastTimeCount = timeCount[0];
            updateTitle();
            DateTime dt = DateTime.Now;
            if (dt.Hour<12)
            {
                maeSwitch = 0;
            }
            else if (dt.Hour < 18)
            {
                maeSwitch = 1;
            }
            else
            {
                maeSwitch = 2;
            }     
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            updateMAE();
            updateTitle(); 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (extended==0)
            {
                extended = 1;
                button3.Text = "↓↓";
                label2.Visible = true;
                label4.Visible = true;
                button1.Text = "Start";
                button1.Width = 47;
                button1.Location = new Point(12, 82);
                button2.Text = "Pause";
                button2.Width = 46;
                button2.Location = new Point(65, 82);
                button4.Text = "Beat: " + beatCount[0].ToString();
                button4.Width = 61;
                button4.Location = new Point(117, 82);
                checkBox1.Location = new Point(181, 87);
                button3.Location = new Point(197, 82);
                updateTitle(); 
                this.Opacity = 1;
                this.Width = 266;
                this.Height = 153;
            }
            else if (extended==1)
            {
                extended = 2;
                button3.Text = "↑↑";
                this.Height = 500;
            }
            else if (extended==2)
            {
                extended = 0;
                button3.Text = "＋＋";
                label2.Visible = false;
                label4.Visible = false;
                button1.Text = "S";
                button1.Width = 25;
                button1.Location = new Point(10, 0);
                button2.Text = "P";
                button2.Width = 25;
                button2.Location = new Point(40, 0);
                button4.Text = "B:" + beatCount[0].ToString();
                button4.Width = 35;
                button4.Location = new Point(70, 0);
                checkBox1.Location = new Point(110, 5);
                button3.Location = new Point(130, 0);
                updateTitle();
                this.Opacity = 0.3;
                this.Width = 190;
                this.Height = 70;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timeCount[0] += 1;
            hoursCount[DateTime.Now.Hour] += 1;
            if (outputing) outputCount[0] += 1;
            label2.Text = timestr(timeCount[0]);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Enabled = false;
            updateMAE();

            FileStream fs = new FileStream("log.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < 25; i++)
            {
                sw.Write(hoursCount[i].ToString() + " ");
            }
            sw.WriteLine();
            sw.WriteLine(start_dt.ToShortDateString().ToString() + " " + currentmae[0].ToString() + " " + currentmae[1].ToString() + " " + currentmae[2].ToString() + " " +
                bestmae[0].ToString() + " " + bestmae[1].ToString() + " " + bestmae[2].ToString());
            for (int i = 0; i <= n ; i++ )
            {
                sw.WriteLine(start_dt.AddDays(-i).ToShortDateString().ToString() + " " + timeCount[i] + " " + totalAvail[i] + " " + outputCount[i] + " " + beatCount[i] + " " + dailyAction[i, 0] + " " + dailyAction[i, 1] + " " + dailyAction[i, 2]);
            }

            sw.Close();
            fs.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            updateMAE();
            updateTitle(); 

            button4.Enabled = false;
            beatCount[0] += 1;
            if (extended > 0)
            {
                button4.Text = "Beat: " + beatCount[0].ToString();
            }
            else
            {
                button4.Text = "B:" + beatCount[0].ToString();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            totalAvail[0] -= 1800;
            textBox2.Text = timestr(totalAvail[0]);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            totalAvail[0] += 1800;
            textBox2.Text = timestr(totalAvail[0]);
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            updateTitle(); // update title on every 5 minute
            if (!button4.Enabled) button4.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            outputing = !outputing;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            dailyAction[0, 0] = 1;
            button7.Enabled = false;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            dailyAction[0, 1] = 1;
            button8.Enabled = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            dailyAction[0, 2] = 1;
            button9.Enabled = false;
        }
    }
}

