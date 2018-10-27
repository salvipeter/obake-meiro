using System;
using System.Collections.Generic;
using Gtk;
using Cairo;

public partial class MainWindow: Gtk.Window
{	
	const int width = 21, height = 11, n_monsters = 10, size = 75;

	enum Cell { Nothing, Wall, Start, Goal, Obake, Bat, Kasa, Mummy }
	Cell[,] map;

	Gdk.Pixbuf player, wall, white, goal, obake, bat, kasa, mummy;
	DrawingArea area;

	struct Pos {
		public int x, y;
		public Pos(int x, int y) {
			this.x = x;
			this.y = y;
		}
	}

	Pos current;

	struct Wall {
		public Pos p1, p2;
		public Wall(int x1, int y1, int x2, int y2) {
			p1.x = x1; p1.y = y1;
			p2.x = x2; p2.y = y2;
		}
	}

	void AddWalls(List<Wall> walls, int x, int y) {
		if (x > 1)
			walls.Add(new Wall(x - 1, y, x - 2, y));
		if (x < width - 2)
			walls.Add(new Wall(x + 1, y, x + 2, y));
		if (y > 1)
			walls.Add(new Wall(x, y - 1, x, y - 2));
		if (y < height - 2)
			walls.Add(new Wall(x, y + 1, x, y + 2));
	}

	void GenerateMap() {
		for (int i = 0; i < width; ++i)
			for (int j = 0; j < height; ++j)
				map[i,j] = Cell.Wall;

		var walls = new List<Wall>();

		map[0,0] = Cell.Start;
		AddWalls(walls, 0, 0);

		var rnd = new Random();
		while (walls.Count != 0) {
			int i = rnd.Next(walls.Count);
			var wall = walls[i];
			walls.RemoveAt(i);
			if (map[wall.p2.x,wall.p2.y] == Cell.Wall) {
				map[wall.p1.x,wall.p1.y] = Cell.Nothing;
				map[wall.p2.x,wall.p2.y] = Cell.Nothing;
				AddWalls(walls, wall.p2.x, wall.p2.y);
			}
		}
		map[width-1,height-1] = Cell.Goal;
	}

	bool IsInside(Pos p) {
		return p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
	}

	List<Pos> TryRoute(Pos start, Pos next, Pos goal, Pos last) {
		if (!IsInside(next) || next.Equals(last) || map[next.x,next.y] == Cell.Wall)
			return null;
		var route = FindRoute(next, goal, start);
		if (route != null)
			route.Add(start);
		return route;
	}

	List<Pos> FindRoute(Pos start, Pos goal, Pos last) {
		if (start.Equals(goal)) {
			var result = new List<Pos>();
			result.Add(start);
			return result;
		}
		return
			TryRoute(start, new Pos(start.x - 1, start.y), goal, last) ??
			TryRoute(start, new Pos(start.x + 1, start.y), goal, last) ??
			TryRoute(start, new Pos(start.x, start.y - 1), goal, last) ??
			TryRoute(start, new Pos(start.x, start.y + 1), goal, last);
	}

	void PlaceMonsters() {
		var route = FindRoute(new Pos(0, 0), new Pos(width - 1, height - 1), new Pos(0, 0));

		var rnd = new Random();
		for (int i = 0; i < n_monsters; ++i) {
			Cell type = Cell.Obake + rnd.Next(4);
			while (true) {
				int x = rnd.Next(width), y = rnd.Next(height);
				if (x % 2 == 1)
					--x;
				if (y % 2 == 1)
					--y;
				var p = new Pos(x, y);
				if (map[x,y] != Cell.Nothing || route.Contains(p))
					continue;
				map[x,y] = type;
				break;
			}
		}
	}

	void Start() {
		GenerateMap();
		PlaceMonsters();
		current = new Pos(0, 0);
	}

	void OnExpose(object sender, ExposeEventArgs args) {
		var area = (DrawingArea)sender;
		var cr = Gdk.CairoHelper.Create(area.GdkWindow);
		for (int i = 0; i < width + 2; ++i) {
			for (int j = 0; j < height + 2; ++j) {
				int x = i * size, y = j * size;
				if (i == 0 || i == width + 1 || j == 0 || j == height + 1 || map[i-1,j-1] == Cell.Wall)
					Gdk.CairoHelper.SetSourcePixbuf(cr, wall, x, y);
				else if (current.Equals(new Pos(i - 1, j - 1)))
					Gdk.CairoHelper.SetSourcePixbuf(cr, player, x, y);
				else {
					switch (map[i-1,j-1]) {
					case Cell.Goal:
						Gdk.CairoHelper.SetSourcePixbuf(cr, goal, x, y);
						break;
					case Cell.Obake:
						Gdk.CairoHelper.SetSourcePixbuf(cr, obake, x, y);
						break;
					case Cell.Bat:
						Gdk.CairoHelper.SetSourcePixbuf(cr, bat, x, y);
						break;
					case Cell.Kasa:
						Gdk.CairoHelper.SetSourcePixbuf(cr, kasa, x, y);
						break;
					case Cell.Mummy:
						Gdk.CairoHelper.SetSourcePixbuf(cr, mummy, x, y);
						break;
					default:
						Gdk.CairoHelper.SetSourcePixbuf(cr, white, x, y);
						break;
					}
				}
				cr.Paint();
			}
		}
	}

	void Go(Pos p) {
		if (!IsInside(p))
			return;
		if (map[p.x,p.y] == Cell.Goal) {
			Start();
		} else if (map[p.x,p.y] == Cell.Nothing || map[p.x,p.y] == Cell.Start) {
			current = p;
		}
		area.QueueDraw();
	}

	[GLib.ConnectBefore]
	void OnKeyPress(object o, KeyPressEventArgs args) {
		switch (args.Event.Key) {
		case Gdk.Key.Left:
			Go(new Pos(current.x - 1, current.y));
			break;
		case Gdk.Key.Right:
			Go(new Pos(current.x + 1, current.y));
			break;
		case Gdk.Key.Up:
			Go(new Pos(current.x, current.y - 1));
			break;
		case Gdk.Key.Down:
			Go(new Pos(current.x, current.y + 1));
			break;
		case Gdk.Key.Escape:
			Application.Quit();
			break;
		default:
			break;
		}
	}

	public MainWindow() : base("Obake Meiro")
	{
		var asm = System.Reflection.Assembly.GetExecutingAssembly();
		player = new Gdk.Pixbuf(asm.GetManifestResourceStream("PlayerImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		wall = new Gdk.Pixbuf(asm.GetManifestResourceStream("WallImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		white = new Gdk.Pixbuf(asm.GetManifestResourceStream("WhiteImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		goal = new Gdk.Pixbuf(asm.GetManifestResourceStream("GoalImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		obake = new Gdk.Pixbuf(asm.GetManifestResourceStream("ObakeImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		bat = new Gdk.Pixbuf(asm.GetManifestResourceStream("BatImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		kasa = new Gdk.Pixbuf(asm.GetManifestResourceStream("KasaImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		mummy = new Gdk.Pixbuf(asm.GetManifestResourceStream("MummyImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);

		map = new Cell[width,height];
		Start();

		SetDefaultSize((width + 2) * size, (height + 2) * size);
		area = new DrawingArea();
		area.ExposeEvent += OnExpose;
		KeyPressEvent += OnKeyPress;
		DeleteEvent += new DeleteEventHandler(OnDeleteEvent);
		Add(area);
		ShowAll();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}
}
