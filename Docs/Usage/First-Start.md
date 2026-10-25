# [Documentation](../README.md) / [Usage](README.md) / First Start

## Installation
StaxRipLinux is written so it can run completely on its own. Nevertheless, some conditions must be met.
Please make sure, that you have followed all steps mentioned in [Installation](../Introduction/Installation.md).

## Settings Directory
StaxRipLinux stores data like settings, templates, the job list and fonts in `~/.config/StaxRip`
(`$XDG_CONFIG_HOME/StaxRip`) without asking. A folder named `Settings` next to the `StaxRip` executable keeps the data
next to the installation instead, so different versions can run next to each other, and the environment variable
`STAXRIP_SETTINGS` overrides both, see [Linux](../Introduction/Linux.md).

## What's New
On the first start the *What's New* dialog appears. It lists all versions of the Linux port with their changes, newest
first, followed by the notes of the StaxRip release the port is based on. It appears again once after each update, and
*Help > What's new...* shows it any time:

<img width=400 src="../../Images/Screenshots/WhatsNew.png">

## Let's go
Finally you will see the main window, which appears directly the next time you launch StaxRipLinux:

<img width=600 src="../../Images/Screenshots/MainWindow.png">


> [!TIP]
> For a better understanding of what you see, what you can use and most important *how*,
I recommend reading about the [Workflow](Workflow.md) and [User Interface](User-Interface/README.md).