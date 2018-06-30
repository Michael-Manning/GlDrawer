using GLDrawer;
using Cyotek.Windows.Forms;

namespace GLDrawerTests
{
    partial class colorwheel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.colorWheel1 = new Cyotek.Windows.Forms.ColorWheel();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.alphaColorSlider = new Cyotek.Windows.Forms.RgbaColorSlider();
            this.lightnessColorSlider = new Cyotek.Windows.Forms.LightnessColorSlider();
            this.SuspendLayout();
            // 
            // colorWheel1
            // 
            this.colorWheel1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.colorWheel1.Location = new System.Drawing.Point(-1, 0);
            this.colorWheel1.Name = "colorWheel1";
            this.colorWheel1.Size = new System.Drawing.Size(260, 259);
            this.colorWheel1.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(248, 190);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(90, 36);
            this.panel1.TabIndex = 2;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(218, 232);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(286, 232);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(52, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // alphaColorSlider
            // 
            this.alphaColorSlider.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.alphaColorSlider.BackColor = System.Drawing.SystemColors.Control;
            this.alphaColorSlider.Channel = Cyotek.Windows.Forms.RgbaChannel.Alpha;
            this.alphaColorSlider.Color = System.Drawing.Color.Transparent;
            this.alphaColorSlider.Location = new System.Drawing.Point(305, 0);
            this.alphaColorSlider.Name = "alphaColorSlider";
            this.alphaColorSlider.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.alphaColorSlider.Size = new System.Drawing.Size(37, 180);
            this.alphaColorSlider.TabIndex = 5;
            // 
            // lightnessColorSlider
            // 
            this.lightnessColorSlider.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lightnessColorSlider.Location = new System.Drawing.Point(265, 0);
            this.lightnessColorSlider.Name = "lightnessColorSlider";
            this.lightnessColorSlider.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.lightnessColorSlider.Size = new System.Drawing.Size(34, 180);
            this.lightnessColorSlider.TabIndex = 6;
            // 
            // colorwheel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(354, 267);
            this.Controls.Add(this.lightnessColorSlider);
            this.Controls.Add(this.alphaColorSlider);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.colorWheel1);
            this.Name = "colorwheel";
            this.Text = "colorwheel";
            this.ResumeLayout(false);

        }

        #endregion
        private ColorWheel colorWheel1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private RgbaColorSlider alphaColorSlider;
        private LightnessColorSlider lightnessColorSlider;
    }
}