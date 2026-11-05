using Godot;
using System;

// Original hand-drawn vector illustrations rendered inside Godot.
// No textures, external services, commercial game assets or image generators.
public static class FoodArt
{
    private static readonly Color Outline = new("#9B6B51");
    private static readonly Color Leaf = new("#5BAE60");
    private static readonly Color LeafDark = new("#358457");
    private static readonly Color Shine = new(1f, 1f, 1f, 0.72f);
    private static readonly Color Gold = new("#FFD34C");
    private static readonly Color Cocoa = new("#9A5741");

    public static void Draw(CanvasItem canvas, Vector2 center, float radius, int family, int tier)
    {
        if (tier < 1) tier = 1;
        if (tier > 10) tier = 10;
        float s = Mathf.Max(0.1f, radius / 30f);
        canvas.DrawSetTransform(center, 0f, new Vector2(s, s));
        canvas.DrawEllipseShadow();
        switch (family)
        {
            case 0: Fruit(canvas, tier); break;
            case 1: Bakery(canvas, tier); break;
            default: Drinks(canvas, tier); break;
        }
        canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    private static void Ellipse(CanvasItem c, float x, float y, float rx, float ry, Color fill)
    {
        // Smooth ellipse using 24 points rather than using a raster image.
        var points = new Vector2[25];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / 24f;
            points[i] = new Vector2(x + Mathf.Cos(angle) * rx, y + Mathf.Sin(angle) * ry);
        }
        c.DrawColoredPolygon(points, fill);
    }

    private static void Poly(CanvasItem c, Color color, params Vector2[] points)
        => c.DrawColoredPolygon(points, color);

    private static void Line(CanvasItem c, Color color, float width, Vector2 a, Vector2 b)
        => c.DrawLine(a, b, color, width, true);

