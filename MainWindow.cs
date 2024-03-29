using System;
using System.Collections.Generic;
using Gtk;
using Cairo;

public partial class MainWindow: Gtk.Window
{	
	int width = 21, height = 11, n_monsters = 10, size = 75;

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

	Pos current, margin;

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

		map[current.x,current.y] = Cell.Start;
		AddWalls(walls, current.x, current.y);

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
		if (current.x == 0)
			map[width-1,height-1] = Cell.Goal;
		else
			map[0,0] = Cell.Goal;
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

	void CorrectCursor() {
		Pos offset;
		GetPosition(out offset.x, out offset.y);
		var x = offset.x + margin.x + (current.x + 1) * size + size/2;
		var y = offset.y + margin.y + (current.y + 1) * size + size/2;
		Display.WarpPointer(Screen, x, y);
	}

	void Start() {
		GenerateMap();
		PlaceMonsters();
		CorrectCursor();
	}

	void OnExpose(object sender, ExposeEventArgs args) {
		var area = (DrawingArea)sender;
		var cr = Gdk.CairoHelper.Create(area.GdkWindow);
		for (int i = 0; i < width + 2; ++i) {
			for (int j = 0; j < height + 2; ++j) {
				int x = margin.x + i * size, y = margin.y + j * size;
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
		((IDisposable)cr.GetTarget()).Dispose();                                      
		((IDisposable)cr).Dispose();
	}

	void Go(Pos p) {
		if (!IsInside(p))
			return;
		if (map[p.x,p.y] == Cell.Goal) {
			current = p;
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

	void OnMotion(object o, Gtk.MotionNotifyEventArgs args) {
		var x = margin.x + (current.x + 1) * size + size/2;
		var y = margin.y + (current.y + 1) * size + size/2;

		Pos cursor;
		Gdk.ModifierType mask;
		args.Event.Window.GetPointer(out cursor.x, out cursor.y, out mask);
		if (cursor.x > x + size && cursor.y > y - size && cursor.y < y + size)
			Go(new Pos(current.x + 1, current.y));
		else if (cursor.x < x - size && cursor.y > y - size && cursor.y < y + size)
			Go(new Pos(current.x - 1, current.y));
		else if (cursor.y > y + size && cursor.x > x - size && cursor.x < x + size)
			Go(new Pos(current.x, current.y + 1));
		else if (cursor.y < y - size && cursor.x > x - size && cursor.x < x + size)
			Go(new Pos(current.x, current.y - 1));
	}

	void ReadConfig() {
		try {
			var lines = System.IO.File.ReadAllLines("ObakeMeiro.cfg");
			width      = Int32.Parse(lines[0].Split(' ')[0]);
			height     = Int32.Parse(lines[1].Split(' ')[0]);
			n_monsters = Int32.Parse(lines[2].Split(' ')[0]);
			size       = Int32.Parse(lines[3].Split(' ')[0]);
		} catch(Exception) {
		}
	}

	public MainWindow() : base("Obake Meiro") {
		ReadConfig();

		var asm = System.Reflection.Assembly.GetExecutingAssembly();
		player = new Gdk.Pixbuf(asm.GetManifestResourceStream("PlayerImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		wall = new Gdk.Pixbuf(asm.GetManifestResourceStream("WallImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		white = new Gdk.Pixbuf(asm.GetManifestResourceStream("WhiteImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		goal = new Gdk.Pixbuf(asm.GetManifestResourceStream("GoalImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		obake = new Gdk.Pixbuf(asm.GetManifestResourceStream("ObakeImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		bat = new Gdk.Pixbuf(asm.GetManifestResourceStream("BatImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		kasa = new Gdk.Pixbuf(asm.GetManifestResourceStream("KasaImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);
		mummy = new Gdk.Pixbuf(asm.GetManifestResourceStream("MummyImage")).ScaleSimple(size, size, Gdk.InterpType.Bilinear);

		//SetDefaultSize((width + 2) * size, (height + 2) * size);
		//margin.x = 0; margin.y = 0;
		Fullscreen();
		margin.x = (Screen.Width - (width + 2) * size) / 2;
		margin.y = (Screen.Height - (height + 2) * size) / 2;

		map = new Cell[width,height];
		current = new Pos(0, 0);
		Start();

		area = new DrawingArea();
		area.AddEvents((int)Gdk.EventMask.PointerMotionMask);
		area.ExposeEvent += OnExpose;
		area.MotionNotifyEvent += OnMotion;
		Add(area);
		KeyPressEvent += OnKeyPress;
		DeleteEvent += OnDeleteEvent;
		ShowAll();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a) {
		Application.Quit();
		a.RetVal = true;
	}
}
