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
        public static GLMouseEvent callb;//rename and move
        public static GLMouseEvent mouseMovedCallback;

        public static GLCanvas can;
        public static GLCanvas previewCan;

        [STAThread]
        static void Main(string[] args)
        {
            //can = new GLCanvas();
            //  can.Add(new Text(new vec2(200, 200), 20, "Fancy Text", new Color(255, 255)));
            // can.AddCenteredEllipse(100, 100, 100, 100, Color.White);

            Application.EnableVisualStyles();

            TestForm tform = new TestForm();

            can = new GLCanvas(tform, tform.surface);

            previewCan = new GLCanvas(tform, tform.preview, BackColor: tform.BackColor);


            tform.updatePreview();

            can.MouseLeftClick += Can_MouseLeftClick;
            can.MouseMove += Can_MouseMove;
            Application.Run(tform);

            Console.WriteLine("test");
            Console.ReadKey();
        }

        private static void Can_MouseMove(vec2 Position, GLCanvas Canvas)
        {
            mouseMovedCallback.Invoke(Position, Canvas);
        }

        private static void Can_MouseLeftClick(vec2 Position, GLCanvas Canvas)
        {            
            callb.Invoke(Position, Canvas);
        }
    }
}
