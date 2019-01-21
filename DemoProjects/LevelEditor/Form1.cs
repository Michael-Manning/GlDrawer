using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.IO;
using AdvancedPlatformer;
using GLDrawer;

namespace LevelEditor
{
    public partial class Form1 : Form
    {
        public Form1()
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
            btncol.Click += (e, s) => UpdateMode(0);
            radioButton4.CheckedChanged += delegate { currentLayer = radioButton3.Checked ? 1 : 0; };
            numericUpDown1.ValueChanged += delegate { gridSubdivision = (int)numericUpDown1.Value; };
            colliderPlaceholder = System.Drawing.Image.FromFile("../../../../data/images/tile set/objects/collider.png");
            impaths = Directory.GetFiles(imgsPath);
            System.Drawing.Image[] imgs = new System.Drawing.Image[impaths.Length];
            for (int i = 0; i < impaths.Length; i++)
                imgs[i] = System.Drawing.Image.FromFile(impaths[i]);
            imageList1.Images.AddRange(imgs);
            checkuv.CheckedChanged += delegate { optimizeAllTextures(); };

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
            listView1.Click += ListView1_SelectedIndexChanged;

            can.MouseScrolled += Can_MouseScrolled;
            can.MouseMove += Can_MouseMove1;
            enableBtns(false);
            pictureBox1.Hide();
        }

        public GLCanvas can;

        const int gridScale = 50;
        int gridSubdivision = 1;

        public const string workFile = "../../../../data/other/mylevel.xml";
        public const string entityFile = "../../../../data/other/entities.xml";
        public const string imgsPath = "../../../../data/images/tile set";//"C:/Users/Micha/Desktop/tileset/separate";
        System.Drawing.Image colliderPlaceholder;

        Polygon[,] cgrid;
        Shape[,] egrid;
        Sprite[][,] sgrid;

        List<Line> lines = new List<Line>();
        List<string> usedPaths = new List<string>();
        Dictionary<int, int> viewIndexToId = new Dictionary<int, int>();
        int selectedImage = 0;
        TileMap.Entity selectedEntity;

        TileMap tilemap;
        int currentLayer = 0, layers = 2;
        int actionMode = 1;
        bool noSelection = true;

        Stack<history> undo = new Stack<history>();
        Stack<history> redo = new Stack<history>();
        struct history
        {
            public int x, y, index, Layer, mode;
            public bool action;
            public history(int x, int y, bool action, int mode, int index = 0, int layer = 0)
            {
                this.x = x;
                this.y = y;
                this.action = action;
                this.mode = mode;
                this.index = index;
                Layer = layer;
            }
        }

        public string[] impaths;


        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedCount = 0;
            listView1.Invoke((Action)delegate { selectedCount = listView1.SelectedIndices.Count; });
            if (selectedCount < 1)
            {
                pictureBox1.Hide();
                noSelection = true;
                return;
            }

