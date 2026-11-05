# Merge Harvest

A free, offline merge-and-serve game for Windows, made with Godot 4.4.1 .NET and C#.

- Unlimited item generation and **unlimited energy**.
- **No in-game purchases, ads, or dialogue/story sequences**.
- Merge matching food items, complete orders, earn coins and level up.
- Automatic local saves.

## Run in Godot

1. Install [Godot 4.4.1 .NET](https://godotengine.org/download/archive/4.4.1-stable/) and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Open the directory containing `project.godot` in Godot .NET.
3. Build the C# project and press **F6/F5** to play.

## Controls

- Click an item and then an empty square to move it, or a matching item to merge it.
- You can also drag an item onto another square.
- Use the three generator buttons as often as you like. Energy never runs out.
- Serve orders once their requested items are on the board.
- **Space** spawns a fruit item. **Esc** clears selection.
- Use **Recycle selected** to free a square, and **Organize board** to group items.

Progress is stored locally in Godot's `user://merge_harvest_save.json`.

The game uses original programmatically drawn icons and no artwork from existing commercial games.
