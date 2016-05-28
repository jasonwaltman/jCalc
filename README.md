# jCalc

jCalc is a small Windows Presentation Foundation (WPF) Visual Studio (2015) project written in C# which implements a standard 4-function calculator.  The UI was inspired by the iOS 7 standard calculator app.

## Features

* Entry accessible by keyboard or mouse
* Backspace key will erase last entered digit
* Math operation can be overwritten by choosing new operation before entering new digits
* Two visual memory store banks allows results to be stored and easily recalled
* Recent history is shown at top of main display
* jCalc stays on top of other windows when it loses focus

Shortcuts           | Action
------------------- | -------------
`Esc` or `C`        | Clear / All Clear
`Backspace`         | Delete last entered digit
`N` or `!`          | Negate
`M`                 | Toggle memory store mode
`F1`                | Recall/save contents of Mem 1
`F2`                | Recall/save contents of Mem 2
Double-click MS     | Reset both memory stores
`+` or `A`          | Add
`-` or `S`          | Subtract
`*` or `X`          | Multiply
`/` or `D`          | Divide
`Enter` or `Return` | Equals


When in memory store mode, the memory store buttons are highlighted.  Clicking one of the memory stores (or pressing the corresponding  function key) will save the currently displayed value into memory.  When not in memory store mode (i.e., memory store buttons are not highlighted) clicking a store button or pressing the corresponding function key will copy the stored value into the display entry.

## For developers

I learned a few techniques which weren't immediately obvious to make jCalc more polished and easy-to-use.  The application illustrates how to implement the following features in C#/WPF:

* Bind a keyboard shortcut to a button-click event
* Bind separate single- vs double-click events to a button
* Add an `About...` item to the system menu
* Make the main window the owner of a new window (new windows closes when main window closes)
* Use the description attribute of an `Enum` for display