            listView1.Invoke((Action)delegate { selectedImage = listView1.SelectedIndices[0] + 1; });
            pictureBox1.Image = System.Drawing.Image.FromFile(impaths[selectedImage - 1]);
            pictureBox1.Show();
            noSelection = false;
            UpdateMode(2);
        }
        private void ListView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedCount = 0;
            listView2.Invoke((Action)delegate { selectedCount = listView2.SelectedIndices.Count; });
            if (selectedCount < 1)
            {
                pictureBox1.Hide();
                noSelection = true;
                return;
            }

            int selectedIndex = 0;
            listView2.Invoke((Action)delegate { selectedIndex = listView2.SelectedIndices[0]; });
            selectedEntity = tilemap.Entities[viewIndexToId[selectedIndex]];
            noSelection = false;
            UpdateMode(1);
        }

        void Undo()
        {
            history h = undo.Pop();
            if (actionMode != h.mode)
                UpdateMode(h.mode);

            if (h.action)
                removeTile(h.x, h.y);
            else
            {
                if (h.mode == 1)
                    selectedEntity = tilemap.Entities[viewIndexToId[h.index]];
                if (h.mode == 2)
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
            if (actionMode != h.mode)
                UpdateMode(h.mode);

            if (!h.action)
                removeTile(h.x, h.y);
            else
            {
                if (h.mode == 1)
                    selectedEntity = tilemap.Entities[viewIndexToId[h.index]];
                if (h.mode == 2)
                    selectedImage = h.index;
                addTile(h.x, h.y);
            }

            if (redo.Count == 0)
                btnredo.Enabled = false;
        }

        private void Can_MouseScrolled(int Delta, GLCanvas Canvas)
        {
            can.CameraZoom += Delta * 0.01f;
            if (can.CameraZoom < 0.1f)
                can.CameraZoom = 0.1f;
            lines.ForEach(l => l.Thickness = 6 / can.CameraZoom);
        }

        private void Can_MouseMove1(vec2 Position, GLCanvas Canvas)
        {
            if (can.MouseMiddleState)
                can.CameraPosition -= can.MouseDeltaPosition / can.CameraZoom;
        }

        void drawLines()
        {
            lines.ForEach(l => can.Remove(l));
            lines.Clear();
            for (int j = 0; j < tilemap.Ytiles + 1; j++)
                lines.Add(can.Add(new Line(new vec2(0, j * gridScale), new vec2(tilemap.Xtiles * gridScale, j * gridScale), 6, new Color(100))) as Line);
            for (int j = 0; j < tilemap.Xtiles + 1; j++)
                lines.Add(can.Add(new Line(new vec2(j * gridScale, 0), new vec2(j * gridScale, tilemap.Ytiles * gridScale), 6, new Color(100))) as Line);
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
            if (noSelection)
                return;

            if (actionMode == 0 && tilemap.CollisionGrid[x, y] == 0)
            {
                tilemap.CollisionGrid[x, y] = 1;
                undo.Push(new history(x, y, true, 0));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimizeCollision();
            }
            else if (actionMode == 1 && tilemap.EntityGrid[x, y] == 0)
            {
                TileMap.Entity e = selectedEntity;
                if (e.FilePath != null && e.FilePath != "")
                {
                    Sprite s = new Sprite(e.FilePath, new vec2(x, y) * gridScale + new vec2(gridScale / 2, -gridScale / 2 + gridScale * 1), e.Scale * gridScale, rotationSpeed: e.RotationSpeed);
                    s.DrawIndex = 0;
                    egrid[x, y] = can.Add(s);
                    tilemap.EntityGrid[x, y] = e.Id;
                    if (e.Duration > 0)
                        s.SetAnimation(e.TilesPerRow, e.Duration);
                }
                else
                {
                    Polygon p = new Polygon(new vec2(x, y) * gridScale + new vec2(gridScale / 2, -gridScale / 2 + gridScale * 1), new vec2(gridScale), 0, 4, e.color);
                    p.DrawIndex = 0;
                    egrid[x, y] = can.Add(p);
                    tilemap.EntityGrid[x, y] = e.Id;
                }
                undo.Push(new history(x, y, true, 1, e.Id));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
            }
            else if (actionMode == 2 && tilemap.SpriteGrid[currentLayer][x, y] == 0)
            {
                if (selectedImage == 0)
                    return;
                tilemap.SpriteGrid[currentLayer][x, y] = selectedImage;

                if (!usedPaths.Contains(impaths[selectedImage - 1]))
                {
                    usedPaths.Add(impaths[selectedImage - 1]);
                    tilemap.SpritePaths.Add(selectedImage, impaths[selectedImage - 1]);
                }

                undo.Push(new history(x, y, true, 2, selectedImage, currentLayer));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimizeTextures(selectedImage, currentLayer);

            }
        }
        void removeTile(int x, int y)
        {
            if (actionMode == 0 && tilemap.CollisionGrid[x, y] != 0)
            {
                tilemap.CollisionGrid[x, y] = 0;
                undo.Push(new history(x, y, false, 0));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                optimizeCollision();
            }
            else if (actionMode == 1 && tilemap.EntityGrid[x, y] != 0 && egrid[x, y] != null)
            {
                can.Remove(egrid[x, y]);
                egrid[x, y] = null;
                undo.Push(new history(x, y, false, 1, tilemap.EntityGrid[x, y]));
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
                tilemap.EntityGrid[x, y] = 0;
            }
            else if (actionMode == 2 && tilemap.SpriteGrid[currentLayer][x, y] != 0 /*&& sgrid[layer][x, y] != null*/)
            {
                undo.Push(new history(x, y, false, 2, tilemap.SpriteGrid[currentLayer][x, y], currentLayer));
                //  can.Remove(sgrid[layer][x, y]);
                //sgrid[layer][x, y] = null;
                int id = tilemap.SpriteGrid[currentLayer][x, y];
                tilemap.SpriteGrid[currentLayer][x, y] = 0;
                optimizeTextures(id, currentLayer);
                btnundo.Invoke((Action)delegate { btnundo.Enabled = true; });
            }
        }

        vec2 mouseGridPos(vec2 Position)
        {
            Position /= can.CameraZoom;
            Position += can.CameraPosition;

            vec2 pos = new vec2((int)Position.x / (gridScale), (int)Position.y / gridScale);
            return pos;
        }

        void clear()
        {
            if (tilemap == null)
                return;
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (cgrid[i, j] != null)
                    {
                        can.Remove(cgrid[i, j]);
                        cgrid[i, j] = null;
                    }
                    for (int l = 0; l < layers; l++)
                    {
                        if (sgrid[l][i, j] != null)
                        {
                            can.Remove(sgrid[l][i, j]);
                            sgrid[l][i, j] = null;
                        }
                    }
                    if (tilemap.Entities.Count > 0 && egrid[i, j] != null)
                    {
                        can.Remove(egrid[i, j]);
                        egrid[i, j] = null;
                    }
                }
            }
            if (tilemap.Entities.Count > 0)
            {
                tilemap = new TileMap(tilemap.Xtiles, tilemap.Ytiles, 2);
                tilemap.LoadEntities(entityFile);
            }
            else
                tilemap = new TileMap(tilemap.Xtiles, tilemap.Ytiles, 2);
            usedPaths.Clear();
        }

        vec2[,] optimizeCollision()
        {
            //need to clear the grid
            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    if (cgrid[i, j] != null)
                        can.Remove(cgrid[i, j]);

            int[,] grid = tilemap.invertMatrix(tilemap.CollisionGrid);
            vec2[,] opgrid = tilemap.Optimize(grid);

            tilemap.OpCollision.Clear();
            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    if (opgrid[i, j].x > 0 && opgrid[i, j].y > 0)
                        tilemap.OpCollision.Add(new TileMap.Tile() { x = i, y = j, w = (int)opgrid[i, j].x, h = (int)opgrid[i, j].y });

            foreach (TileMap.Tile t in tilemap.OpCollision)
                cgrid[t.x, t.y] = can.Add((new Polygon(new vec2(t.x + t.w / 2f, -t.y - (t.h / 2f) + tilemap.Ytiles) * gridScale, new vec2(gridScale * t.w, gridScale * t.h), 0, 4, Color.Hazard, 8, Color.Black))) as Polygon;
            return opgrid;
        }

        vec2[,] optimizeTextures(int textureID, int layer)
        {
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    //removes textures of the id or where a texture was just removed from the grid
                    if (sgrid[layer][i, j] != null && (tilemap.SpriteGrid[layer][i, -j + tilemap.Ytiles - 1] == 0 || tilemap.SpriteGrid[layer][i, -j + tilemap.Ytiles - 1] == textureID))
                    {
                        can.Remove(sgrid[layer][i, j]);
                        sgrid[layer][i, j] = null;
                    }
                }
            }

            int[,] grid = tilemap.invertMatrix(tilemap.SpriteGrid[layer]);

            for (int j = 0; j < tilemap.Ytiles; j++)
                for (int i = 0; i < tilemap.Xtiles; i++)
                    if (grid[i, j] != textureID && grid[i, j] != 0)
                        grid[i, j] = 0;

            vec2[,] opgrid = tilemap.Optimize(grid);

            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (opgrid[i, j].x > 0 && opgrid[i, j].y > 0)
                    {
                        TileMap.Tile t = new TileMap.Tile() { x = i, y = j, w = (int)opgrid[i, j].x, h = (int)opgrid[i, j].y };
                        vec2 p = new vec2(t.x + t.w / 2f, -t.y - (t.h / 2f) + tilemap.Ytiles) * gridScale;
                        vec2 s = new vec2(gridScale * t.w, gridScale * t.h);
                        Sprite sp = new Sprite(impaths[textureID - 1], p, s, uvScale: checkuv.Checked ? new vec2(t.w, t.h) : new vec2(1));
                        sp.DrawIndex = layer;
                        sgrid[layer][i, j] = can.Add(sp) as Sprite;
                    }
                }
            }
            return opgrid;
        }
        void optimizeAllTextures()
        {
            for (int l = 0; l < tilemap.layers; l++)
                foreach (KeyValuePair<int, string> kp in tilemap.SpritePaths)
                    optimizeTextures(kp.Key, l);
        }


        private void loadMap(object sender, EventArgs p)
        {
            tilemap = new TileMap(workFile);
            sgrid = new Sprite[2][,];
            sgrid[0] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
            sgrid[1] = new Sprite[tilemap.Xtiles, tilemap.Ytiles];
            cgrid = new Polygon[tilemap.Xtiles, tilemap.Ytiles];
            drawLines();

            optimizeCollision();
            optimizeAllTextures();

            loadEntities();
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (tilemap.EntityGrid[i, j] > 0)
                    {
                        TileMap.Entity e = tilemap.Entities[tilemap.EntityGrid[i, j]];
                        if (e.FilePath != null && e.FilePath != "")
                        {
                            egrid[i, j] = can.Add(new Sprite(e.FilePath, new vec2(i, j) * gridScale + new vec2(gridScale / 2, -gridScale / 2 + gridScale * 1), e.Scale * gridScale, rotationSpeed: e.RotationSpeed));
                            if (e.Duration > 0)
                                ((Sprite)egrid[i, j]).SetAnimation(e.TilesPerRow, e.Duration);
                        }
                        else
                            egrid[i, j] = can.Add(new Polygon(new vec2(i, j) * gridScale + new vec2(gridScale / 2, -gridScale / 2 + gridScale * 1), new vec2(gridScale), 0, 4, e.color));
                    }
                }
            }

            enableBtns(true);
            UpdateMode(actionMode); //update opacity
            can.CameraPosition += (new vec2(tilemap.Xtiles, tilemap.Ytiles) * gridScale) / 2f;
            usedPaths.Clear();
            foreach (KeyValuePair<int, string> entry in tilemap.SpritePaths)
                usedPaths.Add(entry.Value);
        }
        private void writeMap(object sender, EventArgs e)
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.None;
            s.Indent = true;

            XmlWriter writer = XmlWriter.Create(workFile, s);
            tilemap.WriteXml(writer);

            if (gameStarted)
                AdvancedPlatformer.Program.loadLevel();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            LevelDialog ld = new LevelDialog();
            ld.ShowDialog();
            if (ld.DialogResult == DialogResult.OK)
            {
                clear();
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
            btncol.Enabled = enabled;
            btnundo.Enabled = enabled && (undo.Count != 0);
            btnredo.Enabled = enabled && (redo.Count != 0);
            tabControl1.Enabled = enabled;
        }

        private void UpdateMode(int mode)
        {
            actionMode = mode;
            if (actionMode == 0)
            {
                showCollision(true);
                showTextures(false);
                showEntities(false);

                pictureBox1.Show();
                pictureBox1.Image = colliderPlaceholder;
                noSelection = false;
            }
            if (actionMode == 1)
            {
                optimizeCollision();
                showCollision(false);
                showTextures(false);
                showEntities(true);
            }
            else if (actionMode == 2)
            {
                optimizeCollision();
                showCollision(false);
                showTextures(true);
                showEntities(true);
            }

        }

        void showTextures(bool flag)
        {
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    for (int l = 0; l < layers; l++)
                    {
                        if (sgrid[l][i, j] != null)
                        {
                            if (flag)
                            {
                                sgrid[l][i, j].DrawIndex = l;
                                sgrid[l][i, j].Opacity = 1;
                            }
                            else
                            {
                                sgrid[l][i, j].DrawIndex = l + 1;
                                sgrid[l][i, j].Opacity = 0.3f;
                            }
                        }
                    }
                }
            }
        }
        void showCollision(bool flag)
        {
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (cgrid[i, j] != null)
                    {
                        if (flag)
                        {
                            cgrid[i, j].DrawIndex = 0;
                            Color c = cgrid[i, j].FillColor;
                            cgrid[i, j].FillColor = Color.Hazard;
                        }
                        else
                        {
                            cgrid[i, j].DrawIndex = 3;
                            Color c = cgrid[i, j].FillColor;
                            cgrid[i, j].FillColor = new Color(c.R, c.G, c.B, 100);
                        }
                    }
                }
            }
        }
        void showEntities(bool flag)
        {
            if (tilemap.Entities.Count == 0)
                return;
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if (egrid[i, j] != null)
                    {
                        if (flag)
                        {
                            egrid[i, j].DrawIndex = -1;
                            if (tilemap.Entities[tilemap.EntityGrid[i, j]].FilePath != null)
                                ((Sprite)egrid[i, j]).Opacity = 1f;
                        }
                        else
                        {
                            egrid[i, j].DrawIndex = -1;
                            if (tilemap.Entities[tilemap.EntityGrid[i, j]].FilePath != null)
                                ((Sprite)egrid[i, j]).Opacity = 0.3f;
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
            Color[] colors = tilemap.Entities.Select(e => e.Value.color).ToArray();
            System.Drawing.Image[] imgs = new System.Drawing.Image[entpaths.Length];
            for (int i = 0; i < entpaths.Length; i++)
            {
                if (entpaths[i] != null && entpaths[i] != "")
                    imgs[i] = System.Drawing.Image.FromFile(entpaths[i]);
                else
                {
                    System.Drawing.Bitmap map = new System.Drawing.Bitmap(10, 10);
                    for (int l = 0; l < 10; l++)
                        for (int j = 0; j < 10; j++)
                            map.SetPixel(l, j, colors[i]);



                    imgs[i] = map;
                }
            }

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
            listView2.Click += ListView2_SelectedIndexChanged;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMode(tabControl1.SelectedIndex + 1);
            pictureBox1.Hide();
            noSelection = true;
        }

        private void btnplay_Click(object sender, EventArgs e)
        {
            if (!gameStarted)
            {
                AdvancedPlatformer.Program.RunGame();
                AdvancedPlatformer.Program.can.OnClose += Can_OnClose;
                gameStarted = true;
            }
            else
                AdvancedPlatformer.Program.loadLevel();
        }

        private void Can_OnClose(GLCanvas Canvas)
        {
            gameStarted = false;
        }
    }



}
