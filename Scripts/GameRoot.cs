using Godot;
using MergeHarvest;
using System;
using System.IO;
using System.Text.Json;

public partial class GameRoot : Control
{
    private const string SaveFile = "user://merge_harvest_save.json";
    private readonly GameData _game = new();
    private BoardView _board = null!;
    private Label _levelText = null!;
    private Label _coinsText = null!;
    private Label _ordersText = null!;
    private Label _mergesText = null!;
    private Label _statusText = null!;
    private ProgressBar _experienceBar = null!;
    private VBoxContainer _ordersContainer = null!;
    private bool _resetPending;

    internal static readonly Color Ink = new("#33424A");
    internal static readonly Color Muted = new("#73858C");
    internal static readonly Color Cream = new("#FFF9ED");
    internal static readonly Color Plum = new("#695F9B");
    internal static readonly Color[] FamilyColors =
    {
        new("#EF7772"), new("#D99452"), new("#61B7BB")
    };

    public override void _Ready()
    {
        Load();
        BuildInterface();
        Refresh();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) Save();
    }

    private void BuildInterface()
    {
        var background = new ColorRect
        {
            Color = new Color("#F4F5ED"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var page = new MarginContainer();
        page.SetAnchorsPreset(LayoutPreset.FullRect);
        page.AddThemeConstantOverride("margin_left", 28);
        page.AddThemeConstantOverride("margin_right", 28);
        page.AddThemeConstantOverride("margin_top", 18);
        page.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(page);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 13);
        page.AddChild(column);

        var header = new HBoxContainer { CustomMinimumSize = new Vector2(0, 74) };
        header.AddThemeConstantOverride("separation", 14);
        column.AddChild(header);

        var brand = new VBoxContainer();
        brand.AddThemeConstantOverride("separation", 0);
        brand.CustomMinimumSize = new Vector2(265, 0);
        header.AddChild(brand);
        brand.AddChild(MakeLabel("MERGE HARVEST", 28, Ink, true));
        brand.AddChild(MakeLabel("A cozy, endless food-merging game", 13, Muted));

        var expanding = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(expanding);

        header.AddChild(MakePill("NO ADS  •  NO PAYMENTS", new Color("#E9F5E9"), new Color("#477653"), 14));
        header.AddChild(MakePill("UNLIMITED ENERGY", new Color("#E7F6F5"), new Color("#398B8D"), 14));

        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 14);
        column.AddChild(statsRow);

        var levelPanel = MakePanel(new Color("#FFFDF7"), 18, new Color("#E8E6DA"));
        levelPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        statsRow.AddChild(levelPanel);
        var levelLayout = new VBoxContainer();
        levelLayout.AddThemeConstantOverride("separation", 6);
        levelPanel.AddChild(levelLayout);
        _levelText = MakeLabel("LEVEL 1", 16, Ink, true);
        levelLayout.AddChild(_levelText);
        _experienceBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 12),
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = 100,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _experienceBar.AddThemeStyleboxOverride("background", BoxStyle(new Color("#EEEDE4"), 8));
        _experienceBar.AddThemeStyleboxOverride("fill", BoxStyle(new Color("#8ACDA0"), 8));
        levelLayout.AddChild(_experienceBar);

        var coinPanel = MakePanel(new Color("#FFF2D7"), 18, new Color("#F2E0B6"));
        coinPanel.CustomMinimumSize = new Vector2(184, 0);
        statsRow.AddChild(coinPanel);
        var coinLayout = new VBoxContainer();
        coinPanel.AddChild(coinLayout);
        coinLayout.AddChild(MakeLabel("COINS EARNED", 12, new Color("#917A4D"), true));
        _coinsText = MakeLabel("100", 24, new Color("#B27829"), true);
        coinLayout.AddChild(_coinsText);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        column.AddChild(body);

        var leftPanel = MakePanel(new Color("#FFFDF8"), 20, new Color("#E5E4D9"));
        leftPanel.CustomMinimumSize = new Vector2(248, 550);
        body.AddChild(leftPanel);

        var leftColumn = new VBoxContainer();
        leftColumn.AddThemeConstantOverride("separation", 11);
        leftPanel.AddChild(leftColumn);
        leftColumn.AddChild(MakeLabel("TODAY'S ORDERS", 20, Ink, true));
        leftColumn.AddChild(MakeLabel("Find each dish on the board, then serve it.", 13, Muted, false, true));
        _ordersContainer = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _ordersContainer.AddThemeConstantOverride("separation", 13);
        leftColumn.AddChild(_ordersContainer);
        _ordersText = MakeLabel("", 13, Muted);
        leftColumn.AddChild(_ordersText);

        var boardPanel = MakePanel(new Color("#FFFDF7"), 18, new Color("#E4E5DC"));
        boardPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        boardPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        boardPanel.CustomMinimumSize = new Vector2(576, 550);
        body.AddChild(boardPanel);

        var boardColumn = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        boardColumn.AddThemeConstantOverride("separation", 7);
        boardPanel.AddChild(boardColumn);
        var boardHeader = new HBoxContainer();
        boardColumn.AddChild(boardHeader);
        boardHeader.AddChild(MakeLabel("YOUR MERGE BOARD", 18, Ink, true));
        boardHeader.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        boardHeader.AddChild(MakeLabel("7 × 7", 14, Muted, true));
        _board = new BoardView(_game)
        {
            CustomMinimumSize = new Vector2(535, 505),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop
        };
        _board.ActionFinished += BoardAction;
        boardColumn.AddChild(_board);

        var rightPanel = MakePanel(new Color("#FFFDF8"), 18, new Color("#E5E4D9"));
        rightPanel.CustomMinimumSize = new Vector2(218, 550);
        body.AddChild(rightPanel);

        var rightColumn = new VBoxContainer();
        rightColumn.AddThemeConstantOverride("separation", 14);
        rightPanel.AddChild(rightColumn);
        rightColumn.AddChild(MakeLabel("HOW TO PLAY", 20, Ink, true));
        AddHelp(rightColumn, "01", "GENERATE", "Use the three buttons below. Every item is free.");
        AddHelp(rightColumn, "02", "MERGE", "Drag matching items together, or click both.");
        AddHelp(rightColumn, "03", "SERVE", "Complete food orders to earn coins and XP.");
        AddHelp(rightColumn, "04", "DISCOVER", "Unlock higher tiers and level up forever.");
        var rule = new HSeparator();
        rightColumn.AddChild(rule);
        rightColumn.AddChild(MakeLabel("YOUR KITCHEN", 13, Muted, true));
        _mergesText = MakeLabel("", 15, Ink, true);
        rightColumn.AddChild(_mergesText);
        rightColumn.AddChild(MakeLabel("No timers. No energy limit. No purchases. No story dialogue.", 13, Muted, false, true));

        var actions = new HBoxContainer { CustomMinimumSize = new Vector2(0, 54) };
        actions.AddThemeConstantOverride("separation", 10);
        column.AddChild(actions);
        actions.AddChild(MakeButton("ORCHARD +", new Color("#E77E78"), () => Spawn(0), 145));
        actions.AddChild(MakeButton("BAKERY +", new Color("#D99D62"), () => Spawn(1), 136));
        actions.AddChild(MakeButton("DRINKS +", new Color("#6AB3BB"), () => Spawn(2), 138));
        actions.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        actions.AddChild(MakeButton("HINT", new Color("#8687B7"), () => { _game.ShowHint(); Refresh(); }, 82));
        actions.AddChild(MakeButton("ORGANIZE", new Color("#738A90"), () => Changed(_game.Organize()), 115));
        actions.AddChild(MakeButton("RECYCLE SELECTED", new Color("#B87987"), () => Changed(_game.Recycle()), 170));
        actions.AddChild(MakeButton("RESET", new Color("#A4A29E"), ResetPressed, 80));

        var messagePanel = MakePanel(new Color("#E7EFE8"), 12, new Color("#D4E5DA"));
        column.AddChild(messagePanel);
        _statusText = MakeLabel("", 14, new Color("#49665B"), true, true);
        messagePanel.AddChild(_statusText);
    }

    private void AddHelp(VBoxContainer parent, string number, string heading, string description)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);
        row.AddChild(MakePill(number, new Color("#EDECF6"), Plum, 12));
        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 2);
        row.AddChild(content);
        content.AddChild(MakeLabel(heading, 13, Ink, true));
        content.AddChild(MakeLabel(description, 12, Muted, false, true));
    }

    private void Spawn(int family)
    {
        _resetPending = false;
        Changed(_game.Spawn(family));
    }

    private void BoardAction(bool modified)
    {
        _resetPending = false;
        Changed(modified);
    }

    private void Changed(bool modified)
    {
        if (modified) Save();
        Refresh();
    }

    private void ResetPressed()
    {
        if (!_resetPending)
        {
            _resetPending = true;
            _statusText.Text = "Press RESET once more to erase your local progress.";
            return;
        }
        _resetPending = false;
        _game.NewGame();
        Save();
        Refresh();
    }

    private void Refresh()
    {
        _levelText.Text = "LEVEL " + _game.State.Level + "   •   " +
                          _game.State.Experience + " / " + _game.ExperienceRequired + " XP";
        _experienceBar.Value = 100.0 * _game.State.Experience / _game.ExperienceRequired;
        _coinsText.Text = _game.State.Coins.ToString("N0");
        _ordersText.Text = "Orders served: " + _game.State.CompletedOrders;
        _mergesText.Text = _game.State.TotalMerges + " successful merges";
        _statusText.Text = _game.Message;
        RebuildOrders();
        _board.QueueRedraw();
    }

    private void RebuildOrders()
    {
        foreach (Node child in _ordersContainer.GetChildren())
        {
            _ordersContainer.RemoveChild(child);
            child.QueueFree();
        }
        for (int index = 0; index < _game.State.Orders.Count; index++)
        {
            int orderIndex = index;
            var order = _game.State.Orders[index];
            bool ready = _game.CanServe(index);
            var card = MakePanel(ready ? new Color("#F0F9EF") : new Color("#F5F3F0"),
                                 12, ready ? new Color("#B9DCC2") : new Color("#E5E5E0"));
            _ordersContainer.AddChild(card);
            var content = new VBoxContainer();
            content.AddThemeConstantOverride("separation", 7);
            card.AddChild(content);
            content.AddChild(MakeLabel(FoodCatalog.FamilyNames[order.Family].ToUpperInvariant(),
                                       11, FamilyColors[order.Family], true));
            content.AddChild(MakeLabel(FoodCatalog.Name(order.Family, order.Tier),
                                       16, Ink, true, true));
            content.AddChild(MakeLabel("Tier " + order.Tier + "  •  +" + order.Coins + " coins  •  +" +
                                      order.Experience + " XP", 11, Muted));
            var serveButton = MakeButton(ready ? "SERVE ORDER ✓" : "FIND & MERGE",
                ready ? new Color("#6BAA7D") : new Color("#B0ADB6"),
                () => Changed(_game.Serve(orderIndex)), 0);
            serveButton.CustomMinimumSize = new Vector2(0, 35);
            content.AddChild(serveButton);
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
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
            string filepath = ProjectSettings.GlobalizePath(SaveFile);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);
            File.WriteAllText(filepath, JsonSerializer.Serialize(_game.State));
        }
        catch (Exception error)
        {
            GD.PushWarning("Could not save the game: " + error.Message);
        }
    }

    private void Load()
    {
        try
        {
            string filepath = ProjectSettings.GlobalizePath(SaveFile);
            if (File.Exists(filepath))
            {
                SaveData? saved = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(filepath));
                _game.Restore(saved);
            }
        }
        catch (Exception error)
        {
            GD.PushWarning("Could not load the saved game: " + error.Message);
        }
    }

    private static StyleBoxFlat BoxStyle(Color color, int rounding, Color? border = null)
    {
        var style = new StyleBoxFlat { BgColor = color, BorderColor = border ?? Colors.Transparent };
        style.SetCornerRadiusAll(rounding);
        if (border.HasValue) style.SetBorderWidthAll(1);
        return style;
    }

    private static PanelContainer MakePanel(Color fill, int pad, Color border)
    {
        var panel = new PanelContainer();
        var style = BoxStyle(fill, 17, border);
        style.ContentMarginLeft = pad;
        style.ContentMarginRight = pad;
        style.ContentMarginTop = pad;
        style.ContentMarginBottom = pad;
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Label MakeLabel(string text, int size, Color color, bool bold = false, bool wrap = false)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        if (bold) label.AddThemeColorOverride("font_shadow_color", new Color(color, 0.08f));
        return label;
    }

    private static PanelContainer MakePill(string text, Color fill, Color ink, int size)
    {
        var panel = new PanelContainer();
        var box = BoxStyle(fill, 13);
        box.ContentMarginLeft = 13;
        box.ContentMarginRight = 13;
        box.ContentMarginTop = 10;
        box.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", box);
        panel.AddChild(MakeLabel(text, size, ink, true));
        return panel;
    }

    private static Button MakeButton(string text, Color baseColor, Action pressed, float minWidth)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(minWidth, 44),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        button.AddThemeStyleboxOverride("normal", ButtonBox(baseColor));
        button.AddThemeStyleboxOverride("hover", ButtonBox(baseColor.Lightened(0.12f)));
        button.AddThemeStyleboxOverride("pressed", ButtonBox(baseColor.Darkened(0.14f)));
        button.AddThemeStyleboxOverride("focus", BoxStyle(Colors.Transparent, 12));
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 13);
        button.Pressed += pressed;
        return button;
    }

    private static StyleBoxFlat ButtonBox(Color color)
    {
        var style = BoxStyle(color, 12);
        style.ContentMarginLeft = 9;
        style.ContentMarginRight = 9;
        style.ContentMarginTop = 7;
        style.ContentMarginBottom = 7;
        return style;
    }
}

