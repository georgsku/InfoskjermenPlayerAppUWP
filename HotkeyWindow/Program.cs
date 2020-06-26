using System;

namespace HotkeyWindow
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        public HotkeyAppContext()
        {
            int processId = (int)ApplicationData.Current.LocalSettings.Values["processId"];
            process = Process.GetProcessById(processId);
            process.EnableRaisingEvents = true;
            process.Exited += HotkeyAppContext_Exited;
            hotkeyWindow = new HotKeyWindow();
            hotkeyWindow.HotkeyPressed += new HotKeyWindow.HotkeyDelegate(hotkeys_HotkeyPressed);
            hotkeyWindow.RegisterCombo(1001, Modifiers.Alt, Keys.S); // Alt-S = stingray
            hotkeyWindow.RegisterCombo(1002, Modifiers.Alt, Keys.O); // Alt-O = octopus
        }
    }
}
