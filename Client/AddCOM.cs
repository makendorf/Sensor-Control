using MetroFramework.Forms;
using Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    
    public partial class AddCOM : MetroForm
    {
        public COM com;
        public Guid Workflow;
        public AddCOM()
        {
            InitializeComponent();
            metroComboBox1.Items.AddRange(Enum.GetNames(typeof(COMport)));
            metroComboBox2.Items.AddRange(Enum.GetNames(typeof(TypeAdapter)));
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            com = new COM
            {
                _COM = (COMport)metroComboBox1.SelectedIndex,
                _TimerUpdate = Convert.ToInt32(metroTextBox1.Text),
                _TypeAdapter = (TypeAdapter)metroComboBox2.SelectedIndex,
                _GuidWorkFlow = Workflow
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }

}
