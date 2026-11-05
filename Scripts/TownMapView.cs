using Godot;
using MergeHarvest;
using System;

// Interactive island overview. Each of the 20 lots is clickable and reflects
// unlock level, actual construction stage and the existing saved town state.
public partial class TownMapView : Control
{
    private readonly GameData _game;
    private readonly Texture2D?[] _art = new Texture2D?[TownCatalog.BuildingCount];
    private int _selected;
    private int _hovered = -1;

    public event Action<int>? BuildingSelected;
    public const int Columns = 5;
    public const int Rows = 4;

    public TownMapView(GameData game)
    {
        _game = game;
        CustomMinimumSize = new Vector2(770, 575);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        for (int i = 0; i < TownCatalog.BuildingCount; i++)
            _art[i] = GD.Load<Texture2D>("res://Assets/Buildings/" + i + ".svg");
        Resized += QueueRedraw;
    }

    public void Select(int building)
    {
        _selected = Mathf.Clamp(building, 0, TownCatalog.BuildingCount - 1);
        QueueRedraw();
    }

    private Rect2 Lot(int index)
    {
        float marginX = 39f;
        float marginY = 54f;
        float stepX = (Size.X - 2f * marginX) / Columns;
        float stepY = (Size.Y - marginY - 29f) / Rows;
        int col = index % Columns;
        int row = index / Columns;
        // Stagger the locations to give the town the feel of a winding island.
        float offset = (row % 2 == 0 ? 0.20f : -0.17f) * stepX;
        float x = marginX + col * stepX + offset;
        float y = marginY + row * stepY;
        return new Rect2(x, y, stepX * 0.83f, stepY * 0.85f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            int hover = Hit(motion.Position);
            if (hover != _hovered)
            {
                _hovered = hover;
                QueueRedraw();
            }
        }
        else if (@event is InputEventMouseButton click &&
                 click.ButtonIndex == MouseButton.Left && click.Pressed)
        {
            int index = Hit(click.Position);
            if (index >= 0)
            {
                _selected = index;
                BuildingSelected?.Invoke(index);
                QueueRedraw();
                AcceptEvent();
            }
        }
    }

    private int Hit(Vector2 point)
    {
        for (int i = TownCatalog.BuildingCount - 1; i >= 0; i--)
            if (Lot(i).Grow(4).HasPoint(point)) return i;
        return -1;
    }

    private static StyleBoxFlat Style(Color fill, Color edge, int border = 2, int radius = 14)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        style.SetBorderWidthAll(border);
        style.SetCornerRadiusAll(radius);
        return style;
    }

    public override void _Draw()
    {
        if (Size.X < 150f || Size.Y < 150f) return;

        DrawStyleBox(Style(new Color("#75C9D2"), new Color("#F6E5B1"), 5, 25),
            new Rect2(Vector2.Zero, Size));
        DrawCircle(new Vector2(Size.X * 0.84f, 57f), 29, new Color("#FFEBA0", 0.9f));
        DrawArc(new Vector2(Size.X * 0.84f, 57f), 36f, 0, Mathf.Tau, 32,
            new Color("#FFF6CC", 0.65f), 3, true);

        // Winding sandy island surrounded by turquoise shallow water.
        var coast = new Vector2[]
        {
            new(25, 72), new(Size.X * 0.3f, 23), new(Size.X * 0.73f, 36),
            new(Size.X - 25, 93), new(Size.X - 25, Size.Y * 0.43f),
            new(Size.X - 47, Size.Y - 43), new(Size.X * 0.66f, Size.Y - 17),
            new(Size.X * 0.24f, Size.Y - 25), new(24, Size.Y - 81)
        };
        DrawColoredPolygon(coast, new Color("#F0D49A"));
        for (int row = 0; row < Rows; row++)
        {
            float y = 62 + row * (Size.Y - 83f) / Rows;
            DrawLine(new Vector2(45, y + 71), new Vector2(Size.X - 57, y + 75),
                new Color("#D4B57E", 0.55f), 13, true);
        }
        for (int i = 0; i < TownCatalog.BuildingCount; i++)
        {
            Rect2 lot = Lot(i);
            int stage = _game.Town.Stage(i);
            bool unlocked = _game.Town.Unlocked(i);
            bool selected = _selected == i;
            bool hovered = _hovered == i;
            Color border = selected ? new Color("#FFF26C") :
                hovered ? new Color("#FFFFFF") : new Color("#D6AF78");
            Color background = !unlocked ? new Color("#909A99") :
                stage == 0 ? new Color("#E9D8B3") : new Color("#D5EAB2");
            DrawStyleBox(Style(background, border, selected ? 5 : 2, 13), lot);
            Rect2 preview = new Rect2(lot.Position + new Vector2(4, 4),
                new Vector2(lot.Size.X - 8, lot.Size.Y * 0.69f));
            if (_art[i] != null)
                DrawTextureRect(_art[i]!, preview, false,
                    !unlocked ? new Color(0.55f, 0.57f, 0.58f, 0.80f) :
                    stage == 0 ? new Color(1f, 1f, 1f, 0.48f) : Colors.White);
            if (!unlocked)
            {
                DrawCircle(lot.GetCenter() - new Vector2(0, 12), 17,
                    new Color("#575E69", 0.9f));
                DrawString(GetThemeDefaultFont(), lot.Position + new Vector2(0, lot.Size.Y * 0.53f),
                    "LV " + TownCatalog.Buildings[i].UnlockLevel,
                    HorizontalAlignment.Center, lot.Size.X, 16, Colors.White);
            }
            string name = TownCatalog.Buildings[i].Name;
            DrawString(GetThemeDefaultFont(), lot.Position +
                new Vector2(3, lot.Size.Y - 19), name, HorizontalAlignment.Center,
                lot.Size.X - 6, 12, new Color("#765448"));
            DrawString(GetThemeDefaultFont(), lot.Position +
                new Vector2(3, lot.Size.Y - 3),
                unlocked ? "STAGE " + stage + " / " + TownCatalog.MaxStage : "LOCKED",
                HorizontalAlignment.Center, lot.Size.X - 6, 12,
                stage == TownCatalog.MaxStage ? new Color("#238558") :
                unlocked ? new Color("#976E45") : new Color("#62585D"));
        }
    }
}
