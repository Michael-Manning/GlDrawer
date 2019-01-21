using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using GLDrawer;

namespace AdvancedPlatformer
{
    public class TileMap
    {
        public int Xtiles, Ytiles;
        public int[,] CollisionGrid;
        public int[,] EntityGrid;
        public List<Tile> OpCollision = new List<Tile>();
        public List<Tile>[] OpSprites;
        public int layers { get; private set; }
        public int[][,] SpriteGrid;
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
            OpSprites = new List<Tile>[layers];
            for (int i = 0; i < layers; i++)
            {
                SpriteGrid[i] = new int[Xtiles, Ytiles];
                OpSprites[i] = new List<Tile>();
            }
        }
        public struct Tile
        {
            public int x, y, w, h, id;
            public override string ToString()
            {
                return x + "," + y + "," + w + "," + h + (id == 0 ? "" : "," + id);
            }
        };
        public struct Entity
        {
            public string Tag;
            public vec2 Scale;
            public float RotationSpeed;
            public int Id;
            public string FilePath;
            public int TilesPerRow;
            public float Duration;
            public Color color;
        }

        public TileMap(string filepah)
        {
            ReadXml(filepah);
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
                                        float[] v = Array.ConvertAll(s.Split(','), float.Parse);
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
                                if (childNode.Name == "animationDetails")
                                {
                                    e.TilesPerRow = int.Parse(childNode.Attributes.GetNamedItem("tilesPerRow").Value);
                                    e.Duration = float.Parse(childNode.Attributes.GetNamedItem("duration").Value);
                                }
                                if (childNode.Name == "color")
                                {

                                    var colorNode = childNode.Attributes.GetNamedItem("rgba");
                                    if (colorNode != null)
                                    {
                                        string s = colorNode.Value;
                                        s = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                                        int[] v = Array.ConvertAll(s.Split(','), int.Parse);
                                        e.color = new Color(v[0], v[1], v[2], v[3]);
                                    }
                                }
                            }
                            Entities.Add(e.Id, e);
                        }
                    }
                }
            }

        }

        public vec2[,] Optimize(int[,] grid)
        {
            //need to clear the grid
            vec2[,] opgrid = new vec2[Xtiles, Ytiles];

            for (int j = 0; j < Ytiles; j++)
                for (int i = 0; i < Xtiles; i++)
                    if (grid[i, j] > 0)
                        grid[i, j] = 1;

            for (int j = 0; j < Ytiles; j++)
            {
                for (int i = 0; i < Xtiles; i++)
                {
                    int x = 0, y = 0; //longest stretches
                    if (grid[i, j] > 0)
                    {
                        //find longest horizontal stretch
                        while (i + x < Xtiles && grid[i + x, j] == 1 && opgrid[i + x, j].x == 0)
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
                            while (l + j < Ytiles)
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
                            while (j + y < Ytiles && grid[i, j + y] == 1 && opgrid[i, j + y].y == 0)
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
            for (int j = 0; j < Ytiles; j++)
            {
                for (int i = 0; i < Xtiles; i++)
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
            for (int j = 0; j < Ytiles; j++)
            {
                for (int i = 0; i < Xtiles; i++)
                {
                    if (opgrid[i, j].y == 0 && opgrid[i, j].x > 0)
                        opgrid[i, j] = new vec2(opgrid[i, j].x, 1);
                    if (opgrid[i, j].x == 0 && opgrid[i, j].y > 0)
                        opgrid[i, j] = new vec2(1, opgrid[i, j].y);
                }
            }
            return opgrid;
        }

        public void ReadXml(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            foreach (XmlNode parentNode in doc.ChildNodes)
            {
                if (parentNode.Name == "tileMap")
                {
                    Xtiles = int.Parse(parentNode.Attributes.GetNamedItem("Xtiles").Value);
                    Ytiles = int.Parse(parentNode.Attributes.GetNamedItem("Ytiles").Value);
                    layers = int.Parse(parentNode.Attributes.GetNamedItem("layers").Value);
                    CollisionGrid = new int[Xtiles, Ytiles];
                    EntityGrid = new int[Xtiles, Ytiles];
                    SpriteGrid = new int[layers][,];
                    OpSprites = new List<Tile>[layers];
                    for (int i = 0; i < layers; i++)
                        SpriteGrid[i] = new int[Xtiles, Ytiles];
                    for (int i = 0; i < layers; i++)
                    {
                        SpriteGrid[i] = new int[Xtiles, Ytiles];
                        OpSprites[i] = new List<Tile>();
                    }

                    foreach (XmlNode node in parentNode)
                    {
                        if (node.Name == "sprite")
                        {
                            int id = int.Parse(node.Attributes.GetNamedItem("id").Value);
                            string path = node.InnerText;
                            SpritePaths.Add(id, path);
                        }
                        if (node.Name == "collisionMap")
                        {
                            foreach (XmlNode childNode in node)
                            {
                                //collision grid
                                if (childNode.Name == "grid")
                                    parseGrid(ref CollisionGrid, childNode.InnerText);
                                //optimized collision grid
                                else if (childNode.Name == "opgrid")
                                    parseOpGrid(OpCollision, childNode.InnerText);
                            }
                        }
                        if (node.Name == "spriteMap")
                        {
                            foreach (XmlNode childNode in node)
                            {
                                //sprite grid
                                if (childNode.Name == "grid")
                                {
                                    int layer = int.Parse(childNode.Attributes.GetNamedItem("layer").Value);
                                    parseGrid(ref SpriteGrid[layer], childNode.InnerText);
                                }

                                //optimized sprite grid
                                else if (childNode.Name == "opgrid")
                                {
                                    int layer = int.Parse(childNode.Attributes.GetNamedItem("layer").Value);
                                    parseOpGrid(OpSprites[layer], childNode.InnerText);
                                }
                            }
                        }
                        if (node.Name == "entityMap")
                        {
                            foreach (XmlNode childNode in node)
                            {
                                if (childNode.Name == "grid")
                                    parseGrid(ref EntityGrid, childNode.InnerText);
                            }
                        }
                    }
                }
            }
        }
        public void WriteXml(XmlWriter writer)
        {
            string colgrid, opcolgrid, entgrid;

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
                    builder.Append(t.ToString() + " ");
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
            List<string> spriteOpGrids = new List<string>();
            for (int l = 0; l < layers; l++)
            {
                builder.Append(System.Environment.NewLine);
                foreach (KeyValuePair<int, string> kp in SpritePaths)
                {
                    int[,] grid = invertMatrix(SpriteGrid[l]);
                    for (int j = 0; j < Ytiles; j++)
                        for (int i = 0; i < Xtiles; i++)
                            if (grid[i, j] != kp.Key && grid[i, j] != 0)
                                grid[i, j] = 0;
                    vec2[,] op = Optimize(grid);
                    List<Tile> tiles = new List<Tile>();

                    for (int j = 0; j < Ytiles; j++)
                        for (int i = 0; i < Xtiles; i++)
                            if (op[i, j] != vec2.Zero)
                                tiles.Add(new Tile() { w = (int)op[i, j].x, h = (int)op[i, j].y, x = i, y = j, id = kp.Key });
                    if (tiles.Count == 0)
                        continue;
                    builder.Append("      ");
                    foreach (Tile t in tiles)
                        builder.Append(t.ToString() + " ");
                    builder.Append(System.Environment.NewLine);
                }
                spriteOpGrids.Add(builder.ToString());
                builder.Clear();
            }

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
            builder.Clear();

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
                writer.WriteString(spriteOpGrids[i]);
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
                Tile t = new Tile()
                {
                    x = v[0],
                    y = v[1],
                    w = v[2],
                    h = v[3]
                };
                if (v.Length == 5)
                    t.id = v[4];
                grid.Add(t);
            }
        }
        public T[,] invertMatrix<T>(T[,] a)
        {
            T[,] m = new T[Xtiles, Ytiles];
            for (int j = 0; j < Ytiles; j++)
                for (int i = 0; i < Xtiles; i++)
                    m[i, -j + Ytiles - 1] = a[i, j];
            return m;
        }
    }
}
