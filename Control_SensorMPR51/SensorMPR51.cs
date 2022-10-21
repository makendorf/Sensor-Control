using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Network;

namespace Control_Sensor
{
    public enum StatusSensor
    {
        Unavailable = 0,
        Available = 1
    }
    public partial class SensorMPR51: UserControl
    {
        public TypeSensor type = TypeSensor.None;
        private string headname = "";
        private StatusSensor status;
        private double t_prod;
        private double t_dry;
        private double t_wet;
        private double t_relative;
        public int channel;
        private DateTime timestamp = DateTime.MinValue;
        public DateTime TimeStamp
        {
            get
            {
                return timestamp;
            }
            set
            {
                timestamp = value;
                label10.Text = $"Время: {timestamp}";
            }
        }
        public int Channel 
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
                HeadName = type.ToString() + $" (Канал: {value})";
            }
        }

        public string GUID = "";
        public double T_Prod 
        { 
            get 
            { 
                return t_prod; 
            } 
            set 
            {
                t_prod = value;
                switch (value)
                {
                    case 999:
                        {
                            label6.ForeColor = Color.Red;
                            label6.Text = "OFF";
                            break;
                        }
                    default:
                        {
                            label6.ForeColor = Color.Black;
                            label6.Text = value.ToString();
                            break;
                        }
                }
            } 
        }
        public double T_Dry 
        { 
            get
            { 
                return t_dry;
            } 
            set
            {
                t_dry = value;
                switch (value)
                {
                    case 999:
                        {
                            label7.ForeColor = Color.Red;
                            label7.Text = "OFF";
                            break;
                        }
                    default:
                        {
                            label7.ForeColor = Color.Black;
                            label7.Text = value.ToString();
                            break;
                        }
                } 
            } 
        }
        public double T_Wet
        {
            get
            { 
                return t_wet;
            } 
            set 
            {
                t_wet = value;
                switch (value)
                {
                    case 999:
                        {
                            label8.ForeColor = Color.Red;
                            label8.Text = "OFF";
                            break;
                        }
                    default:
                        {
                            label8.ForeColor = Color.Black;
                            label8.Text = value.ToString();
                            break;
                        }
                }
            } 
        }
        public double T_Relative 
        { 
            get 
            { 
                return t_relative; 
            } 
            set 
            {
                t_relative = value;
                switch (value)
                {
                    case 999:
                        {
                            label9.ForeColor = Color.Red;
                            label9.Text = "OFF";
                            break;
                        }
                    default:
                        {
                            label9.ForeColor = Color.Black;
                            label9.Text = value.ToString();
                            break;
                        }
                }
            } 
        }

        public string HeadName 
        { 
            get { return headname; } 
            set 
            {
                headname = value;
                DrawHeadImage(headname, Color.White);
            } 
        }
        
        public StatusSensor Status 
        { 
            get 
            { 
                return status;
            } 
            set 
            {
                status = value;
            } 
        }


        private Bitmap HeadBitmap;
        public SensorMPR51()
        {
            InitializeComponent();
            HeadBitmap = new Bitmap(HeadBox.Width, HeadBox.Height);
            HeadBox.Image = HeadBitmap;
            T_Prod = 999;
            T_Dry = 999;
            T_Relative = 999;
            T_Wet = 999;
        }
        private void DrawHeadImage(string text, Color color)
        { 
            using (var g = Graphics.FromImage(HeadBitmap))
            {
                BackColor =  Color.FromArgb(218, 223, 223);
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, HeadBitmap.Width, HeadBitmap.Height * 2), 
                    Color.FromArgb(54, 56, 56), 
                    Color.FromArgb(218, 223, 223), 
                    LinearGradientMode.Vertical)
                    )
                {
                    brush.SetSigmaBellShape(0.5f);
                    g.FillRectangle(brush, new Rectangle(0, 0, HeadBitmap.Width, HeadBitmap.Height));

                    label1.Text = text;

                    //g.DrawString(text, new Font("Courier", 16, FontStyle.Bold), drawBrush, HeadBitmap.Width / 2 - HeadBitmap.Width / 7, HeadBitmap.Height / 2 - 10);
                }
            }
        }

        private void SensorMPR51_Resize(object sender, EventArgs e)
        {
            HeadBox.Size = new Size(HeadBox.Width, HeadBox.Height);
            HeadBitmap = new Bitmap(HeadBox.Width, HeadBox.Height);
            DrawHeadImage(headname, Color.Red);
        }
    }
}
