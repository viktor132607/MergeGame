using Godot;
using MergeHarvest;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

// Tasty-Travels-like casual merge layout, built exclusively from original Godot UI and vector drawing.
public partial class GameRoot : Control
{
    private const string SaveFile = "user://merge_harvest_save.json";
    private readonly GameData _game = new();
    private BoardView _board = null!;
    private Label _level = null!;
    private Label _coins = null!;
    private Label _energy = null!;
    private Label _served = null!;
    private Label _status = null!;
    private ProgressBar _progress = null!;
    private HBoxContainer _orders = null!;
    private CardAlbumView _album = null!;
    private TownView _townView = null!;
    private Label _albumCount = null!;
    private OptionButton _generatorChoice = null!;
    private FoodPreview _generatorPreview = null!;
    private Label _generatorInfo = null!;
    private int _knownGenerators = -1;
    private CollectionView _collections = null!;
    private bool _resetPending;

    private static readonly Color Navy = new("#316274");
    private static readonly Color Teal = new("#168D80");
    private static readonly Color DeepTeal = new("#086C70");
    private static readonly Color Cream = new("#FFF9E7");
    private static readonly Color Brown = new("#865B50");
    private static readonly Color Coral = new("#F68D72");
    private static readonly Color Gold = new("#FFC34F");

