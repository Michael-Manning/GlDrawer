using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LevelEditor
{
    public partial class LevelDialog : Form
    {
        public int gridWidth => (int)numericUpDown1.Value;
        public int gridHeight => (int)numericUpDown2.Value;

        public LevelDialog()
        {
            InitializeComponent();
            button1.Click += (s, e) => DialogResult = DialogResult.OK;
            button2.Click += (s, e) => DialogResult = DialogResult.Cancel;
        }
    }
}
