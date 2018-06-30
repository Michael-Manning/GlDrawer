using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Cyotek.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GLDrawer;

namespace GLDrawerTests
{
    public partial class TestForm : Form
    {
        colorwheel wheel = new colorwheel();
        textForm tform = new textForm();
        Ellipse e, EllipC;
        Rectangle r, Rectc;
        Sprite s;
        Shape selected;
        bool init = false;
        List<Shape> shapes = new List<Shape>();
        public TestForm()
        {
            InitializeComponent();
            Program.callb = DrawCirc;
            panel1.Click += delegate { selectRect(); };
            label1.Click += delegate { selectRect(); };
            panel2.Click += delegate { selectCirc(); };
            label2.Click += delegate { selectCirc(); };
            panel7.Click += delegate { selectSprite(); };
            label7.Click += delegate { selectSprite(); };
            panel8.Click += delegate { selectText(); };
            label8.Click += delegate { selectText(); };
            trackBar1.ValueChanged += delegate { updatePreview(); trackBar2.Maximum = trackBar1.Value / 2; };
            trackBar2.ValueChanged += delegate { updatePreview(); };
            wheel.StartPosition = FormStartPosition.Manual;
            panel5.Click += delegate {
                wheel.Location = new System.Drawing.Point(0, 0);
                Color temp = panel5.BackColor;
                wheel.set = x => { panel5.BackColor = x; updatePreview(); };
                wheel.color = panel5.BackColor;
                if (wheel.ShowDialog() == DialogResult.Cancel)
                    panel5.BackColor = temp;
                updatePreview();
            };
            panel6.Click += delegate
            {
                wheel.Location = new System.Drawing.Point(0, 0);
                Color temp = panel6.BackColor;
                wheel.set = x => { panel6.BackColor = x; updatePreview(); };
                wheel.color = panel6.BackColor;
                if (wheel.ShowDialog() == DialogResult.Cancel)
                    panel6.BackColor = temp;
                updatePreview();
            }; 
        }
        public void updatePreview() {
            if (!init)
            {
                init = true;
                e = Program.previewCan.AddCenteredEllipse(110, 110, 210, 210, panel5.BackColor, trackBar2.Value, panel6.BackColor);
                EllipC = Program.previewCan.AddCenteredEllipse(110, 110, 210, 210, panel5.BackColor, trackBar2.Value, panel6.BackColor);
                r = Program.previewCan.AddCenteredRectangle(110, 110, 210, 210, Color.Invisible, trackBar2.Value, Color.Invisible);
                Rectc = Program.previewCan.AddCenteredRectangle(110, 110, 210, 210, Color.Invisible, trackBar2.Value, Color.Invisible);
                selected = e;
                return;
            }
            hidePreview();
            selected.Position = new vec2(110, 110);
            selected.FillColor = panel5.BackColor;
            selected.BorderColor = panel6.BackColor;
            selected.Scale = new vec2(trackBar1.Value);
            selected.BorderWidth = trackBar2.Value;
        }
        void hidePreview()
        {
            if (s != null && selected != s)
                Program.previewCan.RemoveShape(s);
            r.Hidden = selected != r;
            e.Hidden = e != selected;

        }
        public Panel surface { get { return panel3; } }
        public Panel preview { get { return panel4; } }
        bool rect = false;
        void selectRect()
        {
            selected = r;
            Program.callb = DrawRect;
            panel1.BorderStyle = BorderStyle.Fixed3D;
            panel1.BackColor = Color.Yellow;
            panel2.BorderStyle = BorderStyle.None;
            panel2.BackColor = System.Drawing.Color.DarkGray;
            panel7.BorderStyle = BorderStyle.None;
            panel7.BackColor = System.Drawing.Color.DarkGray;
            updatePreview();
            e.Hidden = false;
        }
        void selectCirc()
        {
            selected = e;
            Program.callb = DrawCirc;
            panel2.BorderStyle = BorderStyle.Fixed3D;
            panel2.BackColor = Color.Yellow;
            panel1.BorderStyle = BorderStyle.None;
            panel1.BackColor = System.Drawing.Color.DarkGray;
            panel7.BorderStyle = BorderStyle.None;
            panel7.BackColor = System.Drawing.Color.DarkGray;
            updatePreview();
            e.Hidden = false;
        }
        void selectSprite()
        {
            panel7.BorderStyle = BorderStyle.Fixed3D;
            panel7.BackColor = Color.Yellow;
            panel1.BorderStyle = BorderStyle.None;
            panel1.BackColor = System.Drawing.Color.DarkGray;
            panel2.BorderStyle = BorderStyle.None;
            panel2.BackColor = System.Drawing.Color.DarkGray;

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            Program.callb = DrawSprite;
            s = Program.previewCan.AddCenteredSprite(openFileDialog.FileName, 110, 110, trackBar1.Value, trackBar1.Value, Color.White); ///new Sprite(openFileDialog.FileName, new vec2(), new vec2(trackBar1.Value), Color.White);
            selected = s;
            updatePreview();
        }
        void selectText()
        {
            tform.ShowDialog();
        }
        void DrawSprite(vec2 pos, GLCanvas can)
        {
            Sprite spr = can.AddCenteredSprite(s.FilePath, pos.x, pos.y, s.Scale.x, s.Scale.y, s.FillColor, s.BorderWidth, s.BorderColor);
            shapes.Add(spr);
            lbAdd("Sprite (" + spr.Position.x + ", " + spr.Position.y + ")");
        }
        void DrawRect(vec2 pos, GLCanvas can)
        {
            Rectangle rct = Program.can.AddCenteredRectangle(pos.x, pos.y, r.Scale.x, r.Scale.y, r.FillColor, r.BorderWidth, r.BorderColor);
            shapes.Add(rct);
            lbAdd("Rectangle (" + rct.Position.x + ", " + rct.Position.y + ")");
        }
        void DrawCirc(vec2 pos, GLCanvas ca)
        {
            Ellipse elp = Program.can.AddCenteredEllipse(pos.x, pos.y, e.Scale.x, e.Scale.y, e.FillColor, e.BorderWidth, e.BorderColor);
            shapes.Add(elp);
            lbAdd("Ellipse (" + elp.Position.x + ", " + elp.Position.y + ")");
        }
        void lbAdd(string text)
        {
            Invoke(new Action(() =>
            {
                listBox1.Items.Add(text);
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;
            shapes.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            Program.can.Refresh();
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            int i = listBox1.SelectedIndex;
            if (listBox1.Items.Count <= i+1 || i == -1)
                return;
            Program.can.SwapDrawOrder(i, i + 1);      
            {
                string temp = listBox1.Items[i].ToString();
                listBox1.Items[i] = listBox1.Items[i + 1];
                listBox1.Items[i + 1] = temp;
            }
            {
                Shape temp = shapes[i];
                shapes[i] = shapes[i + 1];
                shapes[i + 1] = temp;
            }
            listBox1.SelectedIndex = i + 1;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int i = listBox1.SelectedIndex;
            if (i < 1)
                return;
            Program.can.SwapDrawOrder(i, i - 1);
            {
                string temp = listBox1.Items[i].ToString();
                listBox1.Items[i] = listBox1.Items[i - 1];
                listBox1.Items[i - 1] = temp;
            }
            {
                Shape temp = shapes[i];
                shapes[i] = shapes[i - 1];
                shapes[i - 1] = temp;
            }
            listBox1.SelectedIndex = i - 1;
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
           //r.Position = Program.can.mo
        }
    }
}
