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

namespace GLDrawerDemos
{
    public partial class TestForm : Form
    {
        colorwheel wheel = new colorwheel(); //color picker

        //references for the preview of each shape
        Polygon prevPoly;
        Polygon prevLine; //the line is represented with a rect for simplicity
        int clicknum = 0; //lines require 2 clicks
        Sprite prevSprite;
        Text prevText;
        JustificationType selectedJustification = JustificationType.Center;
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
            demos.mouseClickCallback = DrawPoly;
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
            demos.mouseMovedCallback = UpdateLineOverlay;
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

            leftRadio.CheckedChanged += delegate {
                if (leftRadio.Checked)
                {
                    selectedJustification = JustificationType.Left;
                    recreateText();
                }};
            centRadio.CheckedChanged += delegate {
                if (centRadio.Checked)
                {
                    selectedJustification = JustificationType.Center;
                    recreateText();
                }
            };
            rightRadio.CheckedChanged += delegate {
                if (rightRadio.Checked)
                {
                    selectedJustification = JustificationType.Right;
                    recreateText();
                } };

            comboBox2.Items.AddRange(fonts);
            comboBox2.SelectedIndex = fonts.ToList().IndexOf("times");
            comboBox2.SelectedIndexChanged += delegate { recreateText(); };
        }

        void recreateText()
        {
            demos.previewCan.Remove(prevText);
            prevText = demos.previewCan.Add(new Text(vec2.Zero, richTextBox1.Text, 20, Color.Invisible, selectedJustification, font: (string)("c:\\windows\\fonts\\" + comboBox2.SelectedItem + ".ttf"))) as Text;
            selectText();
        }

        public void updatePreview() {
            if (!init)
            {
                init = true;

                prevLine = demos.previewCan.AddCenteredRectangle(0, 0, 1000, 100, Color.Invisible, trackBar2.Value, Color.Invisible);
                prevPoly = demos.previewCan.AddCenteredPolygon(0, 0, 210, 210, 1, Color.Invisible, trackBar2.Value, Color.Invisible);
                prevText = demos.previewCan.Add(new Text(new vec2(0,0), richTextBox1.Text, 20, Color.Invisible)) as Text;
                selectCirc();                                                                                                            
                                                                                                                                         
                return;
            }
            hidePreview();

            Polygon tp = selected as Polygon;
            if(tp != null)
            {
                tp.FillColor = panel5.BackColor;
                tp.BorderColor = panel6.BackColor;
                tp.BorderWidth = trackBar2.Value;
                if (checkBox1.Checked)
                    tp.FillColor = Color.Rainbow;
                if (checkBox2.Checked)
                    tp.BorderColor = Color.Rainbow;
            }
            Text tt = selected as Text;
            if (tt != null)
            {
                prevText.Color = panel5.BackColor;
                prevText.Height = trackBar1.Value;
                tt.Color = panel5.BackColor;
                if (checkBox1.Checked)
                    tt.Color = Color.Rainbow;
            }

            selected.Scale = new vec2(trackBar1.Value);
            if (selected == prevLine)
                selected.Scale = new vec2(1000, trackBar1.Value);
            if(selected != prevLine)
                selected.Angle = trackBar3.Value / 360f * (float)(Math.PI * 2f);
        }
        void hidePreview()
        {
            if (prevSprite != null)
                prevSprite.Hidden = prevSprite != selected; 
            prevLine.Hidden = prevLine != selected;
            prevPoly.Hidden = prevPoly != selected;
            prevText.Hidden = prevText != selected;
        }

