using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GLDrawer;
using System.Windows.Forms;

namespace GLDrawerTests
{
    
    public static class Program
    {
        public static GLMouseEvent callb;
        public static GLCanvas can;
        public static GLCanvas previewCan;
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            TestForm tform = new TestForm();          
            can = new GLCanvas(tform, tform.surface);
            
            previewCan = new GLCanvas(tform, tform.preview, BackColor: tform.BackColor);
            tform.updatePreview();

            can.MouseLeftClick += Can_MouseLeftClick;
            Application.Run(tform);
            Console.ReadKey();          
            System.Environment.Exit(0);
        }

        private static void Can_MouseLeftClick(vec2 Position, GLCanvas Canvas)
        {
            callb.Invoke(Position, Canvas);
        }
    }
}
