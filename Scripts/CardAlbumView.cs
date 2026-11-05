using Godot;
using MergeHarvest;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CardAlbumView : Control
{
    private readonly GameData _game;
    private int _selectedSet;
    private PanelContainer _book = null!;
    private VBoxContainer _page = null!;
    private Label _pageHeader = null!;
    private Label _pageProgress = null!;
    private Label _topStats = null!;
    private GridContainer _cards = null!;
    private VBoxContainer _rewardArea = null!;
    private VBoxContainer _revealArea = null!;
    private HBoxContainer _setTabs = null!;
    private readonly List<CardReveal> _lastReveal = new();
    private string _notice = "Open packs to discover new cards.";

    public event Action? StateChanged;
    public event Action? CloseRequested;

    private static readonly Color Ink = new("#85534E");
    private static readonly Color Cream = new("#FFF8E9");
    private static readonly Color Coral = new("#EB866F");
    private static readonly Color Teal = new("#258F89");
    private static readonly Color Gold = new("#D7A344");

    public CardAlbumView(GameData game)
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
        Visible = true;
        _lastReveal.Clear();
        _notice = "Open packs to collect all 54 cards. Each completed set gives a reward.";
        RedrawContents();
    }

    public void Close()
    {
        Visible = false;
        CloseRequested?.Invoke();
    }

    private void Build()
    {
        var dimmer = new ColorRect { Color = new Color("#162B33", 0.83f) };
        dimmer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(dimmer);

        var centering = new CenterContainer();
        centering.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centering);

        _book = Panel(new Color("#F6D4A6"), 17, new Color("#FFF6D8"), 5, 25);
        _book.CustomMinimumSize = new Vector2(1020, 860);
        centering.AddChild(_book);
        _page = new VBoxContainer();
        _page.AddThemeConstantOverride("separation", 9);
        _book.AddChild(_page);

        var header = new HBoxContainer { CustomMinimumSize = new Vector2(0, 64) };
        header.AddThemeConstantOverride("separation", 10);
        _page.AddChild(header);
        var title = new VBoxContainer();
        header.AddChild(title);
        title.AddChild(L("THE TRAVEL CARD ALBUM", 27, new Color("#8C5541"), true));
        title.AddChild(L("COLLECT SETS • OPEN FREE PACKS • EARN PRIZES", 12, Teal, true));
        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _topStats = L("", 15, Ink, true);
        header.AddChild(_topStats);
        header.AddChild(B("X", new Color("#B66C70"), Close, 49));

        var setTitle = L("CHOOSE YOUR COLLECTION", 12, Ink, true);
        _page.AddChild(setTitle);

        _setTabs = new HBoxContainer();
        _setTabs.AddThemeConstantOverride("separation", 6);
        _page.AddChild(_setTabs);

        var contents = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        contents.AddThemeConstantOverride("separation", 12);
        _page.AddChild(contents);

        var gridPanel = Panel(new Color("#FFF9E8"), 14, new Color("#E9BD92"), 3, 20);
        gridPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contents.AddChild(gridPanel);
        var gridColumn = new VBoxContainer();
        gridColumn.AddThemeConstantOverride("separation", 9);
        gridPanel.AddChild(gridColumn);

        _pageHeader = L("", 21, Ink, true);
        gridColumn.AddChild(_pageHeader);
        _pageProgress = L("", 13, new Color("#B08462"), true);
        gridColumn.AddChild(_pageProgress);
        _cards = new GridContainer { Columns = 3, SizeFlagsVertical = SizeFlags.ExpandFill };
        _cards.AddThemeConstantOverride("h_separation", 7);
        _cards.AddThemeConstantOverride("v_separation", 7);
        gridColumn.AddChild(_cards);

        var right = Panel(new Color("#E0F1DF"), 12, new Color("#B8D5B6"), 2, 18);
        right.CustomMinimumSize = new Vector2(232, 0);
        contents.AddChild(right);
        var side = new VBoxContainer();
        side.AddThemeConstantOverride("separation", 11);
        right.AddChild(side);
        side.AddChild(L("CARD PACKS", 20, Teal, true));
        side.AddChild(L("Win packs by completing 2 orders, 8 merges or 10 orders for a golden pack.",
            12, new Color("#65867A"), false, true));

        _rewardArea = new VBoxContainer();
        _rewardArea.AddThemeConstantOverride("separation", 9);
        side.AddChild(_rewardArea);

        side.AddChild(new HSeparator());
        side.AddChild(L("LATEST PACK", 17, Teal, true));
        _revealArea = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _revealArea.AddThemeConstantOverride("separation", 3);
        side.AddChild(_revealArea);

        var foot = Panel(new Color("#FFF7DC"), 9, new Color("#EAB992"), 2, 15);
        _page.AddChild(foot);
        var footContent = new HBoxContainer();
        footContent.AddThemeConstantOverride("separation", 9);
        foot.AddChild(footContent);
        footContent.AddChild(L("STAR SHOP", 17, Ink, true));
        footContent.AddChild(B("30 ★ = PACK", new Color("#78B39B"),
            () => SpendStars(false), 38));
        footContent.AddChild(B("90 ★ = GOLDEN PACK", new Color("#D8A353"),
            () => SpendStars(true), 38));
        footContent.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        footContent.AddChild(L("NO PURCHASES • NO TIMERS", 12, Teal, true));
    }

    private void RedrawContents()
    {
        _topStats.Text = _game.Cards.Collected + " / " + CardCatalog.TotalCards +
                         " CARDS     ★ " + _game.Cards.State.Stars;
        ResetChildren(_setTabs);
        ResetChildren(_cards);
        ResetChildren(_rewardArea);
        ResetChildren(_revealArea);

        for (int i = 0; i < CardCatalog.SetCount; i++)
        {
            int index = i;
            bool selected = i == _selectedSet;
            var tab = B(CardCatalog.SetNames[i].ToUpperInvariant(),
                selected ? Coral : new Color("#B68E7B"),
                () => { _selectedSet = index; RedrawContents(); }, 44);
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.AddThemeFontSizeOverride("font_size", 11);
            _setTabs.AddChild(tab);
        }

        int owned = CardCatalog.CountOwned(_game.Cards.State, _selectedSet);
        _pageHeader.Text = CardCatalog.SetNames[_selectedSet].ToUpperInvariant();
        _pageProgress.Text = "COLLECTED " + owned + " / " + CardCatalog.CardsPerSet +
            "    •    REWARD: 250 COINS + 100 XP + 2 PACKS";

        for (int number = 0; number < CardCatalog.CardsPerSet; number++)
        {
            var def = CardCatalog.Get(_selectedSet * CardCatalog.CardsPerSet + number);
            bool has = _game.Cards.State.OwnedCardIds.Contains(def.Id);
            var outer = Panel(has ? new Color("#FFEDCF") : new Color("#DAC4B5"),
                6, has ? new Color("#F3C47D") : new Color("#C3AAA2"), 2, 12);
            outer.CustomMinimumSize = new Vector2(0, 157);
            outer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _cards.AddChild(outer);

            var body = new VBoxContainer();
            body.AddThemeConstantOverride("separation", 2);
            outer.AddChild(body);

            var portrait = new CardPortrait
            {
                Card = def,
                Owned = has,
                CustomMinimumSize = new Vector2(0, 85),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            body.AddChild(portrait);
            var name = L(has ? def.Name : "???  LOCKED", 12,
                has ? Ink : new Color("#968781"), true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            body.AddChild(name);
            var starText = L(def.StarsText, 15,
                has ? Gold : new Color("#AD9F97"), true);
            starText.HorizontalAlignment = HorizontalAlignment.Center;
            body.AddChild(starText);
        }

        _rewardArea.AddChild(L("REGULAR: " + _game.Cards.State.Packs, 14, Ink, true));
        _rewardArea.AddChild(B("OPEN 3 CARDS", new Color("#65B974"),
            () => OpenPack(false), 40));
        _rewardArea.AddChild(L("GOLDEN: " + _game.Cards.State.PremiumPacks, 14, Ink, true));
        _rewardArea.AddChild(B("OPEN 5 CARDS", new Color("#E6B54C"),
            () => OpenPack(true), 40));

        _rewardArea.AddChild(new HSeparator());
        bool complete = CardCatalog.IsComplete(_game.Cards.State, _selectedSet);
        bool claimed = _game.Cards.State.ClaimedSetIds.Contains(_selectedSet);
        var rewardButton = B(claimed ? "SET REWARD CLAIMED" :
            complete ? "CLAIM SET REWARD" : "COMPLETE THE SET",
            complete && !claimed ? new Color("#5FAB81") : new Color("#A6B5A4"),
            ClaimSet, 37);
        rewardButton.Disabled = !complete || claimed;
        _rewardArea.AddChild(rewardButton);

        bool all = CardCatalog.IsAlbumComplete(_game.Cards.State) &&
                   _game.Cards.State.ClaimedSetIds.Count == CardCatalog.SetCount;
        bool grandClaimed = _game.Cards.State.GrandPrizeClaimed;
        var grand = B(grandClaimed ? "ALBUM PRIZE CLAIMED" :
            all ? "CLAIM ALBUM PRIZE" : "ALBUM " + _game.Cards.CompleteSets + " / 6 SETS",
            all && !grandClaimed ? new Color("#D8A04E") : new Color("#A5B5AA"),
            ClaimGrandPrize, 38);
        grand.Disabled = !all || grandClaimed;
        _rewardArea.AddChild(grand);

        if (_lastReveal.Count == 0)
        {
            _revealArea.AddChild(L(_notice, 12, Ink, false, true));
        }
        else
        {
            foreach (var result in _lastReveal)
            {
                var line = L((result.NewCard ? "NEW  " : "+" + result.Card.Rarity + " ★  ") +
                    result.Card.Name, 12,
                    result.NewCard ? Teal : Brown, result.NewCard, true);
                _revealArea.AddChild(line);
            }
        }
    }

    private void OpenPack(bool golden)
    {
        if (!_game.Cards.OpenPack(golden, out var reveals))
        {
            _notice = golden ? "No golden packs. Earn one after 10 orders." :
                "No regular packs. Earn one after 2 orders or 8 merges.";
            _lastReveal.Clear();
            RedrawContents();
            return;
        }
        _lastReveal.Clear();
        _lastReveal.AddRange(reveals);
        Changed();
    }

    private void SpendStars(bool golden)
    {
        if (_game.Cards.ExchangeStars(golden))
        {
            _lastReveal.Clear();
            Changed();
        }
        else
        {
            _lastReveal.Clear();
            _notice = "Not enough stars. Duplicates become stars based on rarity.";
            RedrawContents();
        }
    }

    private void ClaimSet()
    {
        if (_game.Cards.ClaimSet(_selectedSet))
        {
            _lastReveal.Clear();
            Changed();
        }
    }

    private void ClaimGrandPrize()
    {
        if (_game.Cards.ClaimGrandPrize())
        {
            _lastReveal.Clear();
            Changed();
        }
    }

    private void Changed()
    {
        RedrawContents();
        StateChanged?.Invoke();
    }

    private static void ResetChildren(Node parent)
    {
        foreach (Node node in parent.GetChildren())
        {
            parent.RemoveChild(node);
            node.QueueFree();
        }
    }

    private static PanelContainer Panel(Color fill, int pad, Color border, int borderWidth, int radius)
    {
        var container = new PanelContainer();
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetBorderWidthAll(borderWidth);
        style.SetCornerRadiusAll(radius);
        style.ContentMarginLeft = pad;
        style.ContentMarginRight = pad;
        style.ContentMarginTop = pad;
        style.ContentMarginBottom = pad;
        container.AddThemeStyleboxOverride("panel", style);
        return container;
    }

    private static Label L(string value, int fontSize, Color color, bool bold = false, bool wrap = false)
    {
        var label = new Label
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        if (bold) label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.12f));
        return label;
    }

    private static Button B(string value, Color fill, Action pressed, int height)
    {
        var button = new Button
        {
            Text = value,
            CustomMinimumSize = new Vector2(0, height),
            FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 13);
        foreach (var name in new[] { "normal", "hover", "pressed" })
        {
            var style = new StyleBoxFlat { BgColor = name == "hover" ? fill.Lightened(0.13f) :
                name == "pressed" ? fill.Darkened(0.1f) : fill, BorderColor = new Color("#FFF9E3", 0.7f) };
            style.SetCornerRadiusAll(10);
            style.SetBorderWidthAll(1);
            style.ContentMarginLeft = 6;
            style.ContentMarginRight = 6;
            style.ContentMarginTop = 5;
            style.ContentMarginBottom = 5;
            button.AddThemeStyleboxOverride(name, style);
        }
        button.Pressed += pressed;
        return button;
    }
}

