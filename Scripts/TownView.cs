using Godot;
using MergeHarvest;
using System;

public partial class TownView : Control
{
    private readonly GameData _game;
    private int _selected;
    private Label _title = null!;
    private Label _description = null!;
    private Label _progress = null!;
    private TextureRect _buildingPicture = null!;
    private HBoxContainer _materials = null!;
    private VBoxContainer _sidebar = null!;
    private Button _build = null!;
    private Label _coins = null!;
    private Label _message = null!;

    private static readonly Color Ink = new("#754C3C");
    private static readonly Color Cream = new("#FFF7DE");
    private static readonly Color Teal = new("#248F83");

    public event Action? Changed;

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
        _selected = _game.State.Town.SelectedBuilding;
        Visible = true;
        Refresh();
    }

    public void Close() => Visible = false;

    private void Build()
    {
        var dimmer = new ColorRect { Color = new Color("#132932", 0.84f) };
        dimmer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(dimmer);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = Panel(new Color("#F4D3A6"), 18, new Color("#FFF6D4"), 5, 24);
        panel.CustomMinimumSize = new Vector2(1110, 886);
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 13);
        column.AddChild(header);
        var headings = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(headings);
        headings.AddChild(Label("BUILD YOUR ISLAND", 30, Ink, true));
        headings.AddChild(Label("COLLECT RESOURCES • SPEND COINS • UPGRADE BUILDINGS", 13, Teal, true));
        _coins = Label("", 17, Ink, true);
        header.AddChild(_coins);
        header.AddChild(Button("CLOSE", new Color("#B7716E"), Close, 52));

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 15);
        column.AddChild(body);

        var selector = Panel(new Color("#E0F2D8"), 11, new Color("#B7D4B4"), 2, 18);
        selector.CustomMinimumSize = new Vector2(290, 0);
        body.AddChild(selector);
        _sidebar = new VBoxContainer();
        _sidebar.AddThemeConstantOverride("separation", 8);
        selector.AddChild(_sidebar);

        var details = Panel(new Color("#FFF9EB"), 17, new Color("#E0B99B"), 3, 18);
        details.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        details.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(details);

        var info = new VBoxContainer();
        info.AddThemeConstantOverride("separation", 11);
        details.AddChild(info);

        _title = Label("", 26, Ink, true);
        info.AddChild(_title);
        _description = Label("", 13, new Color("#A1816B"));
        info.AddChild(_description);
        _buildingPicture = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(0, 360),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        info.AddChild(_buildingPicture);
        _progress = Label("", 17, Ink, true);
        info.AddChild(_progress);

        info.AddChild(Label("CONSTRUCTION MATERIALS", 13, Ink, true));
        _materials = new HBoxContainer();
        _materials.AddThemeConstantOverride("separation", 8);
        info.AddChild(_materials);
        _build = Button("BUILD", new Color("#67B86A"), Construct, 56);
        info.AddChild(_build);

        var bottom = Panel(new Color("#FFF2D0"), 12, new Color("#E9C59B"), 2, 16);
        column.AddChild(bottom);
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 10);
        bottom.AddChild(bottomRow);
        bottomRow.AddChild(Label("INVENTORY", 16, Ink, true));

        for (int i = 0; i < 4; i++)
        {
            int k = i;
            var icon = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://Assets/Materials/" +
                    TownCatalog.MaterialIcons[i] + ".svg"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(55, 38)
            };
            bottomRow.AddChild(icon);
            var number = Label("", 14, Teal, true);
            number.SetMeta("material_index", k);
            bottomRow.AddChild(number);
        }
        bottomRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _message = Label("", 12, Ink, false, true);
        _message.CustomMinimumSize = new Vector2(220, 0);
        bottomRow.AddChild(_message);
    }

    private void Refresh()
    {
        _coins.Text = "COINS: " + _game.State.Coins.ToString("N0");
        _message.Text = _game.Message;
        foreach (Node child in _sidebar.GetChildren())
        {
            _sidebar.RemoveChild(child);
            child.QueueFree();
        }
        for (int i = 0; i < TownCatalog.BuildingCount; i++)
        {
            int id = i;
            var data = TownCatalog.Buildings[i];
            var b = Button(
                (i == _selected ? "● " : "") + data.Name.ToUpperInvariant() +
                "    " + _game.Town.Stage(i) + "/3" +
                (_game.Town.Unlocked(i) ? "" : "  [LV " + data.UnlockLevel + "]"),
                i == _selected ? new Color("#E59B5F") : new Color("#67B7A2"),
                () => { _selected = id; _game.State.Town.SelectedBuilding = id; Refresh(); }, 67);
            _sidebar.AddChild(b);
        }

        var selected = TownCatalog.Buildings[_selected];
        int stage = _game.Town.Stage(_selected);
        bool unlocked = _game.Town.Unlocked(_selected);
        bool complete = stage == TownCatalog.MaxStage;
        _title.Text = selected.Name;
        _description.Text = selected.Description;
        _progress.Text = complete ? "FULLY UPGRADED  •  3 / 3" :
            unlocked ? "STAGE " + stage + " / 3    →    " + (stage + 1) + " / 3" :
            "LOCKED UNTIL LEVEL " + selected.UnlockLevel;
        _buildingPicture.Texture = GD.Load<Texture2D>(
            "res://Assets/Buildings/" + _selected + ".svg");
        _buildingPicture.Modulate = stage == 0 ? new Color(1, 1, 1, 0.70f) : Colors.White;

        foreach (Node child in _materials.GetChildren())
        {
            _materials.RemoveChild(child);
            child.QueueFree();
        }

        var cost = complete ? new ConstructionCost(0, 0, 0, 0, 0) :
            TownCatalog.GetCost(_selected, stage + 1);
        for (int i = 0; i < 4; i++)
        {
            int owned = _game.State.Town.Materials[i];
            int needed = cost.GetMaterial(i);
            var panel = Panel(new Color("#FBE8CC"), 8,
                owned >= needed ? new Color("#80B88D") : new Color("#D88477"), 2, 12);
            panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _materials.AddChild(panel);
            var item = new VBoxContainer();
            panel.AddChild(item);
            var icon = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://Assets/Materials/" +
                    TownCatalog.MaterialIcons[i] + ".svg"),
                CustomMinimumSize = new Vector2(80, 63),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            item.AddChild(icon);
            var label = Label(TownCatalog.MaterialNames[i] + " " + owned + "/" + needed, 13,
                owned >= needed ? Teal : new Color("#C36B5A"), true);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            item.AddChild(label);
        }

        _build.Disabled = !unlocked || complete || !_game.Town.CanBuild(_selected);
        _build.Text = complete ? "BUILDING COMPLETE" :
            !unlocked ? "REACH LEVEL " + selected.UnlockLevel :
            "BUILD / UPGRADE  •  " + cost.Coins + " COINS";
        _build.AddThemeColorOverride("font_disabled_color", new Color("#F7E7D7"));

        // Labels with an index are dynamic inventory indicators.
        foreach (Node child in _message.GetParent().GetChildren())
            if (child is Godot.Label l && l.HasMeta("material_index"))
                l.Text = _game.State.Town.Materials[l.GetMeta("material_index").AsInt32()].ToString();
    }

    private void Construct()
    {
        if (_game.Town.Construct(_selected)) Changed?.Invoke();
        Refresh();
    }

    private static PanelContainer Panel(Color fill, int margin, Color edge, int border, int radius)
    {
        var p = new PanelContainer();
        var box = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        box.SetBorderWidthAll(border);
        box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = margin;
        box.ContentMarginRight = margin;
        box.ContentMarginTop = margin;
        box.ContentMarginBottom = margin;
        p.AddThemeStyleboxOverride("panel", box);
        return p;
    }

    private static Label Label(string text, int size, Color color, bool bold = false, bool wrap = false)
    {
        var label = new Label { Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (bold) label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.13f));
        return label;
    }

    private static Button Button(string text, Color color, Action action, int height)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, height),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        button.AddThemeFontSizeOverride("font_size", 13);
        foreach (var name in new[] { "normal", "hover", "pressed", "disabled" })
        {
            var box = new StyleBoxFlat
            {
                BgColor = name == "disabled" ? color.Darkened(0.25f) :
                    name == "hover" ? color.Lightened(0.10f) : color,
                BorderColor = new Color("#FFFFFF", 0.75f)
            };
            box.SetCornerRadiusAll(12);
            box.SetBorderWidthAll(2);
            button.AddThemeStyleboxOverride(name, box);
        }
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.Pressed += action;
        return button;
    }
}
