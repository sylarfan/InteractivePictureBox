using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private float zoomFactor = 1;

        private void Button1_Click(object sender, EventArgs e)
        {
            zoomFactor *= 1.2f;
            myPictureBox1.Zoom(zoomFactor, new Point(myPictureBox1.Width / 2, myPictureBox1.Height / 2));
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            zoomFactor /= 1.2f;
            myPictureBox1.Zoom(zoomFactor, new Point(myPictureBox1.Width / 2, myPictureBox1.Height / 2));
        }

        private void MyPictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            var point = myPictureBox1.GetImagePoint(e.Location);
            label1.Text = $"{point.X},{point.Y}";
        }
    }
}