    public override void _Ready()
    {
        Load();
        Build();
        if (System.Environment.GetEnvironmentVariable("MERGE_CARD_TEST") == "1")
        {
            CardAlbumChecks.Run();
            TownChecks.Run();
            CollectionChecks.Run();
            SpawnerChecks.Run();
            _album.Open();
            _townView.Open();
        }
        Refresh();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) Save();
    }

    private void Build()
    {
        // The art is a real imported game texture, not a placeholder color field.
        var scenic = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://Assets/UI/island_scene.svg"),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        scenic.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(scenic);

        var centering = new CenterContainer();
        centering.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centering);

        var shell = Panel(new Color("#184E60", 0.30f), 15,
            new Color("#FFF5D7", 0.86f), 4, 31);
        shell.CustomMinimumSize = new Vector2(1710, 1000);
        centering.AddChild(shell);

        var vertical = new VBoxContainer();
        vertical.AddThemeConstantOverride("separation", 9);
        shell.AddChild(vertical);
        BuildHeader(vertical);

        var orderShelf = Panel(new Color("#D18D64"), 9,
            new Color("#FFE7B4"), 4, 23);
        vertical.AddChild(orderShelf);
        _orders = new HBoxContainer();
        _orders.AddThemeConstantOverride("separation", 10);
        orderShelf.AddChild(_orders);

        var middle = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        middle.AddThemeConstantOverride("separation", 12);
        vertical.AddChild(middle);
        BuildGenerators(middle);
        BuildBoard(middle);
        BuildTools(middle);
        BuildFooter(vertical);

        _album = new CardAlbumView(_game);
        _album.StateChanged += () => { Save(); Refresh(); };
        AddChild(_album);

        _townView = new TownView(_game);
        _townView.Changed += () => { Save(); Refresh(); };
        AddChild(_townView);

        _collections = new CollectionView(_game);
        _collections.SelectRequested += family =>
        {
            if (_game.SelectGenerator(family))
            {
                Save();
                Refresh();
            }
            _collections.Close();
        };
        AddChild(_collections);
    }

    private void BuildHeader(VBoxContainer parent)
    {
        var bar = Panel(new Color("#155A83", 0.97f), 12,
            new Color("#B7F3F1"), 3, 28);
        parent.AddChild(bar);
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 65) };
        row.AddThemeConstantOverride("separation", 16);
        bar.AddChild(row);

        var emblem = Panel(new Color("#FFD999"), 9, new Color("#FFF7D0"), 3, 33);
        emblem.CustomMinimumSize = new Vector2(70, 60);
        var sun = LabelText("☀", 34, new Color("#D7883C"), true);
        sun.HorizontalAlignment = HorizontalAlignment.Center;
        emblem.AddChild(sun);
        row.AddChild(emblem);

        var brand = new VBoxContainer { CustomMinimumSize = new Vector2(270, 0) };
        brand.AddThemeConstantOverride("separation", 0);
        row.AddChild(brand);
        brand.AddChild(LabelText("MERGE HARVEST", 31, new Color("#FFF5CF"), true));
        brand.AddChild(LabelText("ISLAND ADVENTURE", 14, new Color("#B1F6EB"), true));
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        _level = LabelText("1", 25, new Color("#6E5539"), true);
        row.AddChild(Stat("★  LEVEL", _level, new Color("#FFF1A9"), 116));

        _energy = LabelText("∞", 25, Colors.White, true);
        row.AddChild(Stat("⚡  ENERGY", _energy, new Color("#239BC4"), 130));

        _coins = LabelText("100", 25, new Color("#FFF3B0"), true);
        row.AddChild(Stat("●  COINS", _coins, new Color("#2C657C"), 148));

        row.AddChild(Button("ALBUM", new Color("#E9AA52"), ShowAlbum, 62));
        row.AddChild(Button("TOWN MAP", new Color("#77BD8D"), ShowTown, 62));
    }

    private static PanelContainer Stat(string name, Label value, Color fill, float width)
    {
        var panel = Panel(fill, 7, new Color("#FFFFFF", 0.64f), 2, 24);
        panel.CustomMinimumSize = new Vector2(width, 61);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 0);
        panel.AddChild(stack);
        stack.AddChild(LabelText(name, 12,
            fill.R + fill.G + fill.B > 2.1f ? new Color("#986846") : new Color("#E5FBFE"), true));
        stack.AddChild(value);
        return panel;
    }

    private void BuildGenerators(HBoxContainer middle)
    {
        // Slim navigation rail: item generation belongs on the actual board.
        var rail = Panel(new Color("#205D79", 0.96f), 12,
            new Color("#C3EDE5"), 3, 23);
        rail.CustomMinimumSize = new Vector2(165, 0);
        middle.AddChild(rail);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        rail.AddChild(column);

        var heading = LabelText("EXPLORE", 17, new Color("#FFF3C8"), true);
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(heading);

        column.AddChild(NavButton("▣", "COLLECTIONS", new Color("#9D82D8"), ShowCollections));
        column.AddChild(NavButton("★", "CARD ALBUM", new Color("#E8A950"), ShowAlbum));
        column.AddChild(NavButton("⌂", "TOWN MAP", new Color("#67B897"), ShowTown));
        column.AddChild(NavButton("✦", "MERGE HINT", new Color("#76B9CE"),
            () => { _game.ShowHint(); Refresh(); }));
        column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        column.AddChild(LabelText("20 BUILDINGS", 12, new Color("#DDF7D9"), true));
        column.AddChild(LabelText("23 ITEM CHAINS", 12, new Color("#DDF7D9"), true));
        column.AddChild(LabelText("FREE PLAY", 12, new Color("#FFE1A3"), true));
    }

    private static PanelContainer NavButton(string symbol, string name, Color color, Action callback)
    {
        var frame = Panel(color, 5, new Color("#FFFFFF", 0.77f), 2, 17);
        frame.CustomMinimumSize = new Vector2(0, 103);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 1);
        frame.AddChild(stack);
        var icon = LabelText(symbol, 33, Colors.White, true);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(icon);
        stack.AddChild(Button(name, color.Darkened(0.1f), callback, 37));
        return frame;
    }

    private void RefreshGenerators()
    {
        if (_generatorChoice == null) return;
        int[] unlocked = CollectionCatalog.UnlockedFamilies(_game.Town).ToArray();
        if (_knownGenerators != unlocked.Length)
        {
            _generatorChoice.Clear();
            foreach (int family in unlocked)
                _generatorChoice.AddItem(CollectionCatalog.FamilyNames[family], family);
            _knownGenerators = unlocked.Length;
        }
        int selected = _game.State.SelectedGenerator;
        int index = Array.IndexOf(unlocked, selected);
        if (index < 0) index = 0;
        _generatorChoice.Select(index);
        _generatorPreview.Family = unlocked[index];
        _generatorPreview.Tier = 5;
        _generatorPreview.QueueRedraw();
        _generatorInfo.Text = CollectionCatalog.GeneratorNames[unlocked[index]] +
            "  (" + unlocked.Length + "/" + CollectionCatalog.TotalCount + ")" +
            (_game.HasGeneratorOrPart(unlocked[index]) ? " • ON BOARD" : " • PLACE");
    }

    private void BuildBoard(HBoxContainer middle)
    {
        var panel = Panel(new Color("#F9BCCC"), 10,
            new Color("#FFE9B8"), 5, 30);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(780, 640);
        middle.AddChild(panel);

        var inner = new VBoxContainer();
        inner.AddThemeConstantOverride("separation", 4);
        panel.AddChild(inner);
        var caption = new HBoxContainer { CustomMinimumSize = new Vector2(0, 31) };
        inner.AddChild(caption);
        caption.AddChild(LabelText("✦  YOUR MERGE BOARD", 20, new Color("#934E62"), true));
        caption.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        caption.AddChild(LabelText("TAP SPAWNERS  •  DRAG TO MERGE", 13,
            new Color("#9A6174"), true));

        _board = new BoardView(_game)
        {
            CustomMinimumSize = new Vector2(650, 585),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _board.ActionFinished += BoardAction;
        inner.AddChild(_board);
    }

    private void BuildTools(HBoxContainer middle)
    {
        var panel = Panel(new Color("#FFF0CE", 0.98f), 12,
            new Color("#FFF8E8"), 4, 27);
        panel.CustomMinimumSize = new Vector2(300, 0);
        middle.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 9);
        panel.AddChild(column);

        var heading = LabelText("YOUR KITCHEN", 19, new Color("#996447"), true);
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(heading);

        var previewCard = Panel(new Color("#F8D4A4"), 6, new Color("#FFFFFF"), 2, 18);
        previewCard.CustomMinimumSize = new Vector2(0, 204);
        column.AddChild(previewCard);
        var previewColumn = new VBoxContainer();
        previewColumn.AddThemeConstantOverride("separation", 2);
        previewCard.AddChild(previewColumn);
        _generatorPreview = new FoodPreview
        {
            Family = 0, Tier = 5,
            CustomMinimumSize = new Vector2(0, 145),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        previewColumn.AddChild(_generatorPreview);
        _generatorInfo = LabelText("ORCHARD", 15, new Color("#976443"), true, true);
        _generatorInfo.HorizontalAlignment = HorizontalAlignment.Center;
        previewColumn.AddChild(_generatorInfo);

        column.AddChild(LabelText("SELECT GENERATOR", 12, new Color("#93634E"), true));
        _generatorChoice = new OptionButton
        {
            CustomMinimumSize = new Vector2(0, 46),
            FitToLongestItem = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _generatorChoice.AddThemeFontSizeOverride("font_size", 15);
        _generatorChoice.ItemSelected += index =>
        {
            int family = _generatorChoice.GetItemId((int)index);
            if (_game.SelectGenerator(family)) Changed(true);
        };
        column.AddChild(_generatorChoice);

        column.AddChild(Button("SPAWN FREE ITEM", new Color("#6BBB62"),
            () => Spawn(_game.State.SelectedGenerator), 55));
        column.AddChild(LabelText("Or tap any spawner on your board", 12,
            new Color("#A27A5D"), false, true));

        column.AddChild(new HSeparator());
        column.AddChild(LabelText("BOARD TOOLS", 13, new Color("#9F6A51"), true));
        var tools = new HBoxContainer();
        tools.AddThemeConstantOverride("separation", 5);
        column.AddChild(tools);
        tools.AddChild(Button("HINT", new Color("#D8AE64"),
            () => { _game.ShowHint(); Refresh(); }, 47));
        tools.AddChild(Button("SORT", new Color("#77B8C1"),
            () => Changed(_game.Organize()), 47));
        tools.AddChild(Button("TRASH", new Color("#E18B82"),
            () => Changed(_game.Recycle()), 47));

        column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        _served = LabelText("0 ORDERS SERVED", 15, new Color("#926C53"), true);
        column.AddChild(_served);
        column.AddChild(Button("NEW GAME", new Color("#B1A49E"), ResetPressed, 39));
    }

    private static void Tool(VBoxContainer parent, string symbol, string text, Color color, Action action)
    {
        var wrapper = Panel(color, 7, new Color("#FFFFFF", 0.9f), 2, 15);
        wrapper.CustomMinimumSize = new Vector2(0, 88);
        parent.AddChild(wrapper);
        var vertical = new VBoxContainer();
        vertical.AddThemeConstantOverride("separation", 4);
        wrapper.AddChild(vertical);
        var icon = LabelText(symbol, 25, Colors.White, true);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        vertical.AddChild(icon);
        vertical.AddChild(Button(text, color.Darkened(0.16f), action, 31));
    }

    private void BuildFooter(VBoxContainer parent)
    {
        var footer = Panel(new Color("#FFF0D0", 0.97f), 10,
            new Color("#FFF9E3"), 3, 19);
        parent.AddChild(footer);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 3);
        footer.AddChild(column);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        column.AddChild(row);
        row.AddChild(LabelText("TRAVEL LEVEL", 14, new Color("#92614E"), true));
        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(450, 21),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _progress.AddThemeStyleboxOverride("background",
            Style(new Color("#E0C29E"), new Color("#FFFFFF"), 1, 11));
        _progress.AddThemeStyleboxOverride("fill",
            Style(new Color("#61C6AF"), new Color("#9FF6BB"), 2, 11));
        row.AddChild(_progress);
        _albumCount = LabelText("ALBUM 0/54", 15, new Color("#448B84"), true);
        row.AddChild(_albumCount);
        _status = LabelText("", 14, new Color("#875A47"), true, true);
        column.AddChild(_status);
    }

    private void RebuildOrders()
    {
        foreach (Node old in _orders.GetChildren())
        {
            _orders.RemoveChild(old);
            old.QueueFree();
        }

        for (int i = 0; i < _game.State.Orders.Count; i++)
        {
            int orderIndex = i;
            var order = _game.State.Orders[i];
            bool ready = _game.CanServe(i);
            var card = Panel(new Color("#FFF8E7"), 11,
                ready ? new Color("#A2DA83") : new Color("#FFE9BD"), 3, 20);
            card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            card.CustomMinimumSize = new Vector2(0, 118);
            _orders.AddChild(card);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            card.AddChild(row);
            var picture = new FoodPreview
            {
                Family = order.Family,
                Tier = order.Tier,
                CustomMinimumSize = new Vector2(105, 96)
            };
            row.AddChild(picture);

            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 4);
            row.AddChild(text);
            text.AddChild(LabelText("CUSTOMER ORDER " + (i + 1), 12,
                new Color("#B98B68"), true));
            text.AddChild(LabelText(FoodCatalog.Name(order.Family, order.Tier), 20,
                new Color("#875947"), true, true));
            text.AddChild(LabelText("● +" + order.Coins + "       ★ +" + order.Experience,
                13, new Color("#CB9345"), true));
            text.AddChild(Button(ready ? "SERVE ORDER ✓" : "MERGE TO COMPLETE",
                ready ? new Color("#69BC59") : new Color("#D7AE84"),
                () => Changed(_game.Serve(orderIndex)), 35));
        }
    }

    private void ShowAlbum()
    {
        _album.Open();
    }

    private void ShowTown()
    {
        _townView.Open();
    }

    private void ShowCollections()
    {
        _collections.Open();
    }

    private void BoardAction(bool changed)
    {
        _resetPending = false;
        Changed(changed);
    }

    private void Spawn(int family)
    {
        _resetPending = false;
        Changed(_game.Spawn(family));
    }

    private void Changed(bool stateChanged)
    {
        if (stateChanged) Save();
        Refresh();
    }

    private void ResetPressed()
    {
        if (!_resetPending)
        {
            _resetPending = true;
            _status.Text = "Press NEW GAME again to reset all saved progress.";
            return;
        }

        _resetPending = false;
        _game.NewGame();
        Save();
        Refresh();
    }

    private void Refresh()
    {
        _level.Text = _game.State.Level.ToString();
        _energy.Text = "∞ FREE";
        _coins.Text = _game.State.Coins.ToString("N0");
        _served.Text = _game.State.CompletedOrders + " ORDERS";
        _albumCount.Text = "ALBUM " + _game.Cards.Collected + "/" + CardCatalog.TotalCards +
                           "  |  PACKS " + (_game.Cards.State.Packs + _game.Cards.State.PremiumPacks);
        _progress.Value = 100.0 * _game.State.Experience / _game.ExperienceRequired;
        _status.Text = _game.Message;
        RefreshGenerators();
        RebuildOrders();
        _board.QueueRedraw();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (_collections.Visible)
        {
            if (key.Keycode == Key.Escape)
            {
                _collections.Close();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (_townView.Visible)
        {
            if (key.Keycode == Key.Escape)
            {
                _townView.Close();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (_album.Visible)
        {
            if (key.Keycode == Key.Escape)
            {
                _album.Close();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (key.Keycode == Key.Space)
        {
            Spawn(0);
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            _game.ClearSelection();
            Refresh();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Save()
    {
        try
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(_game.State));
        }
        catch (Exception ex)
        {
            GD.PushWarning("Game save failed: " + ex.Message);
        }
    }

    private void Load()
    {
        try
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);
            if (File.Exists(path))
                _game.Restore(JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path)));
        }
        catch (Exception ex)
        {
            GD.PushWarning("Game save loading failed: " + ex.Message);
        }
    }

    private static PanelContainer Panel(Color fill, int padding, Color border, int borderWidth, int radius)
    {
        var panel = new PanelContainer();
        var style = Style(fill, border, borderWidth, radius);
        style.ContentMarginLeft = padding;
        style.ContentMarginTop = padding;
        style.ContentMarginRight = padding;
        style.ContentMarginBottom = padding;
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static StyleBoxFlat Style(Color fill, Color border, int borderWidth, int radius)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(radius);
        return style;
    }

    private static Label LabelText(string text, int size, Color color, bool bold = false, bool wrap = false)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (bold)
            label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.10f));
        return label;
    }

    private static Button Button(string text, Color fill, Action clicked, int height)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, height),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        var normal = Style(fill, new Color("#FFFFFF", 0.42f), 2, 12);
        normal.ContentMarginLeft = 8;
        normal.ContentMarginRight = 8;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", Style(fill.Lightened(0.15f), Colors.White, 2, 12));
        button.AddThemeStyleboxOverride("pressed", Style(fill.Darkened(0.1f), Colors.White, 2, 12));
        button.AddThemeStyleboxOverride("focus", Style(Colors.Transparent, Colors.Transparent, 0, 12));
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += clicked;
        return button;
    }
}

