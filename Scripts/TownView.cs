using Godot;
using MergeHarvest;
using System;

public partial class TownView : Control
{
    private readonly GameData _game;
    private int _selected;
    private TownMapView _map = null!;
    private Label _heading = null!;
    private Label _description = null!;
    private Label _status = null!;
    private Label _resources = null!;
    private Label _funds = null!;
    private TextureRect _preview = null!;
    private GridContainer _materialGrid = null!;
    private Button _build = null!;

    public event Action? Changed;

    private static readonly Color Ink = new("#795241");
    private static readonly Color Teal = new("#248C81");

    public TownView(GameData game)
    {
        _game = game;
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        Build();
    }

    public void Open()
    {
        _selected = Math.Clamp(_game.State.Town.SelectedBuilding, 0,
            TownCatalog.BuildingCount - 1);
        Visible = true;
        _map.Select(_selected);
        Refresh();
    }

    public void Close() => Visible = false;

    private void Build()
    {
        var shade = new ColorRect { Color = new Color("#132830", 0.88f) };
        shade.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(shade);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = Panel(new Color("#F4D6B0"), 13, new Color("#FFF3D8"), 4, 22);
        panel.CustomMinimumSize = new Vector2(1150, 900);
        center.AddChild(panel);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);
        var titleArea = new VBoxContainer();
        titleArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(titleArea);
        titleArea.AddChild(Text("YOUR ISLAND — 20 BUILDINGS", 26, Ink, true));
        titleArea.AddChild(Text("SELECT A LOT • UNLOCK BY LEVEL • BUILD AND UPGRADE", 13, Teal, true));
        _funds = Text("", 17, Ink, true);
        header.AddChild(_funds);
        header.AddChild(Action("CLOSE", new Color("#B36F69"), Close, 48));

