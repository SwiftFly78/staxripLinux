# [Documentation](../../README.md) / [Usage](../README.md) / [User Interface](README.md) / Events

`Tools > Advanced > Event Commands...` (`Ctrl+E`) opens the list of event commands. An event command runs a
[command](../Commands.md) when something happens, for example after a job is processed. See [Events](../Events.md) for
the events and examples.

<img width=600 src="../../../Images/Screenshots/EventCommands.png">

The checkbox of an event command enables or disables it. `Add...` creates a new one, `Edit...` changes the selected
one, `Clone` copies it, `Remove` removes it, `Up` and `Down` change the order in which event commands of the same
event run.

`Variables...` defines variables, one per line like `name = value`. Event commands and other macros use them as
`%var:name%`, PowerShell scripts as `$Variables['name']`, so a value that several event commands use, like a folder,
is changed in one place.

The event commands are stored in the settings folder (`Events.dat`). When another instance of StaxRipLinux
changes them, the next event reloads them, so all instances run the same event commands.

## Event Command editor

<img width=600 src="../../../Images/Screenshots/EventCommandEditor.png">

- **Name**: the name in the list.
- **Event**: when the command runs.
- **Criteria**: optional conditions, the command only runs when they're met. `Match All` requires all conditions,
  `Match Any` one of them. Each condition compares a [macro](../Macros.md) with a value, for example
  `%source_height%` *Is Higher Than* `1080`, or `%target_file%` *Contains* `.mkv`.
- **Command** and **Parameters**: the command and its parameters, like in the [Menu Editor](Menu-Editor.md).
  The parameters may contain [interactive macros](../Macros.md#interactive-macros) like `$enter_text:prompt$`, which
  ask for the value when the event command runs. During job processing they aren't asked, so jobs don't wait.
