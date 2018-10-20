using System.Windows.Forms;

namespace GLDrawerDemos
{
    public partial class levelDialog : Form
    {
        public int gridWidth => (int)numericUpDown1.Value;
        public int gridHeight => (int)numericUpDown2.Value;
        public levelDialog()
        {
            InitializeComponent();
            button1.Click += (s, e) => DialogResult = DialogResult.OK;
            button2.Click += (s, e) => DialogResult = DialogResult.Cancel;
        }
    }
}
