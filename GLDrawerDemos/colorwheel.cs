using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GLDrawer;

namespace GLDrawerDemos
{
    public delegate void setCol(Color col);
    public partial class colorwheel : Form
    {
        public setCol set;
        public Color color { set { colorWheel1.Color = value; } get { 
                    Color temp = (Color)colorWheel1.Color + (int)((lightnessColorSlider.Value - 50) * 1.5) * 3;
                temp.A = (int)alphaColorSlider.Value;
                return temp;
            } } 
        public colorwheel()
        {
            InitializeComponent();
            colorWheel1.ColorChanged += delegate { panel1.BackColor = color; set(color); };
            button1.Click += delegate { DialogResult = DialogResult.OK; };
            button2.Click += delegate { DialogResult = DialogResult.Cancel; };
            lightnessColorSlider.Value = 50;
            lightnessColorSlider.ValueChanged += delegate { panel1.BackColor = color; set(color); };
            alphaColorSlider.Value = 255;
            alphaColorSlider.ValueChanged += delegate { panel1.BackColor = color; set(color); };
        }

    }
}