        Action lastPolySelection; //used for recalling last selection when switching tabs in th UI
        void selectRect()
        {
            prevPoly.SideCount = 4;
            selected = prevPoly;
            demos.mouseClickCallback = DrawPoly;
            updatePreview();
            lastPolySelection = selectRect;
        }
        void selectCirc()
        {
            prevPoly.SideCount = 1;
            selected = prevPoly;
            demos.mouseClickCallback = DrawPoly;
            updatePreview();
            lastPolySelection = selectCirc;
        }
        void selectPoly()
        {
            selected = prevPoly;
            demos.mouseClickCallback = DrawPoly;
            updatePreview();
            lastPolySelection = selectPoly;
        }
        void selectLine()
        {
            selected = prevLine;
            demos.mouseClickCallback = DrawLine;
            updatePreview();
            lastPolySelection = selectLine;
        }
        void selectSprite()
        {
            demos.mouseClickCallback = DrawSprite;
            selected = prevSprite;
            updatePreview();
        }
        void chooseSprite()
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            //remove previous sprite from the preview canvas if there is one
            if (prevSprite != null)
                demos.previewCan.Remove(prevSprite);
            textBox1.Text = openFileDialog.FileName;
            prevSprite = demos.previewCan.AddCenteredSprite(openFileDialog.FileName, 0, 0, trackBar1.Value, trackBar1.Value);
        }
        void selectText()
        {
            demos.mouseClickCallback = DrawText;
            selected = prevText;
            updatePreview();
        }

        //depending on the selected drawmode, One of these functions are called after a click through mouseClickCallback
        void DrawSprite(vec2 pos, GLCanvas can)
        {
            Sprite spr = can.AddCenteredSprite(prevSprite.FilePath, pos.x, pos.y, prevSprite.Scale.x, prevSprite.Scale.y, prevSprite.Angle);
            shapes.Add(spr);
            lbAdd("Sprite (" + spr.Position.x + ", " + spr.Position.y + ")");
        }
        void DrawPoly(vec2 pos, GLCanvas ca)
        {
            Polygon pol = demos.can.AddCenteredPolygon(pos.x, pos.y, prevPoly.Scale.x, prevPoly.Scale.y, prevPoly.SideCount, prevPoly.FillColor, prevPoly.BorderWidth, prevPoly.BorderColor, prevPoly.Angle);
            shapes.Add(pol);
            lbAdd("Polygon (" + pol.Position.x + ", " + pol.Position.y + ")");
        }
        void DrawText(vec2 pos, GLCanvas ca)
        {
            Text txt = demos.can.Add(new Text(pos, prevText.Body,  prevText.Height, prevText.Color, prevText.Justification, prevText.Font, prevText.Angle, 0)) as Text;
            shapes.Add(txt);
            lbAdd("Text (" + txt.Position.x + ", " + txt.Position.y + ")");
        }
        Line tLine; //persistant referance for an in progress line as they require 2 clicks
        void DrawLine(vec2 pos, GLCanvas ca)
        {
            if(clicknum == 0)
            {
                // tLine = new Line(pos, pos, prevLine.Scale.y, prevLine.FillColor, prevLine.BorderWidth, prevLine.BorderColor);//new Line(pos, pos + 50f, 15f, Color.White); //replace with eventual canvas return function
                //demos.can.Add(tLine);
                tLine = demos.can.AddLine(pos.x, pos.y, pos.x, pos.y, prevLine.Scale.y, prevLine.FillColor, prevLine.BorderWidth, prevLine.BorderColor);
            }
            if(clicknum == 1)
            {
                tLine.End = pos;
                Line finalLine = new Line(tLine.Start, tLine.End, tLine.Thickness, tLine.FillColor, tLine.BorderWidth, tLine.BorderColor);
                shapes.Add(finalLine);
                demos.can.Remove(tLine);
                demos.can.Add(finalLine);
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

        //remove from canvas button
        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;
            Shape toRemove = shapes[listBox1.SelectedIndex];
            demos.can.Remove(toRemove);
            shapes.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
           // demos.can.Refresh();
        }

        //order object forward button
        private void btnUp_Click(object sender, EventArgs e)
        {
            int i = listBox1.SelectedIndex;
            if (listBox1.Items.Count <= i+1 || i == -1)
                return;
          //  demos.can.SwapDrawOrder(i, i + 1);      
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

           // demos.can.SwapDrawOrder(i, i - 1);


            
                string temp = listBox1.Items[i].ToString();
                listBox1.Items[i] = listBox1.Items[i - 1];
                listBox1.Items[i - 1] = temp;

              shapes[i].DrawIndex++;
              shapes[i - 1].DrawIndex--;
                //Shape temp = shapes[i];
                //shapes[i] = shapes[i - 1];
                //shapes[i - 1] = temp;

            listBox1.SelectedIndex = i - 1;
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
           //r.Position = demos.can.mo
        }
    }
}
