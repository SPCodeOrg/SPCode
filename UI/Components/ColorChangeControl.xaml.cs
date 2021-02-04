using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SPCode.UI.Components
{
    /// <summary>
    /// Interaction logic for ColorChangeControl.xaml
    /// </summary>
    public partial class ColorChangeControl : UserControl
    {
        public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
        "ColorChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorChangeControl));

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler(ColorChangedEvent, value); }
            remove { RemoveHandler(ColorChangedEvent, value); }
        }

        private bool RaiseEventAllowed = false;
        public ColorChangeControl()
        {
            InitializeComponent();
        }

        public void SetContent(string SHName, Color c)
        {
            ColorName.Text = SHName;
            UpdateColor(c);
        }

        public Color GetColor()
        {
            return Color.FromArgb(0xFF, (byte)(int)RSlider.Value, (byte)(int)GSlider.Value, (byte)(int)BSlider.Value);
        }

        private void SliderValue_Changed(object sender, RoutedEventArgs e)
        {
            if (!RaiseEventAllowed) { return; }
            var c = Color.FromArgb(0xFF, (byte)(int)RSlider.Value, (byte)(int)GSlider.Value, (byte)(int)BSlider.Value);
            UpdateColor(c, true, false);
            var raiseEvent = new RoutedEventArgs(ColorChangeControl.ColorChangedEvent);
            RaiseEvent(raiseEvent);
        }

        private void UpdateColor(Color c, bool UpdateTextBox = true, bool UpdateSlider = true)
        {
            RaiseEventAllowed = false;
            BrushRect.Background = new SolidColorBrush(c);
            var colorChannelMean = (c.R + c.G + c.B) / 3.0;
            BrushRect.Foreground = new SolidColorBrush((colorChannelMean > 128.0) ? Colors.Black : Colors.White);
            if (UpdateTextBox)
            {
                BrushRect.Text = ((c.R << 16) | (c.G << 8) | (c.B)).ToString("X").PadLeft(6, '0');
            }
            if (UpdateSlider)
            {
                RSlider.Value = c.R;
                GSlider.Value = c.G;
                BSlider.Value = c.B;
            }
            var raiseEvent = new RoutedEventArgs(ColorChangeControl.ColorChangedEvent);
            RaiseEvent(raiseEvent);
            RaiseEventAllowed = true;
        }

        private void BrushRect_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!RaiseEventAllowed) { return; }
            var cVal = 0;
            var parseString = BrushRect.Text.Trim();
            if (parseString.StartsWith("0x", System.StringComparison.InvariantCultureIgnoreCase) && parseString.Length > 2)
            {
                parseString = parseString.Substring(2);
            }
            if (int.TryParse(parseString, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var result))
            { cVal = result; }
            UpdateColor(Color.FromArgb(0xFF, (byte)((cVal >> 16) & 0xFF), (byte)((cVal >> 8) & 0xFF), (byte)(cVal & 0xFF)), false, true);
        }
    }
}
