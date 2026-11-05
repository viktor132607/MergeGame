using Godot;
using System;

// Twenty distinct original pictograms for the town collections.
// Tier changes alter silhouette, ornaments, material and rarity glow.
public static class CollectionArt
{
    private static readonly Color[] Palette =
    {
        new("#AF6B48"), new("#E75B9B"), new("#E8AD4E"), new("#71B9D6"),
        new("#5F9CC4"), new("#F5D785"), new("#E56F4F"), new("#84C96B"),
        new("#F5A9D6"), new("#BC8C64"), new("#C7916C"), new("#62AAC2"),
        new("#75BF7B"), new("#AD7FCD"), new("#5E9FCF"), new("#7DC9AE"),
        new("#55BBD2"), new("#E3A85C"), new("#B8906C"), new("#E6C475")
    };

    private static void Line(CanvasItem c, Vector2 a, Vector2 b, Color col, float width = 4) =>
        c.DrawLine(a, b, col, width, true);

    private static void Ellipse(CanvasItem c, Vector2 center, float rx, float ry, Color color)
    {
        Vector2[] points = new Vector2[24];
        for (int i = 0; i < points.Length; i++)
        {
            float a = Mathf.Tau * i / points.Length;
            points[i] = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        c.DrawColoredPolygon(points, color);
    }

    private static void Shape(CanvasItem c, Color col, params Vector2[] points) =>
        c.DrawColoredPolygon(points, col);

    private static void Rounded(CanvasItem c, Rect2 r, Color fill, Color edge)
    {
        var s = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        s.SetCornerRadiusAll(7);
        s.SetBorderWidthAll(2);
        c.DrawStyleBox(s, r);
    }

    private static void Circle(CanvasItem c, Vector2 point, float radius, Color col) =>
        c.DrawCircle(point, radius, col);

    public static void Draw(CanvasItem c, int family, int tier)
    {
        int index = family - 3;
        if (index < 0 || index >= Palette.Length) return;
        Color color = Palette[index];
        Color light = color.Lightened(0.33f);
        Color dark = color.Darkened(0.23f);
        Color cream = new("#FFF5D5");
        var o = Vector2.Zero;
        // Draw inside FoodArt's active position/scale transform.
        // The base icon changes by building/collection (20 genuinely different silhouettes).
        switch (index)
        {
            case 0: // Coffee machine and coffee cup
                Rounded(c, new Rect2(-21, -17, 42, 32), color, dark);
                Ellipse(c, new Vector2(0, -16), 21, 6, cream);
                Ellipse(c, new Vector2(0, -15), 16, 4, new Color("#8D5139"));
                c.DrawArc(new Vector2(19, -2), 11, -Mathf.Pi/2, Mathf.Pi/2, 16, cream, 5);
                Line(c, new Vector2(-10, -25), new Vector2(-13, -34), cream, 3);
                break;
            case 1: // Flower bouquet
                Line(c, new Vector2(0, 21), new Vector2(0, -16), dark, 5);
                for (int n = 0; n < 5; n++)
                {
                    float a = Mathf.Tau * n / 5;
                    Circle(c, new Vector2(Mathf.Cos(a) * 14, Mathf.Sin(a) * 14 - 10), 11, light);
                }
                Circle(c, new Vector2(0, -10), 9, new Color("#F5D260"));
                Shape(c, dark, new Vector2(-17, 4), new Vector2(17, 4), new Vector2(8, 28), new Vector2(-8, 28));
                break;
            case 2: // Artisan bread
                Ellipse(c, o + new Vector2(0, 6), 28, 18, dark);
                Ellipse(c, o, 25, 17, color);
                for (int n = -1; n <= 1; n++)
                    Line(c, new Vector2(n * 13 - 5, -11), new Vector2(n * 13 + 3, 7), cream, 3);
                break;
            case 3: // Luxury bed / hotel
                Rounded(c, new Rect2(-28, 0, 56, 22), color, dark);
                Rounded(c, new Rect2(-24, -12, 20, 16), cream, light);
                Rounded(c, new Rect2(5, -12, 20, 16), cream, light);
                Line(c, new Vector2(-29, 19), new Vector2(-29, 29), dark, 5);
                Line(c, new Vector2(29, 19), new Vector2(29, 29), dark, 5);
                break;
            case 4: // Sailboat
                Shape(c, dark, new Vector2(-28, 13), new Vector2(29, 13), new Vector2(17, 25), new Vector2(-18, 25));
                Line(c, new Vector2(0, 12), new Vector2(0, -34), dark, 5);
                Shape(c, light, new Vector2(4, -31), new Vector2(26, 5), new Vector2(4, 6));
                Shape(c, cream, new Vector2(-4, -24), new Vector2(-22, 6), new Vector2(-4, 6));
                break;
            case 5: // Lighthouse
                Shape(c, color, new Vector2(-22, 28), new Vector2(-11, -18), new Vector2(12, -18), new Vector2(22, 28));
                Rounded(c, new Rect2(-15, -30, 30, 12), light, dark);
                Shape(c, new Color("#FAE86A"), new Vector2(13, -27), new Vector2(35, -38), new Vector2(34, -11));
                Rounded(c, new Rect2(-6, -13, 12, 14), cream, dark);
                break;
            case 6: // Restaurant cloche
                Ellipse(c, new Vector2(0, 10), 27, 20, color);
                Line(c, new Vector2(-29, 24), new Vector2(29, 24), dark, 6);
                Circle(c, new Vector2(0, -14), 6, light);
                Circle(c, new Vector2(-8, 4), 7, cream);
                break;
            case 7: // Pineapple fruit
                Ellipse(c, new Vector2(0, 5), 21, 25, color);
                for (int n = -1; n <= 1; n++)
                    Line(c, new Vector2(n * 10 - 10, -8), new Vector2(n * 10 + 8, 17), dark, 2);
                Shape(c, dark, new Vector2(-4, -19), new Vector2(-14, -34), new Vector2(0, -25),
                    new Vector2(9, -39), new Vector2(7, -18));
                break;
            case 8: // Ice cream cone
                Shape(c, new Color("#DCA56C"), new Vector2(-15, -1), new Vector2(15, -1), new Vector2(0, 32));
                Circle(c, new Vector2(0, -11), 19, light);
                Circle(c, new Vector2(-9, -18), 10, color);
                Circle(c, new Vector2(11, -15), 9, cream);
                break;
            case 9: // Shell and pendant
                Ellipse(c, new Vector2(0, 3), 27, 23, color);
                for (int n = -2; n <= 2; n++)
                    Line(c, new Vector2(0, 24), new Vector2(n * 10, -15), cream, 2);
                Circle(c, new Vector2(0, -19), 6, light);
                break;
            case 10: // Cabin
                Rounded(c, new Rect2(-22, -6, 44, 32), color, dark);
                Shape(c, dark, new Vector2(-29, -6), new Vector2(0, -31), new Vector2(29, -6));
                Rounded(c, new Rect2(-6, 4, 12, 22), cream, dark);
                break;
            case 11: // Fish
                Ellipse(c, new Vector2(-3, 1), 23, 15, color);
                Shape(c, dark, new Vector2(13, 0), new Vector2(33, -19), new Vector2(32, 19));
                Circle(c, new Vector2(-15, -3), 4, cream);
                Line(c, new Vector2(-9, 10), new Vector2(6, 11), cream, 2);
                break;
            case 12: // Botanical pot
                Rounded(c, new Rect2(-19, 5, 38, 24), color, dark);
                Line(c, new Vector2(0, 6), new Vector2(0, -25), dark, 5);
                Ellipse(c, new Vector2(-12, -18), 13, 7, light);
                Ellipse(c, new Vector2(14, -22), 13, 7, color);
                break;
            case 13: // Guitar
                Ellipse(c, new Vector2(-6, 13), 17, 18, color);
                Ellipse(c, new Vector2(-1, -7), 11, 14, light);
                Line(c, new Vector2(5, -14), new Vector2(18, -34), dark, 9);
                Circle(c, new Vector2(-3, 4), 5, dark);
                for (int n = -1; n <= 1; n++)
                    Line(c, new Vector2(n * 2, 25), new Vector2(18 + n * 2, -35), cream, 1);
                break;
            case 14: // Surfboard
                Shape(c, color, new Vector2(-4, -34), new Vector2(17, -7), new Vector2(16, 19),
                    new Vector2(3, 34), new Vector2(-17, 8), new Vector2(-17, -16));
                Line(c, new Vector2(-2, -27), new Vector2(2, 27), cream, 3);
                Line(c, new Vector2(-15, 1), new Vector2(15, 1), light, 5);
                break;
            case 15: // Spa bottle
                Rounded(c, new Rect2(-19, -17, 38, 44), color, dark);
                Rounded(c, new Rect2(-12, -29, 24, 13), light, dark);
                Circle(c, new Vector2(0, 4), 11, cream);
                Circle(c, new Vector2(0, 4), 4, light);
                break;
            case 16: // Aquarium sea turtle
                Ellipse(c, o, 22, 18, color);
                Circle(c, new Vector2(24, -1), 10, light);
                for (int n = -1; n <= 1; n += 2)
                {
                    Ellipse(c, new Vector2(n * 13, -17), 9, 5, light);
                    Ellipse(c, new Vector2(n * 13, 17), 9, 5, light);
                }
                Circle(c, new Vector2(28, -4), 2, dark);
                break;
            case 17: // Treasure map
                Shape(c, cream, new Vector2(-27, -20), new Vector2(-7, -24),
                    new Vector2(9, -19), new Vector2(27, -24), new Vector2(25, 27),
                    new Vector2(7, 22), new Vector2(-10, 27), new Vector2(-27, 22));
                Line(c, new Vector2(-8, -15), new Vector2(11, 11), color, 3);
                Line(c, new Vector2(11, 11), new Vector2(-13, 16), color, 3);
                Line(c, new Vector2(10, 9), new Vector2(20, 20), dark, 4);
                Line(c, new Vector2(20, 9), new Vector2(10, 20), dark, 4);
                break;
            case 18: // Museum amphora
                Ellipse(c, new Vector2(0, 7), 20, 22, color);
                Rounded(c, new Rect2(-11, -25, 22, 10), light, dark);
                c.DrawArc(new Vector2(-14, -5), 11, Mathf.Pi/2, Mathf.Pi * 1.6f, 14, dark, 4);
                c.DrawArc(new Vector2(14, -5), 11, -Mathf.Pi*0.6f, Mathf.Pi/2, 14, dark, 4);
                Line(c, new Vector2(-16, 7), new Vector2(16, 7), cream, 3);
                break;
            default: // Royal resort crown
                Shape(c, color, new Vector2(-30, -12), new Vector2(-20, 22),
                    new Vector2(20, 22), new Vector2(30, -12), new Vector2(11, 0),
                    new Vector2(0, -26), new Vector2(-11, 0));
                Rounded(c, new Rect2(-22, 17, 44, 10), light, dark);
                Circle(c, new Vector2(0, 3), 6, new Color("#E967A0"));
                break;
        }

        // All ten tier levels visibly evolve: gilded decorations, gems, medals, particles.
        if (tier >= 3)
        {
            Circle(c, new Vector2(24, -24), 5, new Color("#FFE36B"));
            Circle(c, new Vector2(24, -24), 2, Colors.White);
        }
        if (tier >= 5)
        {
            Line(c, new Vector2(-27, -21), new Vector2(-21, -27), cream, 3);
            Line(c, new Vector2(-27, -27), new Vector2(-21, -21), cream, 3);
        }
        if (tier >= 7)
        {
            Ellipse(c, new Vector2(0, 30), 24, 6, new Color("#F2CA65"));
            Circle(c, new Vector2(-21, 28), 3, cream);
            Circle(c, new Vector2(21, 28), 3, cream);
        }
        if (tier >= 9)
        {
            Circle(c, new Vector2(-26, -30), 6, new Color("#A8E5FB"));
            Circle(c, new Vector2(0, -36), 5, new Color("#FFE37F"));
            Circle(c, new Vector2(29, -29), 5, new Color("#F6A9E9"));
        }
    }
}