    private static void Fruit(CanvasItem c, int tier)
    {
        switch (tier)
        {
            case 1: // Seed
                Ellipse(c, 0, 2, 13, 20, new Color("#A96A42"));
                Ellipse(c, -3, -4, 7, 12, new Color("#D79559"));
                Ellipse(c, -5, -8, 2.5f, 6, Shine);
                break;
            case 2: // Sprout
                Ellipse(c, 0, 18, 19, 7, new Color("#A77851"));
                Line(c, LeafDark, 5, new Vector2(0, 15), new Vector2(0, -16));
                Ellipse(c, -12, -12, 13, 7, Leaf);
                Ellipse(c, 11, -18, 13, 7, new Color("#91D65D"));
                Line(c, new Color("#C4EE8E"), 2, new Vector2(-21, -14), new Vector2(-3, -11));
                break;
            case 3: // Red berries
                foreach (var p in new[] { new Vector2(-12, 3), new Vector2(9, 6), new Vector2(-2, -12), new Vector2(0, 17) })
                {
                    c.DrawCircle(p, 12, new Color("#CE3257"));
                    c.DrawCircle(p + new Vector2(-3, -4), 6, new Color("#F96275"));
                    c.DrawCircle(p + new Vector2(-5, -6), 2.6f, Shine);
                }
                Ellipse(c, -7, -20, 9, 5, Leaf);
                Ellipse(c, 8, -20, 9, 5, LeafDark);
                break;
            case 4: // Apple
                c.DrawCircle(new Vector2(-9, 3), 19, new Color("#E94646"));
                c.DrawCircle(new Vector2(9, 3), 19, new Color("#FA6654"));
                Ellipse(c, -12, -5, 8, 15, new Color("#FF9283"));
                Line(c, Cocoa, 4, new Vector2(0, -12), new Vector2(1, -29));
                Ellipse(c, 12, -24, 12, 5, Leaf);
                break;
            case 5: // Citrus
                c.DrawCircle(Vector2.Zero, 27, new Color("#F7B52D"));
                c.DrawCircle(Vector2.Zero, 22, new Color("#FFE777"));
                c.DrawCircle(Vector2.Zero, 18, new Color("#FFCC40"));
                for (int i = 0; i < 8; i++)
                {
                    float angle = Mathf.Tau * i / 8;
                    Line(c, new Color("#FFF4BA"), 3, new Vector2(2 * Mathf.Cos(angle), 2 * Mathf.Sin(angle)),
                         new Vector2(19 * Mathf.Cos(angle), 19 * Mathf.Sin(angle)));
                }
                c.DrawCircle(Vector2.Zero, 3, new Color("#FFF0A2"));
                break;
            case 6: // Grape cluster
                Ellipse(c, 1, -23, 13, 6, Leaf);
                for (int row = 0; row < 4; row++)
                {
                    int count = 4 - row;
                    for (int i = 0; i < count; i++)
                    {
                        float x = (i - (count - 1) / 2f) * 13;
                        float y = -13 + row * 13;
                        c.DrawCircle(new Vector2(x, y), 9, new Color("#8745BB"));
                        c.DrawCircle(new Vector2(x - 2, y - 3), 4, new Color("#C07DE9"));
                    }
                }
                break;
            case 7: // Bowl of fruit
                Ellipse(c, 0, 8, 29, 12, new Color("#E6F9F1"));
                c.DrawCircle(new Vector2(-12, -3), 14, new Color("#F55356"));
                c.DrawCircle(new Vector2(7, -9), 15, Gold);
                c.DrawCircle(new Vector2(15, 4), 10, new Color("#8F5AC1"));
                Poly(c, new Color("#5CC3B0"), new Vector2(-28, 4), new Vector2(28, 4),
                    new Vector2(21, 23), new Vector2(-20, 23));
                Ellipse(c, 0, 5, 28, 5, new Color("#C1F0E3"));
                break;
            case 8: // Smoothie
                Glass(c, new Color("#E45C92"), true);
                c.DrawCircle(new Vector2(-9, -19), 8, new Color("#FA7075"));
                c.DrawCircle(new Vector2(7, -21), 8, new Color("#F8D05E"));
                Line(c, new Color("#6EC8D3"), 4, new Vector2(10, -15), new Vector2(19, -35));
                Ellipse(c, -8, -25, 6, 3, Leaf);
                break;
            case 9: // Jam set
                Jar(c, new Vector2(-12, 1), new Color("#E94871"));
                Jar(c, new Vector2(13, 1), new Color("#B752B2"));
                break;
            default: // Feast
                Ellipse(c, 0, 13, 31, 15, new Color("#83C3C3"));
                Ellipse(c, 0, 9, 28, 12, new Color("#FDF1D0"));
                c.DrawCircle(new Vector2(-13, 1), 14, new Color("#E94D54"));
                c.DrawCircle(new Vector2(12, -4), 15, new Color("#F7CC4E"));
                c.DrawCircle(new Vector2(9, 12), 8, new Color("#A556B1"));
                Ellipse(c, 0, -19, 14, 6, Leaf);
                break;
        }
    }

