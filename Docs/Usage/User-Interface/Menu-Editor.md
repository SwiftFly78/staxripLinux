# [Documentation](../../README.md) / [Usage](../README.md) / [User Interface](README.md) / Menu Editor

Many menus of StaxRipLinux can be customized: their entries, the order, the shortcut keys and the commands they run.
`Tools > Edit Menu...` edits the main menu, `Edit Menu...` in the menus of the [Crop](Crop.md) window, the
[Preview](Preview.md), the [Code Editor](Code-Editor.md), the [Jobs](Jobs.md) window and the size presets of the
[Crop and Resize](Main.md#crop-and-resize) card edits these menus.

<img width=700 src="../../../Images/Screenshots/MenuEditor.png">

The tree on the left shows the menu, the selected entry is edited on the right:

- **Text**: the name of the entry. A `-` creates a separator.
- **Hotkey**: press the key combination that runs the entry, press it twice to remove it.
- **Command**: the command the entry runs, the button next to it lists all commands. The [Commands](../Commands.md)
  page describes them.
- **Parameters**: the parameters of the command, for example the command line of `ExecuteCommandLine`. Text parameters
  may contain [macros](../Macros.md).

The buttons at the top create a new entry, which is a copy of the selected one, cut, copy and paste entries and move
them. Moving left and right changes the level, so an entry becomes a submenu entry or a top level entry. `Tools` has
*New From Defaults...*, which adds an entry of the default menu, for example one that was removed, and
*Reset Everything*, which restores the default menu.

> [!TIP]
> To run your own tool from the main menu, add an entry with the command `ExecuteCommandLine` and a command line like
> `mediainfo-gui "%source_file%"`. With *Show Process Window* the output is shown in the processing window, otherwise
> it runs in the background.

Menus that are simple lists of values, like the size presets, DAR and PAR menus of the main window, are edited as text,
one entry per line in the form `Name = Value`, an empty line creates a separator. `Restore Defaults...` restores the
default entries.