        var body = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 10);
        root.AddChild(body);

        _map = new TownMapView(_game)
        {
            CustomMinimumSize = new Vector2(760, 630),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _map.BuildingSelected += index =>
        {
            _selected = index;
            _game.State.Town.SelectedBuilding = index;
            Refresh();
        };
        body.AddChild(_map);

        var sidebar = Panel(new Color("#FFF9ED"), 11, new Color("#D7AE83"), 2, 16);
        sidebar.CustomMinimumSize = new Vector2(310, 0);
        body.AddChild(sidebar);
        var info = new VBoxContainer();
        info.AddThemeConstantOverride("separation", 6);
        sidebar.AddChild(info);

        _heading = Text("", 21, Ink, true, true);
        info.AddChild(_heading);
        _description = Text("", 12, new Color("#9E7A64"), false, true);
        info.AddChild(_description);
        _preview = new TextureRect
        {
            CustomMinimumSize = new Vector2(260, 176),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        info.AddChild(_preview);
        _status = Text("", 15, Teal, true, true);
        info.AddChild(_status);
        info.AddChild(Text("REQUIRED MATERIALS", 12, Ink, true));
        _materialGrid = new GridContainer { Columns = 2 };
        _materialGrid.AddThemeConstantOverride("h_separation", 6);
        _materialGrid.AddThemeConstantOverride("v_separation", 6);
        info.AddChild(_materialGrid);
        info.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        _build = Action("BUILD", new Color("#55B567"), Construct, 54);
        info.AddChild(_build);

        var footer = Panel(new Color("#FFF5D9"), 8, new Color("#DBB68E"), 2, 12);
        root.AddChild(footer);
        _resources = Text("", 14, Ink, true);
        footer.AddChild(_resources);
    }

    private void Refresh()
    {
        var building = TownCatalog.Buildings[_selected];
        int stage = _game.Town.Stage(_selected);
        bool unlocked = _game.Town.Unlocked(_selected);
        bool maxed = stage >= TownCatalog.MaxStage;

        _heading.Text = building.Name;
        _description.Text = building.Description;
        _preview.Texture = GD.Load<Texture2D>("res://Assets/Buildings/" + _selected + ".svg");
        _preview.Modulate = !unlocked ? new Color(0.6f, 0.6f, 0.6f) :
            stage == 0 ? new Color(1f, 1f, 1f, 0.68f) : Colors.White;
        _status.Text = !unlocked ? "LOCKED • UNLOCKS AT LEVEL " + building.UnlockLevel :
            maxed ? "COMPLETED • LEVEL 3 / 3" : "BUILDING LEVEL " + stage + " → " + (stage + 1);
        _funds.Text = "COINS: " + _game.State.Coins.ToString("N0");
        _resources.Text = "INVENTORY   " + string.Join("     ",
            Array.ConvertAll(TownCatalog.MaterialNames, (name) =>
            {
                int i = Array.IndexOf(TownCatalog.MaterialNames, name);
                return name + ": " + _game.State.Town.Materials[i];
            }));

        foreach (Node child in _materialGrid.GetChildren())
        {
            _materialGrid.RemoveChild(child);
            child.QueueFree();
        }
        ConstructionCost cost = maxed ? new ConstructionCost(0, 0, 0, 0, 0) :
            TownCatalog.GetCost(_selected, stage + 1);
        for (int i = 0; i < 4; i++)
        {
            int owned = _game.State.Town.Materials[i];
            int required = cost.GetMaterial(i);
            var mat = Panel(new Color("#FFEED5"), 5,
                owned >= required ? new Color("#8EC89C") : new Color("#D99785"), 2, 9);
            mat.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _materialGrid.AddChild(mat);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 3);
            mat.AddChild(row);
            row.AddChild(new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://Assets/Materials/" +
                    TownCatalog.MaterialIcons[i] + ".svg"),
                CustomMinimumSize = new Vector2(47, 37),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });
            row.AddChild(Text(owned + " / " + required, 13,
                owned >= required ? Teal : new Color("#BC665C"), true));
        }

        _build.Text = maxed ? "FULLY UPGRADED" :
            !unlocked ? "UNLOCK AT LEVEL " + building.UnlockLevel :
            "BUILD / UPGRADE • " + cost.Coins + " COINS";
        _build.Disabled = maxed || !unlocked || !_game.Town.CanBuild(_selected);
        _map.Select(_selected);
    }

    private void Construct()
    {
        if (_game.Town.Construct(_selected)) Changed?.Invoke();
        Refresh();
    }

    private static PanelContainer Panel(Color fill, int margin, Color edge, int border, int radius)
    {
        var p = new PanelContainer();
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        style.SetBorderWidthAll(border);
        style.SetCornerRadiusAll(radius);
        style.ContentMarginLeft = margin;
        style.ContentMarginRight = margin;
        style.ContentMarginTop = margin;
        style.ContentMarginBottom = margin;
        p.AddThemeStyleboxOverride("panel", style);
        return p;
    }

    private static Label Text(string value, int size, Color color, bool bold = false, bool wrap = false)
    {
        var l = new Label
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        if (bold) l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.09f));
        return l;
    }

    private static Button Action(string title, Color color, Action callback, int height)
    {
        var b = new Button
        {
            Text = title,
            CustomMinimumSize = new Vector2(0, height),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled" })
        {
            var box = new StyleBoxFlat
            {
                BgColor = state == "disabled" ? color.Darkened(0.27f) :
                    state == "hover" ? color.Lightened(0.13f) : color,
                BorderColor = new Color("#FFFFFF", 0.7f)
            };
            box.SetCornerRadiusAll(12);
            box.SetBorderWidthAll(1);
            b.AddThemeStyleboxOverride(state, box);
        }
        b.AddThemeFontSizeOverride("font_size", 13);
        b.AddThemeColorOverride("font_color", Colors.White);
        b.AddThemeColorOverride("font_hover_color", Colors.White);
        b.AddThemeColorOverride("font_pressed_color", Colors.White);
        b.AddThemeColorOverride("font_disabled_color", new Color("#F5E7D7"));
        b.Pressed += callback;
        return b;
    }
}