public partial class CardPortrait : Control
{
    public CardDefinition Card { get; set; }
    public bool Owned { get; set; }

    public override void _Draw()
    {
        if (Size.X <= 1 || Size.Y <= 1) return;
        var rect = new Rect2(Vector2.Zero, Size);
        var colors = new[]
        {
            new Color("#88D6C9"), new Color("#F6B9A7"), new Color("#8CC8DF"),
            new Color("#B7DDA1"), new Color("#F4D08B"), new Color("#D8BADF")
        };
        Color tone = Owned ? colors[Card.SetIndex] : new Color("#BDAFA7");
        var backing = new StyleBoxFlat { BgColor = tone };
        backing.SetCornerRadiusAll(8);
        DrawStyleBox(backing, rect.Grow(-2));
        for (int i = 0; i < 5; i++)
        {
            var x = Size.X * (0.15f + i * 0.17f);
            DrawCircle(new Vector2(x, Size.Y * 0.3f), 3f,
                new Color(1, 1, 1, 0.18f));
        }
        if (Owned)
        {
            int family = Card.SetIndex % 3;
            int tier = Math.Min(10, 1 + (Card.Id % CardCatalog.CardsPerSet));
            FoodArt.Draw(this, rect.GetCenter(), Math.Min(Size.Y * 0.36f, Size.X * 0.26f),
                family, tier);
        }
        else
        {
            DrawString(GetThemeDefaultFont(), rect.GetCenter() + new Vector2(-30, 20), "?",
                HorizontalAlignment.Center, 60, 53, new Color("#F7EFDD"));
        }
    }
}
