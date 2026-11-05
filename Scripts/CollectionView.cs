using Godot;
using MergeHarvest;
using System;

// Complete collection book: 23 families × 10 items, building requirements and generator selection.
public partial class CollectionView : Control
{
    private readonly GameData _game;
    private VBoxContainer _families = null!;
    private GridContainer _items = null!;
    private Label _name = null!;
    private Label _requirements = null!;
    private Label _progress = null!;
    private Button _use = null!;
    private int _selected;

    public event Action<int>? SelectRequested;

    private static readonly Color Ink = new("#825846");
    private static readonly Color Teal = new("#278E83");

    public CollectionView(GameData game)
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
        _selected = _game.State.SelectedGenerator;
        Visible = true;
        RefreshPage();
    }

    public void Close() => Visible = false;

    private void Build()
    {
        var backdrop = new ColorRect { Color = new Color("#152D34", 0.88f) };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var book = Box(new Color("#F7D5AF"), new Color("#FFF8E8"), 4, 22, 14);
        book.CustomMinimumSize = new Vector2(1170, 916);
        center.AddChild(book);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        book.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);
        header.AddChild(Label("MERGE COLLECTIONS", 29, Ink, true));
        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _progress = Label("", 15, Teal, true);
        header.AddChild(_progress);
        header.AddChild(Button("CLOSE", new Color("#AF726D"), Close, 45));

        var body = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 14);
        root.AddChild(body);

        var left = Box(new Color("#E5F3DC"), new Color("#B9DBCA"), 2, 18, 10);
        left.CustomMinimumSize = new Vector2(330, 0);
        body.AddChild(left);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 7);
        left.AddChild(col);
        col.AddChild(Label("ALL 23 COLLECTIONS", 17, Ink, true));

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        col.AddChild(scroll);
        _families = new VBoxContainer();
        _families.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _families.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_families);

        var right = Box(new Color("#FFF8E7"), new Color("#E3B69B"), 2, 19, 14);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(right);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        right.AddChild(content);

        _name = Label("", 23, Ink, true);
        content.AddChild(_name);
        _requirements = Label("", 14, Teal, true, true);
        content.AddChild(_requirements);
        content.AddChild(Label("MERGE TWO MATCHING TILES TO REACH THE NEXT TIER", 12, new Color("#B48A70"), true));
        _items = new GridContainer
        {
            Columns = 5,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _items.AddThemeConstantOverride("h_separation", 9);
        _items.AddThemeConstantOverride("v_separation", 9);
        content.AddChild(_items);
        var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        content.AddChild(spacer);
        _use = Button("USE GENERATOR", new Color("#58B875"), UseSelected, 52);
        content.AddChild(_use);

        root.AddChild(Label("20 NEW COLLECTIONS ARE UNLOCKED BY CONSTRUCTING THEIR BUILDINGS. ALL ITEMS ARE FREE TO GENERATE ONCE UNLOCKED.", 12, Ink, true, true));
    }

    private void UseSelected()
    {
        if (CollectionCatalog.IsUnlocked(_selected, _game.Town))
            SelectRequested?.Invoke(_selected);
    }

    private void RefreshPage()
    {
        foreach (Node child in _families.GetChildren())
        {
            _families.RemoveChild(child);
            child.QueueFree();
        }
        foreach (Node child in _items.GetChildren())
        {
            _items.RemoveChild(child);
            child.QueueFree();
        }

        int unlockedCount = 0;
        for (int family = 0; family < CollectionCatalog.TotalCount; family++)
        {
            int index = family;
            bool unlocked = CollectionCatalog.IsUnlocked(family, _game.Town);
            if (unlocked) unlockedCount++;
            string caption = (unlocked ? "✓ " : "🔒 ") +
                CollectionCatalog.FamilyNames[family];
            var b = Button(caption, _selected == family ? new Color("#E5A16A") :
                unlocked ? new Color("#76B8A0") : new Color("#A0AAA6"),
                () => { _selected = index; RefreshPage(); }, 33);
            b.Alignment = HorizontalAlignment.Left;
            b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _families.AddChild(b);
        }

        _progress.Text = "UNLOCKED " + unlockedCount + " / " + CollectionCatalog.TotalCount +
            "    •    " + CollectionCatalog.TotalItems + " ITEMS";
        bool available = CollectionCatalog.IsUnlocked(_selected, _game.Town);
        _name.Text = CollectionCatalog.FamilyNames[_selected].ToUpperInvariant();

        int building = CollectionCatalog.BuildingForFamily(_selected);
        _requirements.Text = building < 0 ? "STARTER COLLECTION • AVAILABLE FROM THE BEGINNING" :
            available ? "UNLOCKED BY " + TownCatalog.Buildings[building].Name.ToUpperInvariant() :
            "LOCKED • BUILD " + TownCatalog.Buildings[building].Name.ToUpperInvariant() +
            " (LEVEL " + TownCatalog.Buildings[building].UnlockLevel + ")";

        for (int tier = 1; tier <= CollectionCatalog.ItemsPerCollection; tier++)
        {
            var tile = Box(available ? new Color("#F9E5CE") : new Color("#E1D6CF"),
                available ? new Color("#EBC09B") : new Color("#C5B8AE"), 2, 13, 6);
            tile.CustomMinimumSize = new Vector2(0, 156);
            tile.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _items.AddChild(tile);
            var stack = new VBoxContainer();
            stack.AddThemeConstantOverride("separation", 3);
            tile.AddChild(stack);
            stack.AddChild(Label("TIER " + tier, 12, Ink, true));

            var picture = new FoodPreview
            {
                Family = _selected,
                Tier = tier,
                CustomMinimumSize = new Vector2(0, 85),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                Modulate = available ? Colors.White : new Color(0.55f, 0.55f, 0.55f, 0.65f)
            };
            stack.AddChild(picture);
            var text = Label(available ? CollectionCatalog.ItemName(_selected, tier) :
                "LOCKED", 12, Ink, true, true);
            text.HorizontalAlignment = HorizontalAlignment.Center;
            stack.AddChild(text);
        }
        _use.Disabled = !available;
        _use.Text = available ? "USE " + CollectionCatalog.GeneratorNames[_selected].ToUpperInvariant() :
            "BUILD THE LOCATION FIRST";
    }

    private static PanelContainer Box(Color fill, Color border, int width, int radius, int pad)
    {
        var p = new PanelContainer();
        var b = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        b.SetCornerRadiusAll(radius);
        b.SetBorderWidthAll(width);
        b.ContentMarginLeft = pad;
        b.ContentMarginTop = pad;
        b.ContentMarginRight = pad;
        b.ContentMarginBottom = pad;
        p.AddThemeStyleboxOverride("panel", b);
        return p;
    }

    private static Label Label(string value, int size, Color color, bool bold = false, bool wrap = false)
    {
        var l = new Label
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        if (bold) l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.1f));
        return l;
    }

    private static Button Button(string text, Color fill, Action pressed, int height)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, height),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
        {
            var sb = new StyleBoxFlat
            {
                BgColor = state == "disabled" ? fill.Darkened(0.22f) :
                    state == "hover" ? fill.Lightened(0.13f) : fill,
                BorderColor = new Color("#FFFFFF", 0.7f)
            };
            sb.SetCornerRadiusAll(10);
            sb.SetBorderWidthAll(1);
            b.AddThemeStyleboxOverride(state, sb);
        }
        b.AddThemeColorOverride("font_color", Colors.White);
        b.AddThemeColorOverride("font_hover_color", Colors.White);
        b.AddThemeColorOverride("font_pressed_color", Colors.White);
        b.AddThemeColorOverride("font_disabled_color", Colors.White);
        b.AddThemeFontSizeOverride("font_size", 12);
        b.Pressed += pressed;
        return b;
    }
}
