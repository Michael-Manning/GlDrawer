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
using System.IO;

namespace GLDrawerTests
{
    public partial class TestForm : Form
    {
        colorwheel wheel = new colorwheel(); //color picker
        textForm tform = new textForm();

        //references for the preview of each shape
        Ellipse prevElip;
        Rectangle prevRect;
        Polygon prevPoly;
        Rectangle prevLine; //the line is represented with a rect for simplicity
        int clicknum = 0; //lines require 2 clicks
        Sprite prevSprite;
        Text prevText;
        Shape selected;

        bool init = false;
        List<Shape> shapes = new List<Shape>(); //reference list for drawn shapes
        public Panel surface { get { return panel3; } } //main canvas panel
        public Panel preview { get { return panel4; } } //small preview panel
        List<string> fonts = new List<string>();

        vec2 previewCenter { get { return new vec2(panel4.Size.Width / 2, panel4.Size.Height / 2); } }

        public TestForm()
        {
            InitializeComponent();
            Program.callb = DrawCirc;
            rbElip.CheckedChanged += delegate { if (rbElip.Checked) selectCirc(); };
            rbRect.CheckedChanged += delegate { if (rbRect.Checked) selectRect(); };
            rbLine.CheckedChanged += delegate { if (rbLine.Checked) selectLine(); };
            rbPoly.CheckedChanged += delegate { if (rbPoly.Checked) selectPoly(); numericUpDown1.Enabled = rbPoly.Checked; };
            button2.Click += delegate { chooseSprite(); selectSprite(); };
            numericUpDown1.ValueChanged += delegate { prevPoly.SideCount = (int)numericUpDown1.Value; };
            richTextBox1.TextChanged += delegate { if (prevText != null) prevText.Body = richTextBox1.Text; };

            trackBar1.ValueChanged += delegate { updatePreview(); trackBar2.Maximum = trackBar1.Value / 2; };
            trackBar2.ValueChanged += delegate { updatePreview(); };
            trackBar3.ValueChanged += delegate { updatePreview(); };
            checkBox1.CheckedChanged += delegate { updatePreview(); };
            checkBox2.CheckedChanged += delegate { updatePreview(); };
            trackBar2.Maximum = trackBar1.Value / 2;
            wheel.StartPosition = FormStartPosition.Manual;

            panel5.Click += delegate
            {
                Color temp = panel5.BackColor;
                wheel.set = x => { panel5.BackColor = x; updatePreview(); };
                wheel.color = panel5.BackColor;
                if (wheel.ShowDialog() == DialogResult.Cancel)
                    panel5.BackColor = temp;
                updatePreview();
            };
            panel6.Click += delegate
            {
                Color temp = panel6.BackColor;
                wheel.set = x => { panel6.BackColor = x; updatePreview(); };
                wheel.color = panel6.BackColor;
                if (wheel.ShowDialog() == DialogResult.Cancel)
                    panel6.BackColor = temp;
                updatePreview();
            };
            Program.mouseMovedCallback = UpdateLineOverlay;
            tabControl1.SelectedIndexChanged += delegate
            {
                if (tabControl1.SelectedIndex == 0)
                    lastPolySelection();
                if (tabControl1.SelectedIndex == 1 && prevSprite != null)
                    selectSprite();
                if (tabControl1.SelectedIndex == 2)
                    selectText();
            };

            //list of all ttf filepaths
            string[] fonts = Directory.GetFiles(@"C:\windows\fonts\").Where(x => x.ToLower().Contains(".ttf")).ToArray();
            for (int i = 0; i < fonts.Length; i++)
            {
                fonts[i] = fonts[i].Substring(17, fonts[i].Length - 21);
            }

            comboBox2.Items.AddRange(fonts);
            comboBox2.SelectedIndex = fonts.ToList().IndexOf("times");
            comboBox2.SelectedIndexChanged += delegate
            {
                Program.previewCan.RemoveShape(prevText);
                prevText = Program.previewCan.Add(new Text(new vec2(0, 0), 20, richTextBox1.Text, Color.Invisible, font: (string)("c:\\windows\\fonts\\" + comboBox2.SelectedItem + ".ttf"))) as Text;
                selectText();
            };
        }
        public void updatePreview() {
            if (!init)
            {
                init = true;

                prevElip = Program.previewCan.AddCenteredEllipse(110, 110, 210, 210, panel5.BackColor, trackBar2.Value, panel6.BackColor); 
                prevRect = Program.previewCan.AddCenteredRectangle(110, 110, 210, 210, Color.Invisible, trackBar2.Value, Color.Invisible);
                prevLine = Program.previewCan.AddCenteredRectangle(110, 110, 1000, 100, Color.Invisible, trackBar2.Value, Color.Invisible);
                prevPoly = Program.previewCan.AddCenteredPolygon(110, 110, 210, 210, (int)numericUpDown1.Value, Color.Invisible, trackBar2.Value, Color.Invisible);
                prevText = Program.previewCan.Add(new Text(new vec2(0,0), 20, richTextBox1.Text, Color.Invisible)) as Text;
                selectCirc();                                                                                                            
                                                                                                                                         
                return;
            }
            hidePreview();
            selected.Position = selected == prevText ? new vec2(0, 217 - (int)trackBar1.Value/2) : previewCenter;
            selected.FillColor = panel5.BackColor;
            selected.BorderColor = panel6.BackColor;
            selected.BorderWidth = trackBar2.Value;         
            selected.Scale = new vec2(trackBar1.Value);
            if (selected == prevLine)
                selected.Scale = new vec2(1000, trackBar1.Value);
            if(selected != prevLine)
                selected.Angle = trackBar3.Value / 360f * (float)(Math.PI * 2f);

            if (checkBox1.Checked)
                selected.FillColor = Color.Rainbow;
            if (checkBox2.Checked)
                selected.BorderColor = Color.Rainbow;
        }
        void hidePreview()
        {
            if (prevSprite != null)
                prevSprite.Hidden = prevSprite != selected; 
            prevRect.Hidden = prevRect != selected;
            prevElip.Hidden = prevElip != selected;
            prevLine.Hidden = prevLine != selected;
            prevPoly.Hidden = prevPoly != selected;
            prevText.Hidden = prevText != selected;
        }

        Action lastPolySelection; //used for recalling last selection when switching tabs in th UI
        void selectRect()
        {
            selected = prevRect;
            Program.callb = DrawRect;
            updatePreview();
            lastPolySelection = selectRect;
        }
        void selectCirc()
        {
            selected = prevElip;
            Program.callb = DrawCirc;
            updatePreview();
            lastPolySelection = selectCirc;
        }
        void selectPoly()
        {
            selected = prevPoly;
            Program.callb = DrawPoly;
            updatePreview();
            lastPolySelection = selectPoly;
        }
        void selectLine()
        {
            selected = prevLine;
            Program.callb = DrawLine;
            updatePreview();
            lastPolySelection = selectLine;
        }
        void selectSprite()
        {
            Program.callb = DrawSprite;
            selected = prevSprite;
            updatePreview();
        }
        void chooseSprite()
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            //remove previous sprite from the preview canvas if there is one
            if (prevSprite != null)
                Program.previewCan.RemoveShape(prevSprite);
            textBox1.Text = openFileDialog.FileName;
            prevSprite = Program.previewCan.AddCenteredSprite(openFileDialog.FileName, 110, 110, trackBar1.Value, trackBar1.Value, Color.White);
        }
        void selectText()
        {
            Program.callb = DrawText;
            selected = prevText;
            updatePreview();
        }