public partial class FoodPreview : Control
{
    private int _family;
    private int _tier;
    public int Family { get => _family; set { _family = value; QueueRedraw(); } }
    public int Tier { get => _tier; set { _tier = value; QueueRedraw(); } }

    public override void _Draw()
    {
        if (Size.X < 1 || Size.Y < 1) return;
        var center = Size / 2f;
        float side = Mathf.Min(Size.X, Size.Y) * 0.88f;
        DrawCircle(center + new Vector2(0, 2), side * 0.48f,
            new Color("#FFFFFF", 0.46f));
        var artwork = GameArtTextures.Item(Family, Tier);
        if (artwork != null)
            DrawTextureRect(artwork,
                new Rect2(center - Vector2.One * side * 0.5f, Vector2.One * side),
                false);
    }
}

public partial class AwningView : Control
{
    public override void _Ready()
    {
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        int segments = 18;
        if (w < 1) return;

        for (int i = 0; i < segments; i++)
        {
            float x = i * w / segments;
            float segmentWidth = w / segments + 1;
            Color color = i % 2 == 0 ? new Color("#FCF9EB") : new Color("#79C8B2");
            DrawRect(new Rect2(x, 0, segmentWidth, h * 0.70f), color);
            DrawCircle(new Vector2(x + segmentWidth / 2f, h * 0.70f),
                       segmentWidth / 2f, color);
        }
        DrawRect(new Rect2(0, 0, w, 3), new Color("#B6E1C6"));
    }
}