    private static void Bakery(CanvasItem c, int tier)
    {
        switch (tier)
        {
            case 1: // Flour sack
                Poly(c, new Color("#D7B793"), new Vector2(-19, -20), new Vector2(17, -20),
                    new Vector2(25, 20), new Vector2(-24, 20));
                Ellipse(c, 0, -20, 19, 6, new Color("#FFF3E0"));
                Ellipse(c, -6, -4, 10, 12, new Color("#FFF5E6"));
                Line(c, new Color("#C69E6C"), 2, new Vector2(-9, -2), new Vector2(8, 8));
                break;
            case 2: // Dough
                Ellipse(c, 0, 8, 26, 19, new Color("#EAB26C"));
                Ellipse(c, -4, 1, 21, 14, new Color("#F7D49D"));
                Ellipse(c, -10, -3, 8, 4, Shine);
                break;
            case 3: // Cookie
                c.DrawCircle(Vector2.Zero, 27, new Color("#D18B50"));
                c.DrawCircle(new Vector2(-3, -3), 22, new Color("#F4C578"));
                foreach (var p in new[] { new Vector2(-12, -12), new Vector2(11, -13),
                        new Vector2(-5, 4), new Vector2(14, 11), new Vector2(-15, 15) })
                    c.DrawCircle(p, 4.7f, Cocoa);
                break;
            case 4: // Croissant
                Ellipse(c, -10, 4, 18, 15, new Color("#B96E32"));
                Ellipse(c, 9, 4, 19, 15, new Color("#D9883D"));
                Ellipse(c, 0, 0, 19, 20, new Color("#FFC66A"));
                Line(c, new Color("#E89B4B"), 3, new Vector2(-11, -14), new Vector2(-9, 13));
                Line(c, new Color("#E89B4B"), 3, new Vector2(10, -14), new Vector2(10, 13));
                break;
            case 5: // Cupcake
                Poly(c, new Color("#C47FB3"), new Vector2(-21, 1), new Vector2(21, 1),
                    new Vector2(15, 26), new Vector2(-15, 26));
                for (int i = -1; i <= 1; i++)
                    Line(c, new Color("#FFE0EC"), 2, new Vector2(i * 10, 6), new Vector2(i * 8, 22));
                c.DrawCircle(new Vector2(-11, -5), 14, new Color("#FFB0C7"));
                c.DrawCircle(new Vector2(9, -7), 16, new Color("#FFCAD6"));
                c.DrawCircle(new Vector2(0, -17), 13, new Color("#FFE0E8"));
                c.DrawCircle(new Vector2(0, -28), 5.5f, new Color("#E34A62"));
                break;
            case 6: // Tart
                Ellipse(c, 0, 13, 28, 13, new Color("#BF7A45"));
                Ellipse(c, 0, 7, 26, 11, new Color("#F6CE7C"));
                foreach (var p in new[] { new Vector2(-15, 3), new Vector2(5, -3), new Vector2(16, 7) })
                    c.DrawCircle(p, 9, new Color("#D34D78"));
                Ellipse(c, -2, -15, 10, 5, Leaf);
                break;
            case 7: // Pie
                Ellipse(c, 0, 16, 30, 10, new Color("#D19D68"));
                Ellipse(c, 0, 9, 27, 17, new Color("#E9B36A"));
                for (int i = -2; i <= 2; i++)
                    Line(c, new Color("#F6D59B"), 4, new Vector2(-20, i * 7 + 10),
                         new Vector2(20, i * 7 + 10));
                break;
            case 8: // Celebration cake
                Cake(c, true);
                break;
            case 9: // Dessert set
                Cake(c, false);
                c.DrawCircle(new Vector2(-22, 16), 10, new Color("#D88148"));
                c.DrawCircle(new Vector2(22, 16), 10, new Color("#F1B25B"));
                break;
            default: // Banquet
                Cake(c, true);
                c.DrawCircle(new Vector2(-25, 12), 10, new Color("#DF8948"));
                c.DrawCircle(new Vector2(24, 14), 10, new Color("#E65B84"));
                c.DrawCircle(new Vector2(0, -31), 5, Gold);
                break;
        }
    }

    private static void Drinks(CanvasItem c, int tier)
    {
        switch (tier)
        {
            case 1:
                Glass(c, new Color("#A1E5F3"), false);
                break;
            case 2:
                Mug(c, new Color("#C77746"));
                Ellipse(c, 0, -8, 16, 5, new Color("#92502E"));
                break;
            case 3:
                Glass(c, new Color("#F1B34C"), false);
                c.DrawCircle(new Vector2(19, -19), 8, Gold);
                break;
            case 4:
                Glass(c, new Color("#EF76B5"), true);
                c.DrawCircle(new Vector2(-7, -21), 7, new Color("#F4E6EF"));
                Line(c, new Color("#54B3CD"), 3, new Vector2(11, -12), new Vector2(19, -33));
                break;
            case 5:
                Mug(c, new Color("#F4ECE1"));
                Ellipse(c, 0, -9, 16, 6, new Color("#9E5D38"));
                Ellipse(c, -4, -10, 7, 3, new Color("#D1AC88"));
                break;
            case 6:
                Glass(c, new Color("#D8AA83"), false);
                Ellipse(c, 0, -18, 19, 6, new Color("#F5EEE2"));
                Line(c, new Color("#C4647A"), 3, new Vector2(10, -15), new Vector2(17, -34));
                break;
            case 7:
                Ellipse(c, 0, 6, 28, 21, new Color("#88D7CA"));
                Ellipse(c, 0, 1, 25, 15, new Color("#F783A0"));
                c.DrawCircle(new Vector2(-10, -8), 7, new Color("#F9D44A"));
                c.DrawCircle(new Vector2(11, -10), 8, new Color("#6EBD68"));
                Line(c, new Color("#F4F2E0"), 3, new Vector2(8, -10), new Vector2(16, -31));
                break;
            case 8:
                Poly(c, new Color("#55B7D6"), new Vector2(-16, -25), new Vector2(16, -25),
                    new Vector2(16, 25), new Vector2(-16, 25));
                Ellipse(c, 0, -25, 16, 5, new Color("#C5E9F3"));
                Ellipse(c, 0, 25, 16, 4, new Color("#318DB4"));
                Poly(c, new Color("#FFF9DE"), new Vector2(-9, -8), new Vector2(10, -8),
                    new Vector2(10, 14), new Vector2(-9, 14));
                c.DrawCircle(new Vector2(0, 3), 7, new Color("#F9BA59"));
                break;
            case 9:
                Glass(c, new Color("#51C3B7"), true);
                c.DrawCircle(new Vector2(10, -19), 9, Gold);
                c.DrawCircle(new Vector2(-11, -15), 6, new Color("#EE7FA9"));
                break;
            default:
                Glass(c, new Color("#E7AF56"), true);
                c.DrawCircle(new Vector2(0, -26), 6, Gold);
                c.DrawCircle(new Vector2(-14, -18), 7, new Color("#E9688D"));
                c.DrawCircle(new Vector2(15, -18), 8, new Color("#67D9B4"));
                Line(c, Gold, 3, new Vector2(0, -30), new Vector2(0, -41));
                break;
        }
    }

