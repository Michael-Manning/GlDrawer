using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GLDrawer;

namespace GLDrawerDemos
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
            //demos.fastRemoval();//demos.backBufferShapes();
          //  spaceGame.spaceGame.run();
            //platformGame.SimplePlatformer.run();
           // platformGame.AdvancedPlatformer.run();
           GLDrawerDemos.levelEditorProgram.run();

            //demos.IntersectTest();
            //demos.PhysicsTest();                 

            //Application.EnableVisualStyles();
            //TestForm tform = new TestForm();

            //can = new GLCanvas(tform, tform.surface, BackColor: Color.LightGray, debugMode: true);
            //previewCan = new GLCanvas(tform, tform.preview, BackColor: tform.BackColor);

            //tform.updatePreview();
            //can.MouseLeftClick += Can_MouseLeftClick;
            //can.MouseMove += Can_MouseMove;
            //Application.Run(tform);

            //can = new GLCanvas();
            //can.AddCenteredEllipse(0, 0, 200, 200, Color.White, 10, Color.Red);

            //Console.ReadKey();
            //can.Close();

            //Console.ReadKey();

            //can = new GLCanvas();


            //can.AddCenteredEllipse(0, 0, 200, 200, Color.White, 10, Color.Red);
            //can.AddCenteredText("test", 40, Color.Black);

            //can.Update += delegate { if (can.GetKey('a')) can.CameraPosition += new vec2(5, 0); };
            //can.CanvasResized += Can_CanvasResized;

            Console.ReadKey();


        }

        private static void Can_CanvasResized(int Width, int Height, GLCanvas Canvas)
        {
            throw new NotImplementedException();
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
