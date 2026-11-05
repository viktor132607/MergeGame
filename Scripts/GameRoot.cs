using Godot;
using MergeHarvest;
using System;
using System.IO;
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
        var backdrop = new BeachBackdrop { MouseFilter = MouseFilterEnum.Ignore };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var main = Panel(new Color("#167F78"), 17, new Color("#FFF1B9"), 4, 26);
        main.CustomMinimumSize = new Vector2(1760, 1020);
        center.AddChild(main);

        var vertical = new VBoxContainer();
        vertical.AddThemeConstantOverride("separation", 8);
        main.AddChild(vertical);

        BuildHeader(vertical);

        var awning = new AwningView
        {
            CustomMinimumSize = new Vector2(0, 28),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        vertical.AddChild(awning);

        var orderShelf = Panel(new Color("#C78D62"), 10, new Color("#966548"), 2, 17);
        vertical.AddChild(orderShelf);
        _orders = new HBoxContainer();
        _orders.AddThemeConstantOverride("separation", 10);
        orderShelf.AddChild(_orders);

        var middle = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        middle.AddThemeConstantOverride("separation", 11);
        vertical.AddChild(middle);

        BuildGenerators(middle);
        BuildBoard(middle);
        BuildTools(middle);

        BuildFooter(vertical);

        // Modal album overlays the full board; gameplay stays untouched underneath.
        _album = new CardAlbumView(_game);
        _album.StateChanged += () => { Save(); Refresh(); };
        AddChild(_album);

        _townView = new TownView(_game);
        _townView.Changed += () => { Save(); Refresh(); };
        AddChild(_townView);
    }

    private void BuildHeader(VBoxContainer parent)
    {
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 66) };
        row.AddThemeConstantOverride("separation", 11);
        parent.AddChild(row);

        var brand = new VBoxContainer { CustomMinimumSize = new Vector2(245, 0) };
        brand.AddThemeConstantOverride("separation", 0);
        row.AddChild(brand);
        brand.AddChild(LabelText("MERGE HARVEST", 28, new Color("#FFF1AC"), true));
        brand.AddChild(LabelText("SUMMER KITCHEN  •  FREE PLAY", 12,
            new Color("#D1F0D6"), true));

        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var badge = Panel(new Color("#FFF0BD"), 11, new Color("#E6AC62"), 3, 30);
        badge.CustomMinimumSize = new Vector2(100, 55);
        var badgeContent = new VBoxContainer();
        badgeContent.AddThemeConstantOverride("separation", 0);
        badge.AddChild(badgeContent);
        badgeContent.AddChild(LabelText("★  LEVEL", 11, new Color("#B7833C"), true));
        _level = LabelText("1", 22, new Color("#9E5D3C"), true);
        badgeContent.AddChild(_level);
        row.AddChild(badge);

        _energy = LabelText("∞", 20, new Color("#FFFFFF"), true);
        row.AddChild(Stat("⚡ ENERGY", _energy, new Color("#289BC8"), 128));

        _coins = LabelText("100", 20, new Color("#FFF2A5"), true);
        row.AddChild(Stat("● COINS", _coins, new Color("#416E8E"), 133));

        var albumButton = Button("CARD ALBUM", new Color("#D89F4A"), ShowAlbum, 56);
        albumButton.CustomMinimumSize = new Vector2(120, 56);
        row.AddChild(albumButton);

        var townButton = Button("BUILD TOWN", new Color("#C27C51"), ShowTown, 56);
        townButton.CustomMinimumSize = new Vector2(120, 56);
        row.AddChild(townButton);
    }

    private static PanelContainer Stat(string name, Label value, Color fill, float width)
    {
        var panel = Panel(fill, 8, new Color("#D3EDED", 0.75f), 2, 27);
        panel.CustomMinimumSize = new Vector2(width, 53);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 0);
        panel.AddChild(stack);
        stack.AddChild(LabelText(name, 11, new Color("#EAF8FF"), true));
        stack.AddChild(value);
        return panel;
    }

    private void BuildGenerators(HBoxContainer middle)
    {
        var panel = Panel(new Color("#E9F6D9"), 10, new Color("#C5DFB5"), 2, 19);
        panel.CustomMinimumSize = new Vector2(133, 0);
        middle.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        panel.AddChild(column);
        var heading = LabelText("GENERATORS", 13, DeepTeal, true);
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(heading);

        string[] names = { "FRUITS", "BAKERY", "DRINKS" };
        Color[] fills = { new("#F7AA75"), new("#EFC26F"), new("#89D4D8") };
        for (int family = 0; family < 3; family++)
        {
            int index = family;
            var card = Panel(fills[family], 7, new Color("#FFFFFF", 0.8f), 2, 15);
            card.SizeFlagsVertical = SizeFlags.ExpandFill;
            column.AddChild(card);

            var content = new VBoxContainer();
            content.AddThemeConstantOverride("separation", 2);
            card.AddChild(content);
            var preview = new FoodPreview
            {
                Family = family,
                Tier = family == 0 ? 7 : family == 1 ? 5 : 4,
                CustomMinimumSize = new Vector2(0, 60),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            content.AddChild(preview);
            var label = LabelText(names[family], 12, Brown, true);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            content.AddChild(label);
            var button = Button("FREE +", new Color("#59B866"),
                () => Spawn(index), 33);
            content.AddChild(button);
        }

        var energy = LabelText("⚡  UNLIMITED", 12, new Color("#258A87"), true);
        energy.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(energy);
    }

    private void BuildBoard(HBoxContainer middle)
    {
        var panel = Panel(new Color("#F9D5B5"), 7, new Color("#FCEB9B"), 3, 20);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(525, 505);
        middle.AddChild(panel);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 4);
        panel.AddChild(layout);

        var caption = new HBoxContainer { CustomMinimumSize = new Vector2(0, 26) };
        caption.AddThemeConstantOverride("separation", 8);
        layout.AddChild(caption);
        caption.AddChild(LabelText("✦  MERGE BOARD", 15, Brown, true));
        caption.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        caption.AddChild(LabelText("DRAG TO MERGE", 11, new Color("#B47C68"), true));

        _board = new BoardView(_game)
        {
            CustomMinimumSize = new Vector2(505, 505),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _board.ActionFinished += BoardAction;
        layout.AddChild(_board);
    }

    private void BuildTools(HBoxContainer middle)
    {
        var panel = Panel(new Color("#E4F7F1"), 9, new Color("#B5D9CE"), 2, 20);
        panel.CustomMinimumSize = new Vector2(139, 0);
        middle.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        panel.AddChild(column);

        var title = LabelText("BOOSTERS", 13, DeepTeal, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(title);

        Tool(column, "✦", "HINT", new Color("#D9A64D"),
            () => { _game.ShowHint(); Refresh(); });
        Tool(column, "↔", "ORGANIZE", new Color("#75B7C9"), () => Changed(_game.Organize()));
        Tool(column, "✕", "RECYCLE", new Color("#E78E83"), () => Changed(_game.Recycle()));

        column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        var stats = Panel(new Color("#FFF8DE"), 9, new Color("#E0D5AA"), 1, 15);
        column.AddChild(stats);
        var statsContent = new VBoxContainer();
        statsContent.AddThemeConstantOverride("separation", 3);
        stats.AddChild(statsContent);
        statsContent.AddChild(LabelText("SERVED", 11, new Color("#8B886F"), true));
        _served = LabelText("0 ORDERS", 15, Brown, true);
        statsContent.AddChild(_served);

        var reset = Button("NEW GAME", new Color("#9AA7A0"), ResetPressed, 32);
        column.AddChild(reset);
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
        var footer = Panel(new Color("#FFF5CF"), 9, new Color("#F1D991"), 2, 16);
        parent.AddChild(footer);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        footer.AddChild(column);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        column.AddChild(row);
        row.AddChild(LabelText("TRAVEL PROGRESS", 12, Brown, true));

        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(390, 14),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _progress.AddThemeStyleboxOverride("background",
            Style(new Color("#E5CCB0"), new Color("#FFFFFF"), 1, 9));
        _progress.AddThemeStyleboxOverride("fill",
            Style(new Color("#73C57E"), new Color("#C7F7A9"), 1, 9));
        row.AddChild(_progress);

        _albumCount = LabelText("CARDS 0/54", 12, Teal, true);
        row.AddChild(_albumCount);
        _status = LabelText("", 14, Brown, true, true);
        column.AddChild(_status);
    }

    private void RebuildOrders()
    {
        foreach (Node child in _orders.GetChildren())
        {
            _orders.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < _game.State.Orders.Count; i++)
        {
            int orderIndex = i;
            OrderData item = _game.State.Orders[i];
            bool ready = _game.CanServe(i);

            var panel = Panel(new Color("#FFF9EF"), 8,
                ready ? new Color("#9DD882") : new Color("#F3D8BC"), 3, 16);
            panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            panel.CustomMinimumSize = new Vector2(0, 104);
            _orders.AddChild(panel);

            var content = new HBoxContainer();
            content.AddThemeConstantOverride("separation", 7);
            panel.AddChild(content);
            var preview = new FoodPreview
            {
                Family = item.Family,
                Tier = item.Tier,
                CustomMinimumSize = new Vector2(71, 76)
            };
            content.AddChild(preview);

            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 2);
            content.AddChild(text);
            text.AddChild(LabelText("ORDER " + (i + 1), 11, new Color("#BFA087"), true));
            text.AddChild(LabelText(FoodCatalog.Name(item.Family, item.Tier), 15,
                Brown, true, true));
            text.AddChild(LabelText("●  +" + item.Coins + "   ★ +" + item.Experience,
                11, new Color("#D29B4B"), true));

            var serve = Button(ready ? "SERVE ✓" : "MAKE ITEM",
                ready ? new Color("#74C655") : new Color("#D6AF8D"),
                () => Changed(_game.Serve(orderIndex)), 29);
            serve.CustomMinimumSize = new Vector2(0, 28);
            text.AddChild(serve);
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
        RebuildOrders();
        _board.QueueRedraw();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
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
    public int Family { get; set; }
    public int Tier { get; set; }

    public override void _Draw()
    {
        if (Size.X < 1 || Size.Y < 1) return;
        var center = Size / 2f;
        float radius = Mathf.Min(Size.X, Size.Y) * 0.41f;
        DrawCircle(center + new Vector2(0, 1), radius + 2f,
                   new Color("#FFFFFF", 0.63f));
        DrawCircle(center, radius, new Color("#FFF2D6", 0.86f));
        FoodArt.Draw(this, center, radius * 0.80f, Family, Tier);
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
