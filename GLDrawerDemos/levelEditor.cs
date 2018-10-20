using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using GLDrawer;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace GLDrawerDemos
{
    public partial class levelEditor : Form
    {
        public GLCanvas can;

        const int scale = 50;

        public const string workFile = "../../../data/other/mylevel.xml";
        public const string regFile = "../../../data/other/level.txt";
        public const string sprFile = "../../../data/other/sprites.txt";
        public const string opFile = "../../../data/other/oplevel.txt";
        public const string imgsPath = "C:/Users/Micha/Desktop/tileset/separate";

        Polygon[,] cgrid;
        Sprite[,] sgrid;

        List<Line> lines = new List<Line>();
        List<string> usedPaths = new List<string>();
        int selectedImage = 0;

        TileMap tilemap;

        Stack<history> undo = new Stack<history>();
        Stack<history> redo = new Stack<history>();
        struct history
        {
            public int x, y, index;
            public bool action;
            public bool mode;
            public history(int x, int y, bool action, bool mode, int index = 0)
            {
                this.x = x;
                this.y = y;
                this.action = action;
                this.mode = mode;
                this.index = index;
            }
        }

        public string[] impaths;

        public levelEditor()
        {
            InitializeComponent();
            can = new GLCanvas(this, panel1, new Color(70, 255));
            can.MouseLeftClick += Can_MouseLeftClick;
            can.MouseRightClick += Can_MouseRightClick;
            clearbtn.Click += (s, e) => clear();
            btnundo.Click += delegate { Undo(); };
            btnredo.Click += delegate { Redo(); };
            Thread.Sleep(100);
            BringToFront();
            can.MouseMove += Can_MouseMove;
            radioButton1.CheckedChanged += delegate { checkChanged(); };

            impaths = Directory.GetFiles(imgsPath);
            System.Drawing.Image[] imgs = new System.Drawing.Image[impaths.Length];
            for (int i = 0; i < impaths.Length; i++)
                imgs[i] = System.Drawing.Image.FromFile(impaths[i]);
            imageList1.Images.AddRange(imgs);

            listView1.View = View.LargeIcon;
            imageList1.ImageSize = new System.Drawing.Size(64, 64);
            listView1.LargeImageList = imageList1;

            for (int j = 0; j < imageList1.Images.Count; j++)
            {
                ListViewItem item = new ListViewItem();
                item.ImageIndex = j;
                listView1.Items.Add(item);
            }
            listView1.SelectedIndexChanged += delegate
            {
                int selectedCount = 0;
                listView1.Invoke((Action)delegate { selectedCount = listView1.SelectedIndices.Count; });
                if (selectedCount < 1)
                    return;
                listView1.Invoke((Action)delegate { selectedImage = listView1.SelectedIndices[0]; });
            };

            can.MouseScrolled += Can_MouseScrolled;
            can.MouseMove += Can_MouseMove1;
            enableBtns(false);
        }

        void Undo()
        {
            history h = undo.Pop();
            if(radioButton1.Checked != h.mode)
            {
                radioButton1.Checked = h.mode;
                radioButton2.Checked = !h.mode;
                checkChanged();
            }

            if (h.action)
                removeTile(h.x, h.y);
            else
            {
                selectedImage = h.index;
                addTile(h.x, h.y);
            }
            undo.Pop();
            if (undo.Count == 0)
                btnundo.Enabled = false;
            redo.Push(h);
            btnredo.Enabled = true;
        }
        void Redo()
        {
            history h = redo.Pop();
            if (radioButton1.Checked != h.mode)
            {
                radioButton1.Checked = h.mode;
                radioButton2.Checked = !h.mode;
                checkChanged();
            }
            if (!h.action)
                removeTile(h.x, h.y);
            else
            {
                selectedImage = h.index;
                addTile(h.x, h.y);
            }
 
            if (redo.Count == 0)
                btnredo.Enabled = false;
        }

        private void Can_MouseScrolled(int Delta, GLCanvas Canvas)
        {

            can.CamerZoom += Delta * 0.01f;
            if (can.CamerZoom < 0.1f)
                can.CamerZoom = 0.1f;
            lines.ForEach(l => l.Thickness = 6 / can.CamerZoom);
        }

        private void Can_MouseMove1(vec2 Position, GLCanvas Canvas)
        {
            if (can.MouseMiddleState)
                can.CameraPosition -= can.MouseDeltaPosition / can.CamerZoom;
        }

        void drawLines()
        {
            for (int j = 0; j < tilemap.Ytiles + 1; j++)
                lines.Add(can.Add(new Line(new vec2(0, j * scale), new vec2(tilemap.Xtiles * scale, j * scale), 6, new Color(100))) as Line);
            for (int j = 0; j < tilemap.Xtiles + 1; j++)
                lines.Add(can.Add(new Line(new vec2(j * scale, 0), new vec2(j * scale, tilemap.Ytiles * scale), 6, new Color(100))) as Line);
            lines.ForEach(l => l.DrawIndex = 10);
        }

        private void Can_MouseMove(vec2 Position, GLCanvas Canvas)
        {
            if (can.MouseLeftState)
                Can_MouseLeftClick(Position, Canvas);
            else if (can.MouseRightState)
                Can_MouseRightClick(Position, Canvas);
        }

        private void Can_MouseRightClick(vec2 Position, GLCanvas Canvas)
        {
            if (tilemap == null)
                return;

            vec2 pos = mouseGridPos(Position);
            if (pos.x < 0 || pos.x > tilemap.Xtiles - 1 || pos.y < 0 || pos.y > tilemap.Ytiles - 1)
                return;
            int y = (int)pos.y;
            int x = (int)pos.x;
            removeTile(x, y);
        }

        private void Can_MouseLeftClick(vec2 Position, GLCanvas Canvas)
        {
            if (tilemap == null)
                return;

            vec2 pos = mouseGridPos(Position);
            if (pos.x < 0 || pos.x > tilemap.Xtiles - 1 || pos.y < 0 || pos.y > tilemap.Ytiles - 1)
                return;
            int y = (int)pos.y;
            int x = (int)pos.x;
            addTile(x, y);
        }
        void addTile(int x, int y)
        {
            if (radioButton1.Checked && tilemap.CollisionGrid[x, y] == 0)
            {
                tilemap.CollisionGrid[x, y] = 1;
                undo.Push(new history(x, y, true, true));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true;  });
                optimize();
            }
            else if(radioButton2.Checked && tilemap.SpriteGrid[x,y] == 0)
            {
                tilemap.SpriteGrid[x, y] = selectedImage;
                if (sgrid[x, y] == null)
                {
                    if (!usedPaths.Contains(impaths[selectedImage]))
                    {
                        usedPaths.Add(impaths[selectedImage]);
                        tilemap.SpritePaths.Add(selectedImage, impaths[selectedImage]);
                    }
                    sgrid[x, y] = can.Add(new Sprite(impaths[selectedImage], new vec2(x, y) * scale + new vec2(scale / 2, -scale / 2 + scale * 1), new vec2(scale))) as Sprite;
                    undo.Push(new history(x, y, true, false, selectedImage));
                    btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                }
            }
        }
        void removeTile(int x, int y)
        {
            if (radioButton1.Checked && tilemap.CollisionGrid[x, y] != 0)
            {
                tilemap.CollisionGrid[x, y] = 0;
                undo.Push(new history(x, y, false, true));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimize();
            }
            else if (radioButton2.Checked && tilemap.SpriteGrid[x, y] != 0 && sgrid[x, y] != null)
            {
                undo.Push(new history(x, y, false, false, tilemap.SpriteGrid[x, y]));
                can.Remove(sgrid[x, y]);
                sgrid[x, y] = null;
                tilemap.SpriteGrid[x, y] = 0;             
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
            }
        }

        vec2 mouseGridPos(vec2 Position)
        {
            Position /= can.CamerZoom;
            Position += can.CameraPosition;

            vec2 pos = new vec2((int)Position.x / scale, (int)Position.y / scale);
            return pos;
        }

        void clear()
        {
            tilemap = new TileMap(tilemap.Xtiles, tilemap.Ytiles);
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (cgrid[i, j] != null)
                    {
                        can.Remove(cgrid[i, j]);
                        cgrid[i, j] = null;
                    }

                    if (sgrid[i, j] != null)
                    {
                        can.Remove(sgrid[i, j]);
                        sgrid[i, j] = null;
                    }
                }
            }
        }

        T[,] invertMatrix<T>(T[,] a)
        {
            T[,] m = new T[tilemap.Xtiles, tilemap.Ytiles];
            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    m[i, -j + tilemap.Ytiles - 1] = a[i, j];
            return m;
        }

        void optimize()
        {
            //need to clear the grid
            vec2[,] opgrid = new vec2[tilemap.Xtiles, tilemap.Ytiles];
            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    if (cgrid[i, j] != null)
                        can.Remove(cgrid[i, j]);

            int[,] grid = invertMatrix(tilemap.CollisionGrid);

            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    int x = 0, y = 0; //longest stretches
                    if (grid[i, j] == 1)
                    {

                        //find longest horizontal stretch
                        while (i + x < tilemap.Xtiles && grid[i + x, j] == 1 && opgrid[i + x, j].x == 0)
                        {
                            if (x > 0)
                                opgrid[i + x, j] = new vec2(-1, opgrid[i + x, j].y);
                            x++;
                        }

                        //if there was a horizontal stretch, check for a square area
                        int sqr = 0;
                        if (x > 1)
                        {
                            int l = 1, k = i;
                            bool doubleBreak = false;
                            while (l + j < tilemap.Ytiles)
                            {
                                while (k < i + x)
                                {
                                    if (grid[k, j + l] == 0)
                                        doubleBreak = true;
                                    k++;
                                }
                                k = i;
                                if (doubleBreak)
                                    break;
                                sqr = l + 1;
                                l++;
                            }
                        }
                        if (sqr > 1 && sqr < 100)
                        {
                            opgrid[i, j] = new vec2(x, sqr);
                            for (int l = j; l < j + sqr; l++)
                                for (int k = i; k < i + x; k++)
                                    if (l != j || k != i)
                                        opgrid[k, l] = new vec2(-1, -1);
                            continue;
                        }
                        if (x < 2 && opgrid[i, j].x != -1)
                        {
                            while (j + y < tilemap.Ytiles && grid[i, j + y] == 1 && opgrid[i, j + y].y == 0)
                            {
                                opgrid[i, j + y] = new vec2(opgrid[i, j + y].x, -1);
                                y++;
                            }
                            if (y > 0)
                                opgrid[i, j] = new vec2(1, y);
                            continue;
                        }
                        if (x > 0)
                            opgrid[i, j] = new vec2(x, 1);
                    }
                }
            }
            //pass 2
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (opgrid[i, j].y == 1 && opgrid[i, j].x < 0)
                        opgrid[i, j] = new vec2(opgrid[i, j].x, 0);
                    if (opgrid[i, j].y != 0 && opgrid[i, j].x == 1)
                        opgrid[i, j] = new vec2(0, opgrid[i, j].y);

                    if (opgrid[i, j].y < 0)
                        opgrid[i, j] = new vec2(opgrid[i, j].x, 0);
                    if (opgrid[i, j].x < 0)
                        opgrid[i, j] = new vec2(0, opgrid[i, j].y);
                }
            }
            //pass 3
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (opgrid[i, j].y == 0 && opgrid[i, j].x > 0)
                        opgrid[i, j] = new vec2(opgrid[i, j].x, 1);
                    if (opgrid[i, j].x == 0 && opgrid[i, j].y > 0)
                        opgrid[i, j] = new vec2(1, opgrid[i, j].y);
                }
            }

            tilemap.OpCollision = opgrid;

            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    float sx = tilemap.OpCollision[i, j].x;
                    float sy = tilemap.OpCollision[i, j].y;
                    if (tilemap.OpCollision[i, j] != vec2.Zero)
                        cgrid[i, j] = can.Add((new Polygon(new vec2(i + sx / 2f, -j - (sy / 2f) + tilemap.Ytiles) * scale, new vec2(scale * sx, scale * sy), 0, 4, Color.Hazard, 8, Color.Black))) as Polygon;
                }
            }
        }

        private void loadMap(object sender, EventArgs e)
        {
            XmlReader reader = XmlReader.Create(workFile);
            tilemap = new TileMap(reader);
            sgrid = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
            cgrid = new Polygon[tilemap.Xtiles, tilemap.Ytiles];
            drawLines();

            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (tilemap.SpriteGrid[i, j] > 0)
                        sgrid[i, j] = can.Add(new Sprite(tilemap.SpritePaths[tilemap.SpriteGrid[i, j]], new vec2(i, j + 1) * scale + new vec2(scale / 2, -scale / 2), new vec2(scale))) as Sprite;
                }
            }
            optimize();
            enableBtns(true);
            checkChanged(); //update opacity
            can.CameraPosition += (new vec2(tilemap.Xtiles, tilemap.Ytiles) * scale) / 2f;
            foreach (KeyValuePair<int, string> entry in tilemap.SpritePaths)
            {
                usedPaths.Add(entry.Value);
            }
        }
        private void writeMap(object sender, EventArgs e)
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.None;
            s.Indent = true;

            XmlWriter writer = XmlWriter.Create(workFile, s);
            tilemap.WriteXml(writer);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            levelDialog ld = new levelDialog();
            ld.ShowDialog();
            if (ld.DialogResult == DialogResult.OK)
            {
                tilemap = new TileMap(ld.gridWidth, ld.gridHeight);
                sgrid = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
                cgrid = new Polygon[tilemap.Xtiles, tilemap.Ytiles];
                drawLines();
                can.CameraPosition += new vec2(can.Width / 2, can.Height / 2);
                enableBtns(true);
            }
        }
        void enableBtns(bool enabled)
        {
            foreach (Control c in groupBox1.Controls)
                c.Enabled = enabled;
            button2.Enabled = true;

            btnundo.Enabled = enabled && (undo.Count != 0);
            btnredo.Enabled = enabled && (redo.Count != 0);
        }

        private void checkChanged()
        {
            if (radioButton2.Checked)
            {
                optimize();
                for (int j = 0; j < tilemap.Ytiles; j++)
                {
                    for (int i = 0; i < tilemap.Xtiles; i++)
                    {
                        if (cgrid[i, j] != null)
                        {
                            cgrid[i, j].DrawIndex = 2;
                            Color c = cgrid[i, j].FillColor;
                            cgrid[i, j].FillColor = new Color(c.R, c.G, c.B, 100);
                        }
                        if (sgrid[i, j] != null)
                        {
                            sgrid[i, j].DrawIndex = 0;
                            sgrid[i, j].Opacity = 1;
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < tilemap.Ytiles; j++)
                {
                    for (int i = 0; i < tilemap.Xtiles; i++)
                    {
                        if (cgrid[i, j] != null)
                        {
                            cgrid[i, j].DrawIndex = 0;
                            Color c = cgrid[i, j].FillColor;
                            cgrid[i, j].FillColor = Color.Hazard;
                        }
                        if (sgrid[i, j] != null)
                        {
                            sgrid[i, j].DrawIndex = 2;
                            sgrid[i, j].Opacity = 0.3f;
                        }
                    }
                }
            }
        }
    }
    public static class levelEditorProgram
    {
        [STAThread]
        public static void run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new levelEditor());
            Console.ReadKey();
        }
    }
    public class TileMap : IXmlSerializable
    {
        public int Xtiles, Ytiles;
        public int[,] CollisionGrid;
        public vec2[,] OpCollision;
        public int[,] SpriteGrid;
        public vec2[,] OpSprite;
        public Dictionary<int, string> SpritePaths = new Dictionary<int, string>();

        public TileMap(int width, int height)
        {
            Xtiles = width;
            Ytiles = height;
            CollisionGrid = new int[Xtiles, Ytiles];
            OpCollision = new vec2[Xtiles, Ytiles];
            SpriteGrid = new int[Xtiles, Ytiles];
            OpSprite = new vec2[Xtiles, Ytiles];
        }

        public TileMap(XmlReader r) => ReadXml(r);

        public TileMap(string filepah)
        {
            XmlReader reader = XmlReader.Create(filepah);
            ReadXml(reader);
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "tileMap")
                    {
                        Xtiles = int.Parse(reader.GetAttribute("Xtiles"));
                        Ytiles = int.Parse(reader.GetAttribute("Ytiles"));
                        CollisionGrid = new int[Xtiles, Ytiles];
                        OpCollision = new vec2[Xtiles, Ytiles];
                        SpriteGrid = new int[Xtiles, Ytiles];
                        OpSprite = new vec2[Xtiles, Ytiles];
                    }
                    if (reader.Name == "collisionMap")
                    {
                        //collision grid
                        reader.ReadToDescendant("grid");
                        reader.Read();
                        string gridString = reader.Value;
                        parseGrid(ref CollisionGrid, gridString);

                        //optimized collision grid
                        reader.ReadToFollowing("opgrid");
                        reader.Read();
                        gridString = reader.Value;
                        parseOpGrid(ref OpCollision, gridString);

                    }
                    if (reader.Name == "sprite")
                    {
                        int id = int.Parse(reader.GetAttribute("id"));
                        reader.Read();
                        SpritePaths.Add(id, reader.Value);
                    }
                    if (reader.Name == "spriteMap")
                    {
                        //sprite grid
                        reader.ReadToDescendant("grid");
                        reader.Read();
                        string gridString = reader.Value;
                        parseGrid(ref SpriteGrid, gridString);

                        //optimized sprite grid
                        reader.ReadToFollowing("opgrid");
                        reader.Read();
                        gridString = reader.Value;
                        parseOpGrid(ref OpSprite, gridString);
                    }
                }
            }
            reader.Close();
        }
        public void WriteXml(XmlWriter writer)
        {
            string colgrid, opcolgrid, sprgrid, opsprgrid;

            //collision grid
            StringBuilder builder = new StringBuilder();
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                    builder.Append(CollisionGrid[i, j] + " ");
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            colgrid = builder.ToString();
            builder.Clear();

            //optimized collision grid
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                {
                    builder.Append((int)OpCollision[i, j].x + "," + (int)OpCollision[i, j].y + " ");
                }
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            opcolgrid = builder.ToString();
            builder.Clear();

            //sprite grid
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                    builder.Append(SpriteGrid[i, j] + " ");
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            sprgrid = builder.ToString();
            builder.Clear();

            //optimized sprite grid
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                {
                    builder.Append((int)OpSprite[i, j].x + "," + (int)OpSprite[i, j].y + " ");
                }
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            opsprgrid = builder.ToString();

            writer.WriteStartElement("tileMap");
            writer.WriteAttributeString("Xtiles", Xtiles.ToString());
            writer.WriteAttributeString("Ytiles", Ytiles.ToString());

            writer.WriteStartElement("collisionMap");

            writer.WriteStartElement("grid");
            writer.WriteString(colgrid);
            writer.WriteEndElement();

            writer.WriteStartElement("opgrid");
            writer.WriteString(opcolgrid);
            writer.WriteEndElement();

            writer.WriteEndElement(); //</collisionmap>

            foreach (KeyValuePair<int, string> entry in SpritePaths)
            {
                writer.WriteStartElement("sprite");
                writer.WriteAttributeString("id", entry.Key.ToString());
                writer.WriteString(entry.Value);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("spriteMap");

            writer.WriteStartElement("grid");
            writer.WriteString(sprgrid);
            writer.WriteEndElement();

            writer.WriteStartElement("opgrid");
            writer.WriteString(opsprgrid);
            writer.WriteEndElement();

            writer.WriteEndElement(); //</spriteMap>
            writer.WriteEndElement(); //</tileMap>
            writer.Close();
        }
        private void parseGrid(ref int[,] grid, string gridString)
        {
            string[] lines = gridString.Split('\n').Where(s => s.Any(c => char.IsDigit(c))).ToArray();
            for (int j = 0; j < Ytiles; j++)
            {
                string[] blocks = lines[j].Split(' ').Where(s => s.Any(c => char.IsDigit(c))).ToArray();

                for (int i = 0; i < Xtiles; i++)
                {
                    grid[i, j] = int.Parse(blocks[i]);
                }
            }
        }
        private void parseOpGrid(ref vec2[,] grid, string gridString)
        {
            string[] lines = gridString.Split('\n').Where(s => s.Any(c => char.IsDigit(c))).ToArray();
            for (int j = 0; j < lines.Length; j++)
            {
                string[] blocks = lines[j].Split(' ').Where(s => s.Any(c => char.IsDigit(c))).ToArray();
                for (int i = 0; i < blocks.Length; i++)
                {
                    string[] v = blocks[i].Split(',');
                    grid[i, j] = new vec2(int.Parse(v[0]), int.Parse(v[1]));
                }
            }
        }
    }
    class DropOutStack<T>
    {
        private T[] items;
        private int top = 0;
        public DropOutStack(int capacity)
        {
            items = new T[capacity];
        }

        public void Push(T item)
        {
            items[top] = item;
            top = (top + 1) % items.Length;
        }
        public T Pop()
        {
            top = (items.Length + top - 1) % items.Length;
            return items[top];
        }
    }
}