public partial class BoardView : Control
{
    private readonly GameData _game;
    private int _held = -1;
    private Vector2 _mouseStart;
    private Vector2 _pointer;
    private const float Gap = 7f;
    public event Action<bool>? ActionFinished;

    public BoardView(GameData game) => _game = game;

    private (float cell, float x, float y) Geometry()
    {
        float cell = Mathf.Min(
            (Size.X - 12 - (FoodCatalog.Columns - 1) * Gap) / FoodCatalog.Columns,
            (Size.Y - 12 - (FoodCatalog.Rows - 1) * Gap) / FoodCatalog.Rows);
        cell = Mathf.Max(1, cell);
        float width = cell * FoodCatalog.Columns + (FoodCatalog.Columns - 1) * Gap;
        float height = cell * FoodCatalog.Rows + (FoodCatalog.Rows - 1) * Gap;
        return (cell, (Size.X - width) / 2f, (Size.Y - height) / 2f);
    }

    private int HitTest(Vector2 at)
    {
        var (size, x, y) = Geometry();
        float localX = at.X - x;
        float localY = at.Y - y;
        if (localX < 0 || localY < 0) return -1;
        int col = Mathf.FloorToInt(localX / (size + Gap));
        int row = Mathf.FloorToInt(localY / (size + Gap));
        if (col < 0 || col >= FoodCatalog.Columns || row < 0 || row >= FoodCatalog.Rows) return -1;
        if (localX - col * (size + Gap) > size || localY - row * (size + Gap) > size) return -1;
        return row * FoodCatalog.Columns + col;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left &&
            button.Pressed)
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
        else if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left &&
                 !button.Pressed)
        {
            int source = _held;
            int destination = HitTest(GetLocalMousePosition());
            _held = -1;
            _pointer = _mouseStart;
            if (destination >= 0)
            {
                ActionFinished?.Invoke(_game.Interact(source, destination));
                GetViewport().SetInputAsHandled();
            }
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var (size, offsetX, offsetY) = Geometry();
        for (int i = 0; i < FoodCatalog.CellCount; i++)
        {
            int row = i / FoodCatalog.Columns;
            int col = i % FoodCatalog.Columns;
            var rect = new Rect2(offsetX + col * (size + Gap),
                                 offsetY + row * (size + Gap), size, size);
            var tile = _game.State.Tiles[i];
            bool selected = _game.Selected == i;
            bool hinted = _game.HintOne == i || _game.HintTwo == i;
            var color = (row + col) % 2 == 0 ? new Color("#F3F3E9") : new Color("#EBEEE7");
            if (tile == null)
            {
                DrawRounded(rect, color, new Color("#E0E5DC"), 1);
                DrawCircle(rect.GetCenter(), 4f, new Color("#D9DDD3"));
                continue;
            }
            Color hue = GameRoot.FamilyColors[tile.Family];
            var fill = new Color("#FFFFFF").Lerp(hue, 0.08f + tile.Tier * 0.017f);
            DrawRounded(rect, fill,
                selected ? new Color("#6275B5") : hinted ? new Color("#E7B951") : hue.Lightened(0.4f),
                selected || hinted ? 3 : 1);
            DrawIcon(rect.GetCenter() - new Vector2(0, size * 0.07f),
                size * 0.23f, tile.Family, tile.Tier, hue);
            DrawRounded(new Rect2(rect.Position + new Vector2(6, 6), new Vector2(31, 17)),
                        hue, hue, 0, 7);
            DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(6, 19),
                       "T" + tile.Tier, HorizontalAlignment.Center, 31, 11, Colors.White);
            string name = FoodCatalog.Name(tile.Family, tile.Tier);
            string[] parts = name.Split(' ', 2);
            if (parts.Length == 2)
            {
                DrawString(GetThemeDefaultFont(),
                    rect.Position + new Vector2(3, size - 22),
                    parts[0], HorizontalAlignment.Center, size - 6, 11, GameRoot.Ink);
                DrawString(GetThemeDefaultFont(),
                    rect.Position + new Vector2(3, size - 9),
                    parts[1], HorizontalAlignment.Center, size - 6, 11, GameRoot.Ink);
            }
            else
            {
                DrawString(GetThemeDefaultFont(),
                    rect.Position + new Vector2(3, size - 11),
                    name, HorizontalAlignment.Center, size - 6, 11, GameRoot.Ink);
            }
        }

        if (_held >= 0 && _game.State.Tiles[_held] is { } dragged &&
            _mouseStart.DistanceTo(_pointer) > 12f)
        {
            DrawCircle(_pointer, size * 0.35f, new Color(1f, 1f, 1f, 0.82f));
            DrawIcon(_pointer, size * 0.23f, dragged.Family, dragged.Tier,
                     GameRoot.FamilyColors[dragged.Family]);
        }
    }

    private void DrawRounded(Rect2 rect, Color fill, Color outline, int border, int radius = 12)
    {
        var shape = new StyleBoxFlat { BgColor = fill, BorderColor = outline };
        shape.SetCornerRadiusAll(radius);
        shape.SetBorderWidthAll(border);
        DrawStyleBox(shape, rect);
    }

    // All icons are drawn in code. No copied assets or generated image files.
    private void DrawIcon(Vector2 pos, float radius, int family, int tier, Color hue)
    {
        Color accent = hue.Lightened(0.22f);
        if (family == 0)
        {
            // Orchard: fruit with leaves, seeds and a stem.
            DrawCircle(pos + new Vector2(-radius * 0.32f, radius * 0.09f),
                       radius * 0.68f, hue);
            DrawCircle(pos + new Vector2(radius * 0.31f, radius * 0.09f),
                       radius * 0.68f, accent);
            DrawLine(pos - new Vector2(0, radius * 0.64f),
                     pos - new Vector2(radius * 0.09f, radius * 1.08f),
                     new Color("#668A5C"), 3);
            DrawColoredPolygon(new[]
            {
                pos + new Vector2(0, -radius * 0.8f),
                pos + new Vector2(radius * 0.62f, -radius * 1.2f),
                pos + new Vector2(radius * 0.42f, -radius * 0.68f)
            }, new Color("#7CAF7A"));
            if (tier >= 3)
                for (int i = 0; i < 3; i++)
                    DrawCircle(pos + new Vector2((i - 1) * radius * 0.36f, radius * 0.25f),
                               1.65f, new Color("#FFE5B1"));
        }
        else if (family == 1)
        {
            // Bakery: a pastry with icing and a wrapper.
            var baseRect = new Rect2(pos - new Vector2(radius * 0.79f, radius * 0.05f),
                                     new Vector2(radius * 1.58f, radius * 0.95f));
            DrawRounded(baseRect, hue, hue, 0, 4);
            DrawCircle(pos - new Vector2(radius * 0.45f, radius * 0.24f),
                       radius * 0.44f, new Color("#F5C6B4"));
            DrawCircle(pos - new Vector2(0, radius * 0.46f),
                       radius * 0.53f, new Color("#F5B5B6"));
            DrawCircle(pos + new Vector2(radius * 0.42f, -radius * 0.21f),
                       radius * 0.42f, new Color("#F5C6B4"));
            DrawCircle(pos - new Vector2(0, radius * 0.83f), radius * 0.12f,
                       new Color("#C9586E"));
            for (int i = -1; i <= 1; i++)
                DrawLine(pos + new Vector2(i * radius * 0.33f, radius * 0.24f),
                         pos + new Vector2(i * radius * 0.25f, radius * 0.7f),
                         accent, 1.6f);
        }
        else
        {
            // Drinks: tapered glass, straw, garnish.
            DrawColoredPolygon(new[]
            {
                pos + new Vector2(-radius * 0.74f, -radius * 0.52f),
                pos + new Vector2(radius * 0.74f, -radius * 0.52f),
                pos + new Vector2(radius * 0.49f, radius * 0.86f),
                pos + new Vector2(-radius * 0.49f, radius * 0.86f)
            }, hue);
            DrawLine(pos + new Vector2(radius * 0.20f, -radius * 0.35f),
                     pos + new Vector2(radius * 0.58f, -radius * 1.10f),
                     new Color("#E889A7"), 3f);
            DrawLine(pos + new Vector2(-radius * 0.55f, -radius * 0.29f),
                     pos + new Vector2(radius * 0.55f, -radius * 0.29f),
                     Colors.White, 2f);
            DrawCircle(pos + new Vector2(-radius * 0.44f, -radius * 0.64f),
                       radius * 0.22f, new Color("#F6D88C"));
            DrawCircle(pos + new Vector2(radius * 0.14f, radius * 0.26f),
                       radius * 0.10f, new Color(1f, 1f, 1f, 0.7f));
        }
    }
}