        //depending on the selected drawmode, One of these functions are called after a click through callb
        void DrawSprite(vec2 pos, GLCanvas can)
        {
            Sprite spr = can.AddCenteredSprite(prevSprite.FilePath, pos.x, pos.y, prevSprite.Scale.x, prevSprite.Scale.y, prevSprite.FillColor, prevSprite.BorderWidth, prevSprite.BorderColor, prevSprite.Angle);
            shapes.Add(spr);
            lbAdd("Sprite (" + spr.Position.x + ", " + spr.Position.y + ")");
        }
        void DrawRect(vec2 pos, GLCanvas can)
        {
            Rectangle rct = Program.can.AddCenteredRectangle(pos.x, pos.y, prevRect.Scale.x, prevRect.Scale.y, prevRect.FillColor, prevRect.BorderWidth, prevRect.BorderColor, prevRect.Angle);
            shapes.Add(rct);
            lbAdd("Rectangle (" + rct.Position.x + ", " + rct.Position.y + ")");
        }
        void DrawCirc(vec2 pos, GLCanvas ca)
        {
            Ellipse elp = Program.can.AddCenteredEllipse(pos.x, pos.y, prevElip.Scale.x, prevElip.Scale.y, prevElip.FillColor, prevElip.BorderWidth, prevElip.BorderColor, prevElip.Angle);
            shapes.Add(elp);
            lbAdd("Ellipse (" + elp.Position.x + ", " + elp.Position.y + ")");
        }
        void DrawPoly(vec2 pos, GLCanvas ca)
        {
            Polygon pol = Program.can.AddCenteredPolygon(pos.x, pos.y, prevPoly.Scale.x, prevPoly.Scale.y, prevPoly.SideCount, prevPoly.FillColor, prevPoly.BorderWidth, prevPoly.BorderColor, prevPoly.Angle);
            shapes.Add(pol);
            lbAdd("Polygon (" + pol.Position.x + ", " + pol.Position.y + ")");
        }
        void DrawText(vec2 pos, GLCanvas ca)
        {
            Text txt = Program.can.Add(new Text(pos, prevText.Scale.y, prevText.Body, prevText.FillColor, angle: prevText.Angle, font: prevText.Font)) as Text;
            shapes.Add(txt);
            lbAdd("Text (" + txt.Position.x + ", " + txt.Position.y + ")");
        }
        Line tLine; //persistant referance for an in progress line as they require 2 clicks
        void DrawLine(vec2 pos, GLCanvas ca)
        {
            if(clicknum == 0)
            {
                // tLine = new Line(pos, pos, prevLine.Scale.y, prevLine.FillColor, prevLine.BorderWidth, prevLine.BorderColor);//new Line(pos, pos + 50f, 15f, Color.White); //replace with eventual canvas return function
                //Program.can.Add(tLine);
                tLine = Program.can.AddLine(pos.x, pos.y, pos.x, pos.y, prevLine.Scale.y, prevLine.FillColor, prevLine.BorderWidth, prevLine.BorderColor);
            }
            if(clicknum == 1)
            {
                tLine.End = pos;
                Line finalLine = new Line(tLine.Start, tLine.End, tLine.Thickness, tLine.FillColor, tLine.BorderWidth, tLine.BorderColor);
                shapes.Add(finalLine);
                Program.can.RemoveShape(tLine);
                Program.can.Add(finalLine);
                lbAdd("Line (" + finalLine.Start.x + ", " + finalLine.Start.y + ")-(" + finalLine.End.x + ", " + finalLine.End.y + ")");
                clicknum = 0;
                return;
            }
            clicknum++;
        }
        void UpdateLineOverlay(vec2 pos, GLCanvas ca)
        {
            if (tLine != null)
                tLine.End = pos;
        }

        //adds items to the listbox thread independantly
        void lbAdd(string text)
        {
            Invoke(new Action(() =>
            {
                listBox1.Items.Add(text);
            }));
        }

        //remove from canvasa button
        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;
            shapes.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            Program.can.Refresh();
        }

        //order object forward button
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

        //order object backward button
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
