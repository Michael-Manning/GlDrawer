using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GLDrawer
{
    public partial class textForm : Form
    {
        List<string> fonts = new List<string>();
        public textForm()
        {
            InitializeComponent();
            tableLayoutPanel1.RowCount = 0;//System.Drawing.FontFamily.Families.Count();

            int i = 0;
            foreach (FontFamily font in System.Drawing.FontFamily.Families)
            {
                if (i > 10)
                    break;
                fonts.Add(font.Name);
                Label temp = new Label();
                temp.Text = font.Name;
                temp.Font = new Font(font, 10);
                temp.AutoSize = true;
                tableLayoutPanel1.Controls.Add(temp ,0, i);

                i++;
          //      tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            }
            foreach (RowStyle r in tableLayoutPanel1.RowStyles)
            {
                //r.SizeType = SizeType.Absolute;
                //r.Height = 400;
            }
        }
    }
}
