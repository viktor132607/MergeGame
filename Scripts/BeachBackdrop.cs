using Godot;
using System;

// Scenic, original beach/cafe backdrop drawn with Godot primitives.
public partial class BeachBackdrop : Control
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        var w = Size.X;
        var h = Size.Y;
        if (w <= 1 || h <= 1) return;

        var top = new Color("#79D5E9");
        var horizon = new Color("#E4F9EE");
        for (int i = 0; i < 30; i++)
        {
            var t = i / 29f;
            DrawRect(new Rect2(0, h * i / 30f, w, h / 30f + 2),
                top.Lerp(horizon, Mathf.Min(1, t * 1.3f)));
        }

        DrawCloud(w * 0.13f, h * 0.15f, 1.3f);
        DrawCloud(w * 0.77f, h * 0.12f, 1.1f);
        DrawCloud(w * 0.48f, h * 0.07f, 0.8f);
        DrawRect(new Rect2(0, h * 0.58f, w, h * 0.14f), new Color("#46C9C6"));
        DrawRect(new Rect2(0, h * 0.7f, w, h * 0.055f), new Color("#B3EEE0"));
        DrawRect(new Rect2(0, h * 0.75f, w, h * 0.25f), new Color("#F3D99A"));
        DrawRect(new Rect2(0, h * 0.82f, w, h * 0.035f), new Color("#F9E5B5", 0.55f));

        DrawIsland(new Vector2(w * 0.04f, h * 0.66f), 1.0f);
        DrawIsland(new Vector2(w * 0.90f, h * 0.68f), 1.3f);
        DrawPalm(new Vector2(w * 0.075f, h * 0.74f), h * 0.37f, false);
        DrawPalm(new Vector2(w * 0.925f, h * 0.76f), h * 0.44f, true);

        // Soft sea foam and beach shadows.
        for (int i = 0; i < 8; i++)
        {
            var x = (i + 0.4f) * w / 8f;
            DrawLine(new Vector2(x, h * 0.73f), new Vector2(x + 35, h * 0.73f),
                new Color("#F4FFEE", 0.65f), 2f, true);
        }
        DrawCircle(new Vector2(82, h - 45), 11, new Color("#F7BFA6"));
        DrawCircle(new Vector2(w - 93, h - 70), 7, new Color("#FFDC81"));
    }

    private void DrawCloud(float x, float y, float scale)
    {
        var white = new Color("#FFFFFF", 0.7f);
        DrawCircle(new Vector2(x - 27 * scale, y + 3 * scale), 19 * scale, white);
        DrawCircle(new Vector2(x, y - 5 * scale), 26 * scale, white);
        DrawCircle(new Vector2(x + 30 * scale, y + 2 * scale), 20 * scale, white);
    }

    private void DrawIsland(Vector2 center, float scale)
    {
        var sand = new Color("#F6E4A8");
        DrawColoredPolygon(new[]
        {
            center + new Vector2(-80, 0) * scale,
            center + new Vector2(-38, -18) * scale,
            center + new Vector2(30, -20) * scale,
            center + new Vector2(84, 0) * scale
        }, sand);
        DrawCircle(center + new Vector2(-25, -17) * scale, 19 * scale, new Color("#68B980"));
        DrawCircle(center + new Vector2(15, -18) * scale, 27 * scale, new Color("#5EAC74"));
    }

    private void DrawPalm(Vector2 root, float height, bool mirror)
    {
        var dir = mirror ? -1f : 1f;
        Vector2 crown = root + new Vector2(dir * height * 0.22f, -height);
        var trunk = new Color("#B78050");
        var shadow = new Color("#8F634A");
        DrawLine(root, crown, trunk, Mathf.Max(12f, height * 0.055f), true);
        DrawLine(root + new Vector2(-4 * dir, 0), crown + new Vector2(-4 * dir, 1),
            shadow, 3, true);
        for (int i = 0; i < 7; i++)
        {
            var a = -Mathf.Pi + i * (Mathf.Pi / 6f);
            var tip = crown + new Vector2(Mathf.Cos(a) * height * 0.31f,
                                          Mathf.Sin(a) * height * 0.20f + height * 0.06f);
            var middle = crown + (tip - crown) * 0.57f + new Vector2(0, -22);
            DrawLine(crown, middle, new Color("#398E69"), 13, true);
            DrawLine(middle, tip, i % 2 == 0 ? new Color("#61B96A") : new Color("#42A779"), 7, true);
        }
        for (int i = 0; i < 3; i++)
            DrawCircle(crown + new Vector2((i - 1) * 12, 13), 8, new Color("#AD784F"));
    }
}
