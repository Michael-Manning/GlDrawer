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
        public const string entityFile = "../../../data/other/entities.xml";
        public const string imgsPath = "../../../data/images/tile set";//"C:/Users/Micha/Desktop/tileset/separate";

        Polygon[,] cgrid;
        Shape[,] egrid;
        Sprite[][,] sgrid;

        List<Line> lines = new List<Line>();
        List<string> usedPaths = new List<string>();
        Dictionary<int, int> viewIndexToId = new Dictionary<int, int>();
        int selectedImage = 0;
        TileMap.Entity selectedEntity;

        TileMap tilemap;
        int layer = 0, layers = 2;
        int actionMode = 1;

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
            tabControl1.SelectedIndexChanged += (e, s) => UpdateMode(tabControl1.SelectedIndex + 1); //entity mode texture mode
            btncol.Click += (e, s) => UpdateMode(0);
            // radioButton1.CheckedChanged += delegate { checkChanged(); };
            radioButton4.CheckedChanged += delegate { layer = radioButton3.Checked ? 1 : 0; };

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
            listView1.SelectedIndexChanged += ListView1_SelectedIndexChanged;

            can.MouseScrolled += Can_MouseScrolled;
            can.MouseMove += Can_MouseMove1;
            enableBtns(false);
        }

        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedCount = 0;
            listView1.Invoke((Action)delegate { selectedCount = listView1.SelectedIndices.Count; });
            if (selectedCount < 1)
                return;
            listView1.Invoke((Action)delegate { selectedImage = listView1.SelectedIndices[0] + 1; });
            UpdateMode(2);
        }
        private void ListView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedCount = 0;
            listView2.Invoke((Action)delegate { selectedCount = listView2.SelectedIndices.Count; });
            if (selectedCount < 1)
                return;
            int selectedIndex = 0;
            listView2.Invoke((Action)delegate { selectedIndex = listView2.SelectedIndices[0]; });
            selectedEntity = tilemap.Entities[viewIndexToId[selectedIndex]];
            UpdateMode(1);
        }

        void Undo()
        {
            history h = undo.Pop();
            //if (radioButton1.Checked != h.mode)
            //{
            //    radioButton1.Checked = h.mode;
            //    radioButton2.Checked = !h.mode;
            //    UpdateMode();
            //}

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
            //if (radioButton1.Checked != h.mode)
            //{
            //    radioButton1.Checked = h.mode;
            //    radioButton2.Checked = !h.mode;
            //    UpdateMode();
            //}
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
            if (actionMode == 0 && tilemap.CollisionGrid[x, y] == 0)
            {
                tilemap.CollisionGrid[x, y] = 1;
                undo.Push(new history(x, y, true, true));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimize();
            }
            else if (actionMode == 1 && tilemap.EntityGrid[x, y] == 0)
            {
                TileMap.Entity e = selectedEntity;
                if (e.FilePath != null || e.FilePath != "")
                {
                    Sprite s = new Sprite(e.FilePath, new vec2(x, y) * scale + new vec2(scale / 2, -scale / 2 + scale * 1), e.Scale * scale, rotationSpeed: e.RotationSpeed);
                    s.DrawIndex = 0;
                    egrid[x, y] = can.Add(s);
                    tilemap.EntityGrid[x, y] = e.Id;
                }
            }
            else if (actionMode == 2 && tilemap.SpriteGrid[layer][x, y] == 0)
            {
                if (selectedImage == 0)
                    return;
                tilemap.SpriteGrid[layer][x, y] = selectedImage;
                if (sgrid[layer][x, y] == null)
                {
                    if (!usedPaths.Contains(impaths[selectedImage - 1]))
                    {
                        usedPaths.Add(impaths[selectedImage - 1]);
                        tilemap.SpritePaths.Add(selectedImage, impaths[selectedImage - 1]);
                    }
                    Sprite s = new Sprite(impaths[selectedImage - 1], new vec2(x, y) * scale + new vec2(scale / 2, -scale / 2 + scale * 1), new vec2(scale));
                    s.DrawIndex = layer;
                    sgrid[layer][x, y] = can.Add(s) as Sprite;
                    undo.Push(new history(x, y, true, false, selectedImage));
                    btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                }
            }
        }
        void removeTile(int x, int y)
        {
            if (actionMode == 0 && tilemap.CollisionGrid[x, y] != 0)
            {
                tilemap.CollisionGrid[x, y] = 0;
                undo.Push(new history(x, y, false, true));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimize();
            }
            else if (actionMode == 1 && tilemap.EntityGrid[x, y] != 0 && egrid[x, y] != null)
            {
                can.Remove(egrid[x, y]);
                egrid[x, y] = null;
                tilemap.EntityGrid[x, y] = 0;
            }
            else if (actionMode == 2 && tilemap.SpriteGrid[layer][x, y] != 0 && sgrid[layer][x, y] != null)
            {
                undo.Push(new history(x, y, false, false, tilemap.SpriteGrid[layer][x, y]));
                can.Remove(sgrid[layer][x, y]);
                sgrid[layer][x, y] = null;
                tilemap.SpriteGrid[layer][x, y] = 0;
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
            tilemap = new TileMap(tilemap.Xtiles, tilemap.Ytiles, 2);
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (cgrid[i, j] != null)
                    {
                        can.Remove(cgrid[i, j]);
                        cgrid[i, j] = null;
                    }

                    if (sgrid[layer][i, j] != null)
                    {
                        can.Remove(sgrid[layer][i, j]);
                        sgrid[layer][i, j] = null;
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

            tilemap.OpCollision.Clear();
            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    if (opgrid[i, j].x > 0 && opgrid[i, j].y > 0)
                        tilemap.OpCollision.Add(new TileMap.Tile() { x = i, y = j, w = (int)opgrid[i, j].x, h = (int)opgrid[i, j].y });

            foreach (TileMap.Tile t in tilemap.OpCollision)
                cgrid[t.x, t.y] = can.Add((new Polygon(new vec2(t.x + t.w / 2f, -t.y - (t.h / 2f) + tilemap.Ytiles) * scale, new vec2(scale * t.w, scale * t.h), 0, 4, Color.Hazard, 8, Color.Black))) as Polygon;
        }

        private void loadMap(object sender, EventArgs p)
        {
            XmlReader reader = XmlReader.Create(workFile);
            tilemap = new TileMap(reader);
            sgrid = new Sprite[2][,];
            sgrid[0] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
            sgrid[1] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
            cgrid = new Polygon[tilemap.Xtiles, tilemap.Ytiles];
            drawLines();

            for (int l = 0; l < tilemap.layers; l++)
                for (int j = 0; j < tilemap.Ytiles; j++)
                    for (int i = 0; i < tilemap.Xtiles; i++)
                        if (tilemap.SpriteGrid[l][i, j] > 0)
                            sgrid[l][i, j] = can.Add(new Sprite(tilemap.SpritePaths[tilemap.SpriteGrid[l][i, j]], new vec2(i, j + 1) * scale + new vec2(scale / 2, -scale / 2), new vec2(scale))) as Sprite;
            optimize();
            loadEntities();
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (tilemap.EntityGrid[i, j] > 0)
                    {
                        TileMap.Entity e = tilemap.Entities[tilemap.EntityGrid[i, j]];
                        egrid[i,j] = can.Add(new Sprite(e.FilePath, new vec2(i, j) * scale + new vec2(scale / 2, -scale / 2 + scale * 1), e.Scale * scale, rotationSpeed: e.RotationSpeed));
                    }
                }
            }

            enableBtns(true);
            UpdateMode(actionMode); //update opacity
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

            if (gameStarted)
                platformGame.AdvancedPlatformer.loadLevel();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            levelDialog ld = new levelDialog();
            ld.ShowDialog();
            if (ld.DialogResult == DialogResult.OK)
            {
                tilemap = new TileMap(ld.gridWidth, ld.gridHeight, 2);
                sgrid = new Sprite[2][,];
                sgrid[0] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
                sgrid[1] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
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

        private void UpdateMode(int mode)
        {
            // if (actionMode == mode)
            //     return;
            actionMode = mode;
            if (actionMode == 0)
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
                        for (int l = 0; l < layers; l++)
                        {
                            if (sgrid[l][i, j] != null)
                            {
                                sgrid[l][i, j].DrawIndex = l + 1;
                                sgrid[l][i, j].Opacity = 0.3f;
                            }
                        }
                    }
                }
            }
            if (actionMode == 1)
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
                        for (int l = 0; l < layers; l++)
                        {
                            if (sgrid[l][i, j] != null)
                            {
                                sgrid[l][i, j].DrawIndex = l;
                                sgrid[l][i, j].Opacity = 1;
                            }
                        }
                    }
                }
            }
            else if (actionMode == 2)
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
                        for (int l = 0; l < layers; l++)
                        {
                            if (sgrid[l][i, j] != null)
                            {
                                sgrid[l][i, j].DrawIndex = l;
                                sgrid[l][i, j].Opacity = 1;
                            }
                        }
                    }
                }
            }

        }

        bool gameStarted = false;

        private void button3_Click(object sender, EventArgs p)
        {
            loadEntities();
        }

        void loadEntities()
        {
            egrid = new Shape[tilemap.Xtiles, tilemap.Ytiles];
            tilemap.LoadEntities(entityFile);

            for (int i = 0; i < tilemap.Entities.Count; i++)
            {
                viewIndexToId.Add(i, tilemap.Entities.ElementAt(i).Key);
            }

            string[] entpaths = tilemap.Entities.Select(e => e.Value.FilePath).ToArray();
            System.Drawing.Image[] imgs = new System.Drawing.Image[entpaths.Length];
            for (int i = 0; i < entpaths.Length; i++)
                imgs[i] = System.Drawing.Image.FromFile(entpaths[i]);
            imageList2.Images.AddRange(imgs);

            listView2.View = View.LargeIcon;
            imageList2.ImageSize = new System.Drawing.Size(64, 64);
            listView2.LargeImageList = imageList2;

            for (int j = 0; j < imageList2.Images.Count; j++)
            {
                ListViewItem item = new ListViewItem();
                item.ImageIndex = j;
                listView2.Items.Add(item);
            }
            listView2.SelectedIndexChanged += ListView2_SelectedIndexChanged;
        }

        private void btnplay_Click(object sender, EventArgs e)
        {
            if (!gameStarted)
            {
                platformGame.AdvancedPlatformer.run();
                gameStarted = true;
            }
            else
                platformGame.AdvancedPlatformer.loadLevel();
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
        public int[,] EntityGrid;
        public List<Tile> OpCollision = new List<Tile>();
        public int layers { get; private set; }
        public int[][,] SpriteGrid;
        public vec2[,] OpSprite;
        public Dictionary<int, string> SpritePaths = new Dictionary<int, string>();
        public Dictionary<int, Entity> Entities = new Dictionary<int, Entity>();

        public TileMap(int width, int height, int spriteLayers)
        {
            Xtiles = width;
            Ytiles = height;
            layers = spriteLayers;
            CollisionGrid = new int[Xtiles, Ytiles];
            EntityGrid = new int[Xtiles, Ytiles];
            SpriteGrid = new int[layers][,];
            OpSprite = new vec2[Xtiles, Ytiles];
            for (int i = 0; i < layers; i++)
                SpriteGrid[i] = new int[Xtiles, Ytiles];
        }
        public struct Tile { public int x, y, w, h; };
        public struct Entity
        {
            public string Tag;
            public vec2 Scale;
            public float RotationSpeed;
            public int Id;
            public string FilePath;
        }

        public TileMap(XmlReader r) => ReadXml(r);

        public TileMap(string filepah)
        {
            XmlReader reader = XmlReader.Create(filepah);
            ReadXml(reader);
        }

        public XmlSchema GetSchema() => null;

        public void LoadEntities(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            foreach (XmlNode parentNode in doc.ChildNodes)
            {
                if (parentNode.Name == "entities")
                {
                    foreach (XmlNode node in parentNode)
                    {
                        if (node.Name == "entity")
                        {
                            Entity e = new Entity();
                            e.Id = int.Parse(node.Attributes.GetNamedItem("id").Value);
                            e.Tag = node.Attributes.GetNamedItem("tag").Value;
                            foreach (XmlNode childNode in node)
                            {
                                if (childNode.Name == "img")
                                {
                                    vec2 scale = new vec2(1);
                                    float rotSpeed = 0;

                                    //scale
                                    var scaleNode = childNode.Attributes.GetNamedItem("scale");
                                    if (scaleNode != null)
                                    {
                                        string s = scaleNode.Value;
                                        s = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                                        int[] v = Array.ConvertAll(s.Split(','), int.Parse);
                                        scale = new vec2(v[0], v[1]);
                                    }
                                    //rspeed
                                    var rotNode = childNode.Attributes.GetNamedItem("rotationSpeed");
                                    if (rotNode != null)
                                        rotSpeed = int.Parse(((string)rotNode.Value));

                                    e.FilePath = childNode.InnerText;
                                    e.Scale = scale;
                                    e.RotationSpeed = rotSpeed;
                                }
                            }
                            Entities.Add(e.Id, e);
                        }
                    }
                }
            }

        }

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
                        layers = int.Parse(reader.GetAttribute("layers"));
                        CollisionGrid = new int[Xtiles, Ytiles];
                        EntityGrid = new int[Xtiles, Ytiles];
                        SpriteGrid = new int[layers][,];
                        OpSprite = new vec2[Xtiles, Ytiles];
                        for (int i = 0; i < layers; i++)
                            SpriteGrid[i] = new int[Xtiles, Ytiles];
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
                        parseOpGrid(OpCollision, gridString);

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
                        if (reader.ReadToDescendant("grid"))
                        {
                            int layer = int.Parse(reader.GetAttribute("layer"));
                            reader.Read();
                            string gridString = reader.Value;
                            parseGrid(ref SpriteGrid[layer], gridString);

                            while (reader.ReadToFollowing("grid"))
                            {
                                layer = int.Parse(reader.GetAttribute("layer"));
                                reader.Read();
                                gridString = reader.Value;
                                parseGrid(ref SpriteGrid[layer], gridString);
                            }
                        }

                        //optimized sprite grid
                        reader.ReadToFollowing("opgrid");
                        reader.Read();
                        //gridString = reader.Value;
                        // parseOpGrid(ref OpSprite, gridString);
                    }
                    if (reader.Name == "entityMap")
                    {
                        //entity grid
                        reader.ReadToDescendant("grid");
                        reader.Read();
                        string gridString = reader.Value;
                        parseGrid(ref EntityGrid, gridString);
                    }
                }
            }
            reader.Close();
        }
        public void WriteXml(XmlWriter writer)
        {
            string colgrid, opcolgrid, opsprgrid, entgrid;

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
            OpCollision.Sort((a, b) => a.y.CompareTo(b.y));
            var lines = OpCollision.GroupBy(g => g.y); //grouped to make XML more readable
            foreach (var group in lines)
            {
                builder.Append("      ");
                foreach (Tile t in group)
                    builder.Append(t.x + "," + t.y + "," + t.w + "," + t.h + " ");
                builder.Append(System.Environment.NewLine);
            }

            builder.Append("    ");
            opcolgrid = builder.ToString();
            builder.Clear();

            //sprite grid
            List<string> spriteGrids = new List<string>();
            for (int l = 0; l < layers; l++)
            {
                builder.Append(System.Environment.NewLine);
                for (int j = 0; j < Ytiles; j++)
                {
                    builder.Append("      ");
                    for (int i = 0; i < Xtiles; i++)
                        builder.Append(SpriteGrid[l][i, j] + " ");
                    builder.Append(System.Environment.NewLine);
                }
                builder.Append("    ");
                spriteGrids.Add(builder.ToString());
                builder.Clear();
            }


            //optimized sprite grid
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                    builder.Append((int)OpSprite[i, j].x + "," + (int)OpSprite[i, j].y + " ");
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            opsprgrid = builder.ToString();
            builder.Clear();

            //entity grid
            builder.Append(System.Environment.NewLine);
            for (int j = 0; j < Ytiles; j++)
            {
                builder.Append("      ");
                for (int i = 0; i < Xtiles; i++)
                    builder.Append(EntityGrid[i, j] + " ");
                builder.Append(System.Environment.NewLine);
            }
            builder.Append("    ");
            entgrid = builder.ToString();


            writer.WriteStartElement("tileMap");
            writer.WriteAttributeString("Xtiles", Xtiles.ToString());
            writer.WriteAttributeString("Ytiles", Ytiles.ToString());
            writer.WriteAttributeString("layers", layers.ToString());

            writer.WriteStartElement("collisionMap");

            writer.WriteStartElement("grid");
            writer.WriteString(colgrid);
            writer.WriteEndElement();

            writer.WriteStartElement("opgrid");
            writer.WriteString(opcolgrid);
            writer.WriteEndElement();

            writer.WriteEndElement(); //</collisionmap>


            writer.WriteStartElement("entityMap");

            writer.WriteStartElement("grid");
            writer.WriteString(entgrid);
            writer.WriteEndElement();

            writer.WriteEndElement(); //</entiyMap>

            foreach (KeyValuePair<int, string> entry in SpritePaths)
            {
                writer.WriteStartElement("sprite");
                writer.WriteAttributeString("id", entry.Key.ToString());
                writer.WriteString(entry.Value);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("spriteMap");

            for (int i = 0; i < layers; i++)
            {
                writer.WriteStartElement("grid");
                writer.WriteAttributeString("layer", i.ToString());
                writer.WriteString(spriteGrids[i]);
                writer.WriteEndElement();

                writer.WriteStartElement("opgrid");
                writer.WriteAttributeString("layer", i.ToString());
                //  writer.WriteString(opsprgrid);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();//</spriteMap>
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
                    grid[i, j] = int.Parse(blocks[i]);
            }
        }
        private void parseOpGrid(List<Tile> grid, string gridString)
        {
            gridString.Replace('\n', ' ');

            string[] blocks = gridString.Split(' ').Where(s => s.Any(c => char.IsDigit(c))).ToArray();
            for (int i = 0; i < blocks.Length; i++)
            {
                int[] v = Array.ConvertAll(blocks[i].Split(','), int.Parse);
                grid.Add(new Tile() { x = v[0], y = v[1], w = v[2], h = v[3] });
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