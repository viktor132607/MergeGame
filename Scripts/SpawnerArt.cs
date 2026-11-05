using Godot;
using MergeHarvest;
using System;

// Original scalable drawings for generators and generator components.
public static class SpawnerArt
{
    private static readonly Color[] Tints =
    {
        new("#DE875B"), new("#DFA45B"), new("#65BDD2"), new("#A77A62"),
        new("#D78CAC"), new("#D6A064"), new("#B3A5DD"), new("#71B9CF"),
        new("#F0CA70"), new("#E8A265"), new("#76C58A"), new("#F0A9D0"),
        new("#C39871"), new("#C58F66"), new("#69B2CD"), new("#7ABD85"),
        new("#B68DD6"), new("#68C8DB"), new("#91C3B3"), new("#6FAAC8"),
        new("#CDA36E"), new("#A7C8A8"), new("#D3B26B")
    };
    private static void Circle(CanvasItem canvas, Vector2 point, float radius, Color fill) =>
        canvas.DrawCircle(point, radius, fill);
    private static void Line(CanvasItem canvas, Vector2 a, Vector2 b, Color color, float width = 3f) =>
        canvas.DrawLine(a, b, color, width, true);
    private static void Rounded(CanvasItem canvas, Rect2 rect, Color fill, Color border, int radius)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetCornerRadiusAll(radius);
        style.SetBorderWidthAll(3);
        canvas.DrawStyleBox(style, rect);
    }

    public static void Draw(CanvasItem canvas, Vector2 position, float radius,
        int family, int tier, TileKind kind)
    {
        Color tint = Tints[Mathf.Clamp(family, 0, Tints.Length - 1)];
        Color edge = tint.Darkened(0.29f);
        Color shine = tint.Lightened(0.37f);
        float size = Mathf.Clamp(radius / 35f, 0.3f, 1.65f);
        canvas.DrawSetTransform(position, 0, new Vector2(size, size));

        if (kind == TileKind.Generator)
        {
            // A different-colored countertop machine with a glowing top, window,
            // funnel and production arrow. Higher stages gain chrome and stars.
            Rounded(canvas, new Rect2(-25, -14, 50, 42), tint, edge, 8);
            Rounded(canvas, new Rect2(-19, -32, 38, 22), shine, edge, 8);
            Rounded(canvas, new Rect2(-14, -8, 28, 20), new Color("#F9F1DE"), edge, 5);
            Circle(canvas, new Vector2(0, 1), 7f, tint);
            Circle(canvas, new Vector2(0, 1), 3.3f, shine);
            Circle(canvas, new Vector2(15, 20), 4f, new Color("#FFF5AF"));
            Rounded(canvas, new Rect2(-22, 25, 44, 7), edge, edge, 3);
            if (tier > 1)
            {
                Line(canvas, new Vector2(-21, -22), new Vector2(21, -22),
                    new Color("#FDE9A5"), 4);
                Circle(canvas, new Vector2(22, -21), 6, new Color("#FFE17C"));
            }
            if (tier > 2)
                for (int i = -1; i <= 1; i++)
                    Circle(canvas, new Vector2(i * 11, -39), 4, new Color("#FFE17C"));
            if (tier > 3)
            {
                Line(canvas, new Vector2(-29, 9), new Vector2(-29, 24), shine, 5);
                Line(canvas, new Vector2(29, 9), new Vector2(29, 24), shine, 5);
            }
            if (tier == 5)
                Circle(canvas, new Vector2(0, -44), 8, new Color("#FFF2B9"));
        }
        else
        {
            // Component chain: bolt -> gears -> motor -> assembled machine.
            if (tier == 1)
            {
                Rounded(canvas, new Rect2(-12, -25, 24, 50), tint, edge, 6);
                Line(canvas, new Vector2(-8, -16), new Vector2(8, -16),
                    new Color("#F8E8CD"), 5);
                Line(canvas, new Vector2(-8, 15), new Vector2(8, 15),
                    new Color("#F8E8CD"), 5);
            }
            else if (tier == 2)
            {
                Circle(canvas, Vector2.Zero, 27, edge);
                for (int i = 0; i < 8; i++)
                {
                    float angle = Mathf.Tau * i / 8f;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Circle(canvas, dir * 24, 8, tint);
                }
                Circle(canvas, Vector2.Zero, 19, shine);
                Circle(canvas, Vector2.Zero, 8, edge);
                Circle(canvas, Vector2.Zero, 4, new Color("#F9F4DF"));
            }
            else
            {
                Rounded(canvas, new Rect2(-23, -22, 46, 47), tint, edge, 8);
                Circle(canvas, new Vector2(0, -1), 15, shine);
                Circle(canvas, new Vector2(0, -1), 6, edge);
                Line(canvas, new Vector2(-12, 19), new Vector2(12, 19),
                    new Color("#F9E9B7"), 4);
                Line(canvas, new Vector2(-7, -30), new Vector2(7, -30),
                    new Color("#F5EAA0"), 5);
            }
        }
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
