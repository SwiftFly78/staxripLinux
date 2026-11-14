# [Documentation](../../README.md) / [Usage](../README.md) / [User Interface](README.md) / Video Comparison

`File > Video Comparison` compares videos frame by frame, for example the source and an encode, or encodes with
different settings. Every video gets a tab, all tabs show the same frame, so switching the tabs shows the differences.
When the window opens, it adds the source of the project and its target file, if it exists already, otherwise it asks
for the files.

<img width=700 src="../../../Images/Screenshots/VideoComparison.png">

The videos are opened with a source filter of the current [frame server](../Frame-Servers.md), images with an image
source. The line below the slider shows the position, the time and the size of the current tab, the menu button next
to it and a right-click on the video open the menu.

| Entry | Key | Description |
|---|---|---|
| Add files to compare... | `O` | Opens videos, several can be selected at once |
| Close selected tab | `Delete` | Closes the current tab |
| Select next tab | `Space` | Switches to the next tab |
| Save PNGs at current position | `S` | Saves the current frame of every tab as PNG next to its video |
| Frame Info | `I` | Shows the name, frame number, time and picture type on the videos and on saved PNGs |
| Split View | `V` | Shows the selected video left and the next video right of a line, which can be dragged |
| Zoom 100% | `Z` | Shows a screen pixel for each video pixel, dragging moves the videos, in the split view with the middle mouse button |
| Crop... | `C` | Crops the selected video or, with *Crop all videos*, all videos the same way |
| Go To Frame..., Go To Time... | `F`, `T` | Jumps to a frame or time |
| Synchronize by Time | | Switching the tab keeps the time instead of the frame number, for videos with different frame rates |
| Aspect Ratio... | `A` | Display aspect ratio of the selected video, like `16:9`, empty uses its sample aspect ratio |
| Navigate | `Left`, `Right`, `Ctrl+Left`, `Ctrl+Right` | 1 or 100 frames back and forward |
| Reload all tabs | `R` | Reloads the videos, for example after a new encode |
| Help | `F1` | Shows the help with all keys |

Videos with a different frame count don't line up, cut or encode them the same way before comparing. Videos with a
different frame rate line up with *Synchronize by Time*. Anamorphic videos are shown with their display aspect ratio,
which comes from the sample aspect ratio of the frames.

> [!TIP]
> When x265 writes a CSV log with frame level statistics (*Log Level Frame* and a CSV file on the statistics page of the
> x265 options), the video comparison shows the frame type and other information of each frame.