    private static void Glass(CanvasItem c, Color drink, bool festive)
    {
        Poly(c, new Color("#D4F8FF"), new Vector2(-21, -20), new Vector2(21, -20),
            new Vector2(16, 26), new Vector2(-16, 26));
        Poly(c, drink, new Vector2(-18, -13), new Vector2(18, -13),
            new Vector2(14, 23), new Vector2(-14, 23));
        Line(c, new Color("#FFFFFF", 0.83f), 3, new Vector2(-15, -12), new Vector2(-12, 18));
        Ellipse(c, 0, -21, 21, 6, new Color("#EFFAFF"));
        if (festive)
        {
            c.DrawCircle(new Vector2(-9, -25), 7, new Color("#FFF4ED"));
            c.DrawCircle(new Vector2(7, -23), 7, new Color("#FFF8EE"));
            Line(c, new Color("#E87082"), 4, new Vector2(13, -16), new Vector2(22, -36));
        }
    }

    private static void Mug(CanvasItem c, Color fill)
    {
        c.DrawArc(new Vector2(18, 0), 12, -Mathf.Pi / 2, Mathf.Pi / 2,
                  16, new Color("#E9E4DD"), 7f, true);
        Poly(c, new Color("#FBFBF5"), new Vector2(-22, -18), new Vector2(18, -18),
            new Vector2(14, 20), new Vector2(-18, 20));
        Ellipse(c, -2, -18, 20, 6, fill);
        Line(c, new Color("#E0DED6"), 3, new Vector2(-18, -12), new Vector2(-14, 17));
    }

    private static void Cake(CanvasItem c, bool cherry)
    {
        Poly(c, new Color("#E18E9F"), new Vector2(-24, -11), new Vector2(24, -11),
            new Vector2(24, 23), new Vector2(-24, 23));
        c.DrawRect(new Rect2(-24, 1, 48, 7), new Color("#FAE4C5"));
        c.DrawRect(new Rect2(-24, 12, 48, 5), new Color("#F7D2BC"));
        Ellipse(c, 0, -12, 25, 9, new Color("#FFF3E3"));
        for (int i = -1; i <= 1; i++)
            c.DrawCircle(new Vector2(i * 16, -16), 7, new Color("#FFE3F0"));
        if (cherry) c.DrawCircle(new Vector2(0, -27), 7, new Color("#DF526A"));
    }

    private static void Jar(CanvasItem c, Vector2 pos, Color fill)
    {
        var b = new Rect2(pos.X - 12, pos.Y - 14, 24, 35);
        c.DrawRect(b, new Color("#F3E8E2"));
        c.DrawRect(new Rect2(pos.X - 10, pos.Y - 10, 20, 27), fill);
        c.DrawRect(new Rect2(pos.X - 13, pos.Y - 19, 26, 8), new Color("#F2C96F"));
        Ellipse(c, pos.X - 3, pos.Y + 3, 6, 7, new Color("#FFE7DD", 0.6f));
    }

    private static void DrawEllipseShadow(this CanvasItem c)
    {
        Ellipse(c, 0, 26, 24, 4, new Color("#6D4A55", 0.14f));
    }
}
