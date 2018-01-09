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

namespace CombineTest
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBox2.Text = ofd.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream(textBox1.Text, FileMode.Append));
            BinaryReader br = new BinaryReader(new FileStream(textBox2.Text, FileMode.Open));
            byte[] buffer = new byte[256];
            buffer = br.ReadBytes(256);
            while (buffer.Length > 0)
            {
                bw.Write(buffer);
                buffer = br.ReadBytes(256);
            }
            bw.Flush();
            bw.Close();
            br.Close();
            MessageBox.Show("Finished");
        }
    }
}
