using MetroFramework.Forms;
using Network;
using System;
using System.Windows.Forms;

namespace Client
{
    public partial class AddSensor : MetroForm
    {
        public Sensor sensor;
        public COM com;
        public AddSensor(string srv, COM _com)
        {
            InitializeComponent();
            com = _com;
            metroComboBox1.Items.AddRange(Enum.GetNames(typeof(TypeSensor)));
            metroLabel3.Text += $": {srv}";
            metroLabel4.Text += $": {_com._COM}";
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            sensor = new Sensor
            {
                TypeSensor = (TypeSensor)metroComboBox1.SelectedIndex,
                _COMGUID = com._Guid,
                StartAdress = Convert.ToInt32(metroTextBox1.Text)
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
