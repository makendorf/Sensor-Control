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
    public partial class Sensor_MBA8_TPM138 : UserControl
    {

        private string headname = "";
        private ConnectionStatus status = ConnectionStatus.None;
        private ReadValue[] t_prod;
        public TypeSensor type = TypeSensor.None;

        private int startAddress = 0;
        public string GUID = "";
        public DateTime timestamp = DateTime.MinValue;
        public DateTime TimeStamp
        {
            get
            {
                return timestamp;
            }
            set
            {
                timestamp = value;
                label18.Text = $"Время: {timestamp}";
            }
        }
        public int _StartAddress 
        { 
            get { return startAddress; } 
            set 
            { 
                startAddress = value; 
                HeadName = type.ToString() + $" (Адресс: {value})"; 
            } 
        }
        public ReadValue[] T_Prod 
        { 
            get 
            { 
                return t_prod; 
            } 
            set 
            {
                t_prod = value;
                for(int i = 0; i < t_prod.Length; i++)
                {
                    GroupBox grp = Controls["groupBox" + (i + 1)] as GroupBox;
                    ChangeName(grp, t_prod[i].NameChannel);
                    ChangeValue(grp.Controls["label" + (i + 2)] as Label, t_prod[i].Value);
                    ChangeStatus(grp.Controls["label" + (i + 10)] as Label, $"{i + 1}) {t_prod[i].Status}");
                }
            } 
        }

        public string HeadName 
        { 
            get { return headname; } 
            set 
            {
                headname = value;
                DrawHeadImage(value, Color.White);
            } 
        }
        
        public ConnectionStatus Status 
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
        private void ChangeStatus(Label label, string value)
        {
            label.Text = value.ToString();
        }
        private void ChangeName(GroupBox groupBox, string name)
        {
            groupBox.Text = name;
        }
        private void ChangeValue(Label label, double value)
        {
            switch (value)
            {
                case 999:
                    {
                        label.ForeColor = Color.Red;
                        label.Text = "ВЫКЛ";
                        break;
                    }
                default:
                    {
                        label.ForeColor = Color.Black;
                        label.Text = Math.Round(value, 2).ToString();
                        break;
                    }
            }
        }

        private Bitmap HeadBitmap;
        public Sensor_MBA8_TPM138()
        {
            InitializeComponent();
            HeadBitmap = new Bitmap(HeadBox.Width, HeadBox.Height);
            Status = ConnectionStatus.None;
            HeadBox.Image = HeadBitmap;

            groupBox1.Paint += GroupBox_Paint;
            groupBox2.Paint += GroupBox_Paint;
            groupBox3.Paint += GroupBox_Paint;
            groupBox4.Paint += GroupBox_Paint;
            groupBox5.Paint += GroupBox_Paint;
            groupBox6.Paint += GroupBox_Paint;
            groupBox7.Paint += GroupBox_Paint;
            groupBox8.Paint += GroupBox_Paint;

            for(int i = 0; i < 8; i++)
            {
                (Controls["groupBox" + (i + 1)] as GroupBox).Text = $"Канал {i + 1}";
            }

            ReadValue[] qwe = new ReadValue[8];
            for (int i = 0; i < 8; i++)
            {
                qwe[i].Value = 999;
            }
            T_Prod = qwe;
        }

        private void GroupBox_Paint(object sender, PaintEventArgs p)
        {
            GroupBox box = (GroupBox)sender;
            Brush borderBrush = new SolidBrush(Color.Black);
            Pen borderPen = new Pen(borderBrush);
            SizeF strSize = p.Graphics.MeasureString(box.Text, box.Font);
            Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                           box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                           box.ClientRectangle.Width - 1,
                                           box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

            p.Graphics.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
            //Right
            p.Graphics.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
            //Bottom
            p.Graphics.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
            //Top1
            p.Graphics.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
            //Top2
            p.Graphics.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            
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
