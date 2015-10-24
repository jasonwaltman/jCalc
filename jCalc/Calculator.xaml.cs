using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Calculator
{
    /// <summary>
    /// Extension Methods 
    /// </summary>
    public static class MyExtensions
    {
        /// <summary>
        /// Allows use of Description attribute in Enum for printing
        /// </summary>
        /// <param name="value">Enum constant</param>
        /// <returns>Display value of enum</returns>
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Enum for calculator operations
    /// Description attr is displayed in ticker, assumed to be one character
    /// </summary>
    public enum Operation {
        [Description(" ")] NONE,
        [Description("+")] PLUS,
        [Description("-")] MINUS,
        [Description("×")] MULT,
        [Description("÷")] DIV
    }

    /// <summary>
    /// Keeps calculator display, tracker, and pending operation status; performs operations
    /// </summary>
    public class CalculatorRuntime
    {
        private string displayEntry;
        private bool   displayEntryHasDecimal;
        private string displayTicker;

        private double currentResult;
        private Operation pendingOperation;
        private bool pendingOperationLocked;

        public CalculatorRuntime()
        {
            Clear();
        }

        /// <summary>
        /// Initialize/reset calculator
        /// </summary>
        public void Clear()
        {
            displayEntry = "0";
            displayEntryHasDecimal = false;
            displayTicker = "";
            currentResult = 0;
            pendingOperation = Operation.NONE;
            pendingOperationLocked = false;
        }

        /// <summary>
        /// Adds one character to displayEntry
        /// </summary>
        /// <param name="c">Character to add</param>
        public void AddToEntry(char c)
        {
            // if no decimal already OR not trying to add a decimal:
            if (!displayEntryHasDecimal || c != '.')
            {
                if (c == '.')
                {
                    displayEntryHasDecimal = true;
                }
                else if (displayEntry == "0")
                {
                    // if just a 0 in the display, replace with new char
                    displayEntry = "";
                }
                displayEntry += c;
                pendingOperationLocked = true;
            }
            // (bypass above if there is a decimal already and trying to add another)
        }

        /// <summary>
        /// Replace entire displayEntry with new string
        /// </summary>
        /// <param name="str">String to replace</param>
        public void ReplaceEntry(string str)
        {
            displayEntry = str;
            if (displayEntry.Contains("."))
            {
                displayEntryHasDecimal = true;
            }
            else
            {
                displayEntryHasDecimal = false;
            }
            pendingOperationLocked = true;
        }

        /// <summary>
        /// Deletes one character from displayEntry
        /// </summary>
        public void DeleteFromEntry()
        {
            // Allow digit to be deleted only if 
            // pending operation is locked AND a value other than 0 is in the entry
            if (pendingOperationLocked && !IsEntryClear())
            {
                var length = displayEntry.Length;

                if (length > 0) // should always be true!
                {
                    if (displayEntry[length - 1] == '.')
                    {
                        displayEntryHasDecimal = false;
                    }
                    displayEntry = displayEntry.Remove(length - 1);
                }
                if (displayEntry.Length == 0)
                {
                    displayEntry = "0";
                }
            }
        }

        /// <summary>
        /// Negate the current displayEntry if possible
        /// </summary>
        public void NegateEntry()
        {
            // Allow +/- change only if 
            // pending operation is locked AND a value other than 0 is in the entry
            if (pendingOperationLocked && !IsEntryClear())
            { 
                if (displayEntry[0] == '-')
                {
                    // If displayEntry is negative, 
                    // remove negative sign to make positive
                    displayEntry = displayEntry.Substring(1);
                }
                else
                {
                    // Else displayEntry is positive,
                    // add negative sign to make negative
                    displayEntry = "-" + displayEntry;
                }
            }
        }

        /// <summary>
        /// Reset displayEntry back to 0
        /// </summary>
        public void ClearEntry()
        {
            displayEntry = "0";
            displayEntryHasDecimal = false;
        }

        /// <summary>
        /// Whether or not displayEntry has been reset
        /// </summary>
        /// <returns>True if displayEntry has been reset</returns>
        public bool IsEntryClear()
        {
            return displayEntry == "0";
        }

        /// <summary>
        /// Return displayEntry for display
        /// </summary>
        /// <returns>Current calulator display</returns>
        public string GetEntry()
        {
            return displayEntry;
        }

        /// <summary>
        /// Add string to displayTicker
        /// </summary>
        /// <param name="str">String to add to the end of the current ticker</param>
        private void AddToTicker(string str)
        {
            displayTicker += str;

            var length = displayTicker.Length;
            var maxLength = 20;
            if (length > maxLength)
            {
                // Keep displayTicker short if it gets too long with ellipsis up front.
                // Could also keep the full ticker and only return the shortened
                // version, but there's not a need to do that right now.
                displayTicker = "..." + displayTicker.Substring(length - maxLength);
            }
        }

        /// <summary>
        /// Update displayTicker with different math operation at the tail
        /// </summary>
        /// <param name="c">Character that represents new math operation</param>
        private void ReplaceTickerOperation(char c)
        {
            var length = displayTicker.Length;

            if (length > 0) // should always be true!
            {
                displayTicker = displayTicker.Remove(length - 1) + c;
            }
        }

        /// <summary>
        /// Reset displayTicker
        /// </summary>
        private void ClearTicker()
        {
            displayTicker = "";
        }

        /// <summary>
        /// Return displayTicker for display
        /// </summary>
        /// <returns>Current calculator ticker display</returns>
        public string GetTicker()
        {
            return displayTicker;
        }

        /// <summary>
        /// Apply current pending operation, update displayEntry with results, 
        /// add new pending operation
        /// </summary>
        /// <param name="op">New pending operation</param>
        public void ApplyOperation(Operation op)
        {
            // Apply only if operation is set AND a value other than 0 is in the entry
            if (op != Operation.NONE && !IsEntryClear())
            {
                if (pendingOperationLocked)
                {
                    // Pending operation is locked: must apply pending and set new operation
                    AddToTicker(displayEntry + op.GetDescription()[0]);
                    Process(Convert.ToDouble(displayEntry));
                    ReplaceEntry(currentResult.ToString());
                    pendingOperation = op;
                }
                else
                {
                    // Pending operation can be changed: do not apply current pending operation
                    ReplaceTickerOperation(op.GetDescription()[0]);
                    pendingOperation = op;      
                }
                pendingOperationLocked = false;
            }
        }

        /// <summary>
        /// Apply current pending operation and update displayEntry with results
        /// </summary>
        public void FlushOperation()
        {
            ClearTicker();

            if (pendingOperationLocked)
            {
                // Pending operation is locked, apply pending and display new result
                Process(Convert.ToDouble(displayEntry));
                ReplaceEntry(currentResult.ToString());
            }

            currentResult = 0;
            pendingOperation = Operation.NONE;
            pendingOperationLocked = true;
        }

        /// <summary>
        /// Apply pending operation with num and update currentResult
        /// </summary>
        /// <param name="num">Value to apply to current pending Operation</param>
        private void Process(double num)
        {
            switch (pendingOperation)
            {
                case Operation.NONE:  { currentResult  = num; break; }
                case Operation.PLUS:  { currentResult += num; break; }
                case Operation.MINUS: { currentResult -= num; break; }
                case Operation.MULT:  { currentResult *= num; break; }
                case Operation.DIV:   { currentResult /= num; break; }
            }
        }

    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CalculatorRuntime cRun;

        // true if display should be cleared on next number entry
        private bool resetDisplay;

        private bool memStore;   // true if in memory store mode
        private bool mem1Active; // true if value stored in mem1
        private bool mem2Active; // true if value stored in mem2

        System.Windows.Media.Brush MemoryInactiveBrush;
        System.Windows.Media.Brush MemoryHighlightBrush;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(WindowLoaded);

            cRun = new CalculatorRuntime();
            resetDisplay = true;

            memStore = false;
            mem1Active = false;
            mem2Active = false;
            MemoryInactiveBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
            MemoryHighlightBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xBE, 0xE6, 0xFD));
        }

        private void NumberClick(object sender, ExecutedRoutedEventArgs e)
        {
            // e.Parameter contains string representing digit clicked or typed

            if (resetDisplay)
            {
                cRun.ClearEntry();
                btnClear.Content = "C";
            }

            var number = e.Parameter.ToString()[0];
            cRun.AddToEntry(number);
            display.Text = cRun.GetEntry();
            resetDisplay = false;
        }

        private void ClearClick(object sender, ExecutedRoutedEventArgs e)
        {
            if (btnClear.Content.ToString() == "C")
            {
                // Just clear the current entry
                btnClear.Content = "AC";
                cRun.ClearEntry();
                display.Text = cRun.GetEntry();
            }
            else
            {
                // Clear everything
                cRun.Clear();
                display.Text = cRun.GetEntry();
                ticker.Text = cRun.GetTicker();
            }
            resetDisplay = true;
        }

        private void MathClick(object sender, ExecutedRoutedEventArgs e)
        {
            // e.Parameter contains string representing operation clicked or typed
            // should be one of: "+", "-", "*", "/"

            Operation op;
            switch (e.Parameter.ToString())
            {
                case "+": op = Operation.PLUS;  break;
                case "-": op = Operation.MINUS; break;
                case "*": op = Operation.MULT;  break;
                case "/": op = Operation.DIV;   break;
                default : op = Operation.NONE;  break;
            }

            cRun.ApplyOperation(op);
            display.Text = cRun.GetEntry();
            ticker.Text = cRun.GetTicker();
            resetDisplay = true;
        }

        private void EqualsClick(object sender, ExecutedRoutedEventArgs e)
        {
            cRun.FlushOperation();
            display.Text = cRun.GetEntry();
            ticker.Text = cRun.GetTicker();
            resetDisplay = true;
        }

        private void NegateClick(object sender, ExecutedRoutedEventArgs e)
        {
            cRun.NegateEntry();
            display.Text = cRun.GetEntry();
        }

        private void BackspaceClick(object sender, ExecutedRoutedEventArgs e)
        {
            cRun.DeleteFromEntry();
            display.Text = cRun.GetEntry();
        }

        private void MemoryStoreClick(object sender, ExecutedRoutedEventArgs e)
        {
            // swap memStore state, then color memory buttons accordingly
            memStore = !memStore;

            if (memStore)
            {
                btnMem1.Background = MemoryHighlightBrush;
                btnMem2.Background = MemoryHighlightBrush;
            }
            else
            {
                btnMem1.Background = MemoryInactiveBrush;
                btnMem2.Background = MemoryInactiveBrush;
            }
        }

        private void MemoryRecallClick(object sender, ExecutedRoutedEventArgs e)
        {
            if (memStore)
            {
                // Store display value into memory
                switch (e.Parameter.ToString())
                {
                    case "1": btnMem1.Content = cRun.GetEntry(); mem1Active = true; break;
                    case "2": btnMem2.Content = cRun.GetEntry(); mem2Active = true; break;
                    default: break;
                }
                // After storing a value, turn off memory store mode
                memStore = false;
                btnMem1.Background = MemoryInactiveBrush;
                btnMem2.Background = MemoryInactiveBrush;
            }
            else
            {
                // Retrieve value from memory and put into display
                switch (e.Parameter.ToString())
                {
                    case "1": if (mem1Active) { cRun.ReplaceEntry(btnMem1.Content.ToString()); } break;
                    case "2": if (mem2Active) { cRun.ReplaceEntry(btnMem2.Content.ToString()); } break;
                    default: break;
                }
                resetDisplay = false;
                display.Text = cRun.GetEntry();
            }
        }

        private void MemoryStoreDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // On double-click, reset memory storage
            btnMem1.Content = "Mem 1";
            btnMem2.Content = "Mem 2";
            mem1Active = false;
            mem2Active = false;

            // The single-click handler will be called after this.
            // Setting memStore to true here, so it'll be set to false in the single-click handler.
            memStore = true; 
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        
        // System Menu Extension: Add About to system menu

        // P/Invoke Methods (Calling C/C++ functions from a .NET program)
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

        // System menu constants
        private const Int32 WM_SYSCOMMAND = 0x112;
        private const Int32 MF_SEPARATOR = 0x800;
        private const Int32 MF_BYPOSITION = 0x400;

        // Define constant for new menu item
        private const Int32 AboutSysMenuID = 1000;

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Get handles for this Window and the System Menu
            var windowHandle = new WindowInteropHelper(this).Handle;
            var systemMenuHandle = GetSystemMenu(windowHandle, false);

            // Create our new System Menu items just before the Close menu item
            InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // Menu seperator
            InsertMenu(systemMenuHandle, 6, MF_BYPOSITION, AboutSysMenuID, "About...");

            // Attach our WndProc handler to this Window
            HwndSource source = HwndSource.FromHwnd(windowHandle);
            source.AddHook(new HwndSourceHook(WndProc));

        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Check if a System Command has been executed
            if (msg == WM_SYSCOMMAND)
            {
                // Execute the appropriate code for the System Menu item that was clicked
                switch (wParam.ToInt32())
                {
                    case AboutSysMenuID:
                        var about = new About();
                        about.Show();
                        about.Owner = Application.Current.MainWindow; // close this window when main window closes
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }
    }
}
