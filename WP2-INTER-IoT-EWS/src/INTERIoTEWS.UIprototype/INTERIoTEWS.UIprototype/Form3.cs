using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace INTERIoTEWS.UIprototype
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                JObject temp = JObject.Parse(textBox1.Text);
                textBox1.Text = temp.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                JObject temp = JObject.Parse(textBox1.Text);
                textBox1.Text = temp.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
