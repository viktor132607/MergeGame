using Godot;
using MergeHarvest;
using System;

// A polished pastel checkerboard inspired by the visual conventions of casual merge games.
public partial class BoardView : Control
{
    private readonly GameData _game;
    private int _held = -1;
    private Vector2 _mouseStart;
    private Vector2 _pointer;
    private int _sparkTile = -1;
    private float _sparkTime;
    private const float Gap = 3f;

    public event Action<bool>? ActionFinished;

    public BoardView(GameData game)
    {
        _game = game;
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    public override void _Ready() => SetProcess(false);

    private (float cell, float x, float y) Geometry()
    {
        float size = Mathf.Min((Size.X - 20f - (FoodCatalog.Columns - 1) * Gap) / FoodCatalog.Columns,
                                (Size.Y - 20f - (FoodCatalog.Rows - 1) * Gap) / FoodCatalog.Rows);
        size = Mathf.Max(1f, size);
        float width = size * FoodCatalog.Columns + (FoodCatalog.Columns - 1) * Gap;
        float height = size * FoodCatalog.Rows + (FoodCatalog.Rows - 1) * Gap;
        return (size, (Size.X - width) / 2f, (Size.Y - height) / 2f);
    }

    private int HitTest(Vector2 at)
    {
        var (size, x, y) = Geometry();
        float localX = at.X - x;
        float localY = at.Y - y;
        if (localX < 0f || localY < 0f) return -1;
        int col = Mathf.FloorToInt(localX / (size + Gap));
        int row = Mathf.FloorToInt(localY / (size + Gap));
        if (col < 0 || col >= FoodCatalog.Columns || row < 0 || row >= FoodCatalog.Rows) return -1;
        if (localX - col * (size + Gap) > size || localY - row * (size + Gap) > size) return -1;
        return row * FoodCatalog.Columns + col;
    }

    private Vector2 CenterOf(int index)
    {
        var (cell, x, y) = Geometry();
        return new Vector2(x + (index % FoodCatalog.Columns) * (cell + Gap) + cell / 2f,
                           y + (index / FoodCatalog.Columns) * (cell + Gap) + cell / 2f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button &&
            button.ButtonIndex == MouseButton.Left && button.Pressed)
        {
            _held = HitTest(button.Position);
            _mouseStart = button.Position;
            _pointer = button.Position;
            if (_held >= 0) AcceptEvent();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_held < 0) return;
        if (@event is InputEventMouseMotion)
        {
            _pointer = GetLocalMousePosition();
            QueueRedraw();
        }
        else if (@event is InputEventMouseButton button &&
                 button.ButtonIndex == MouseButton.Left && !button.Pressed)
        {
            int from = _held;
            int to = HitTest(GetLocalMousePosition());
            _held = -1;
            _pointer = _mouseStart;
            if (to >= 0)
            {
                int previousMergeCount = _game.State.TotalMerges;
                bool modified = _game.Interact(from, to);
                if (_game.State.TotalMerges > previousMergeCount) BeginSpark(to);
                ActionFinished?.Invoke(modified);
                GetViewport().SetInputAsHandled();
            }
            QueueRedraw();
        }
    }

    private void BeginSpark(int index)
    {
        _sparkTile = index;
        _sparkTime = 0.45f;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _sparkTime -= (float)delta;
        if (_sparkTime <= 0f)
        {
            _sparkTime = 0f;
            SetProcess(false);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X < 50 || Size.Y < 50) return;
        var (cell, x, y) = Geometry();
        float boardWidth = cell * FoodCatalog.Columns + Gap * (FoodCatalog.Columns - 1);
        float boardHeight = cell * FoodCatalog.Rows + Gap * (FoodCatalog.Rows - 1);

        var bezel = Style(new Color("#D48E84"), new Color("#F9C8AF"), 6, 20);
        DrawStyleBox(bezel, new Rect2(x - 11, y - 11, boardWidth + 22, boardHeight + 22));
        DrawStyleBox(Style(new Color("#D78C85"), new Color("#B97873"), 1, 11),
            new Rect2(x - 1, y - 1, boardWidth + 2, boardHeight + 2));

        for (int i = 0; i < FoodCatalog.CellCount; i++)
        {
            int row = i / FoodCatalog.Columns;
            int col = i % FoodCatalog.Columns;
            var square = new Rect2(x + col * (cell + Gap), y + row * (cell + Gap), cell, cell);
            bool alternate = (row + col) % 2 == 0;
            var baseColor = alternate ? new Color("#F9C9C4") : new Color("#ECAFAD");
            DrawStyleBox(Style(baseColor, new Color("#F9DDD2"), 1, 4), square);

            TileData? tile = _game.State.Tiles[i];
            if (tile == null)
            {
                if (row % 3 == 0 && col % 4 == 0)
                    DrawCircle(square.GetCenter(), 3f, new Color("#FFFFFF", 0.20f));
                continue;
            }

            var plate = new Rect2(square.Position + new Vector2(3f, 4f),
                                  square.Size - new Vector2(6f, 9f));
            DrawStyleBox(Style(alternate ? new Color("#FBE4D8") : new Color("#F9D8D1"),
                new Color("#FFFFFF", 0.22f), 1, 10), plate);

            bool selected = _game.Selected == i;
            bool hinted = _game.HintOne == i || _game.HintTwo == i;
            if (selected || hinted)
            {
                DrawStyleBox(Style(new Color(1, 1, 1, 0f),
                    selected ? new Color("#26E4A7") : new Color("#FFE665"), 4, 11),
                    plate.Grow(2));
                DrawCornerMarks(plate, selected ? new Color("#15D8A2") : new Color("#F8D249"));
            }

            float scale = Mathf.Clamp(cell * 0.49f, 20f, 37f);
            FoodArt.Draw(this, square.GetCenter() + new Vector2(0, -2f), scale,
                tile.Family, tile.Tier);

            // Tiny tier medallion; no intrusive labels over the colorful illustrations.
            DrawCircle(square.Position + new Vector2(cell - 10f, 11f), 10f,
                new Color("#F8D45B"));
            DrawCircle(square.Position + new Vector2(cell - 10f, 11f), 8f,
                new Color("#FFF3BF"));
            DrawString(GetThemeDefaultFont(), square.Position + new Vector2(cell - 20f, 16f),
                tile.Tier.ToString(), HorizontalAlignment.Center, 20, 11, new Color("#93613F"));
        }

        if (_sparkTime > 0f && _sparkTile >= 0)
        {
            Vector2 center = CenterOf(_sparkTile);
            float alpha = Mathf.Clamp(_sparkTime * 2.2f, 0f, 1f);
            float expansion = (0.45f - _sparkTime) * 40f;
            DrawArc(center, 22f + expansion, 0, Mathf.Tau, 36,
                new Color("#FFE966", alpha), 3f, true);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.Tau / 8f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(center + direction * (23f + expansion * 0.5f),
                    center + direction * (31f + expansion), new Color("#FFFCE2", alpha), 3f, true);
            }
        }

        if (_held >= 0 && _game.State.Tiles[_held] is { } dragged &&
            _mouseStart.DistanceTo(_pointer) > 10f)
        {
            DrawCircle(_pointer, cell * 0.42f, new Color("#FFFFFF", 0.72f));
            FoodArt.Draw(this, _pointer, cell * 0.48f, dragged.Family, dragged.Tier);
        }
    }

    private void DrawCornerMarks(Rect2 rect, Color color)
    {
        const float d = 12f;
        const float w = 4f;
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? rect.Position.X : rect.End.X;
            float y = i / 2 == 0 ? rect.Position.Y : rect.End.Y;
            float dx = i % 2 == 0 ? d : -d;
            float dy = i / 2 == 0 ? d : -d;
            DrawLine(new Vector2(x, y), new Vector2(x + dx, y), color, w, true);
            DrawLine(new Vector2(x, y), new Vector2(x, y + dy), color, w, true);
        }
    }

    private static StyleBoxFlat Style(Color fill, Color border, int borderWidth, int radius)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(radius);
        return style;
    }
}
