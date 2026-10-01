using System.Windows;
using System.Windows.Input;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.UI.Controls
{
    /// <summary>
    /// A custom control that captures keyboard input for hotkey configuration.
    /// Displays the current key combination and provides visual feedback during capture.
    /// </summary>
    public class HotkeyCaptureBox : System.Windows.Controls.Control
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="VirtualKeyCode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VirtualKeyCodeProperty =
            DependencyProperty.Register(
                nameof(VirtualKeyCode),
                typeof(int),
                typeof(HotkeyCaptureBox),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindingPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Modifiers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register(
                nameof(Modifiers),
                typeof(HotkeyModifier),
                typeof(HotkeyCaptureBox),
                new FrameworkPropertyMetadata(HotkeyModifier.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindingPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DisplayText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(
                nameof(DisplayText),
                typeof(string),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata("Press a key combination..."));

        /// <summary>
        /// Identifies the <see cref="IsCapturing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCapturingProperty =
            DependencyProperty.Register(
                nameof(IsCapturing),
                typeof(bool),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsValid"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(
                nameof(IsValid),
                typeof(bool),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="ValidationMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.Register(
                nameof(ValidationMessage),
                typeof(string),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata("Press a key combination...", OnBindingPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="RequireModifier"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RequireModifierProperty =
            DependencyProperty.Register(
                nameof(RequireModifier),
                typeof(bool),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="CapturingBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CapturingBackgroundProperty =
            DependencyProperty.Register(
                nameof(CapturingBackground),
                typeof(System.Windows.Media.Brush),
                typeof(HotkeyCaptureBox),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 255, 224))));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the virtual key code for the captured key.
        /// </summary>
        public int VirtualKeyCode
        {
            get => (int)GetValue(VirtualKeyCodeProperty);
            set => SetValue(VirtualKeyCodeProperty, value);
        }

        /// <summary>
        /// Gets or sets the modifier keys for the captured hotkey.
        /// </summary>
        public HotkeyModifier Modifiers
        {
            get => (HotkeyModifier)GetValue(ModifiersProperty);
            set => SetValue(ModifiersProperty, value);
        }

        /// <summary>
        /// Gets the display text showing the current key combination.
        /// </summary>
        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            private set => SetValue(DisplayTextProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the control is currently capturing input.
        /// </summary>
        public bool IsCapturing
        {
            get => (bool)GetValue(IsCapturingProperty);
            private set => SetValue(IsCapturingProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the current key combination is valid.
        /// </summary>
        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            private set => SetValue(IsValidProperty, value);
        }

        /// <summary>
        /// Gets any validation message for the current combination.
        /// </summary>
        public string ValidationMessage
        {
            get => (string)GetValue(ValidationMessageProperty);
            private set => SetValue(ValidationMessageProperty, value);
        }

        /// <summary>
        /// Gets or sets the placeholder text shown when no key is captured.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Gets or sets whether a modifier key is required for valid hotkeys.
        /// </summary>
        public bool RequireModifier
        {
            get => (bool)GetValue(RequireModifierProperty);
            set => SetValue(RequireModifierProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush when capturing input.
        /// </summary>
        public System.Windows.Media.Brush CapturingBackground
        {
            get => (System.Windows.Media.Brush)GetValue(CapturingBackgroundProperty);
            set => SetValue(CapturingBackgroundProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a new hotkey combination is captured.
        /// </summary>
        public event EventHandler<HotkeyBinding>? HotkeyCaptured;

        /// <summary>
        /// Occurs when the hotkey is cleared.
        /// </summary>
        public event EventHandler? HotkeyCleared;

        #endregion

        #region Fields

        private HotkeyModifier _currentModifiers;
        private bool _hasNonModifierKey;
        private System.Windows.Controls.Button? _clearButton;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the <see cref="HotkeyCaptureBox"/> class.
        /// </summary>
        static HotkeyCaptureBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(HotkeyCaptureBox),
                new FrameworkPropertyMetadata(typeof(HotkeyCaptureBox)));

            FocusableProperty.OverrideMetadata(
                typeof(HotkeyCaptureBox),
                new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyCaptureBox"/> class.
        /// </summary>
        public HotkeyCaptureBox()
        {
            UpdateDisplayText();
        }

        #endregion

        #region Overrides

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            if (_clearButton != null) _clearButton.Click -= ClearButton_Click;
            base.OnApplyTemplate();
            _clearButton = GetTemplateChild("PART_ClearButton") as System.Windows.Controls.Button;
            if (_clearButton != null) _clearButton.Click += ClearButton_Click;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            StartCapturing();
        }

        /// <inheritdoc/>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            StopCapturing();
        }

        /// <inheritdoc/>
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (!IsCapturing)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                Clear();
                return;
            }

            if (e.Key == Key.Tab)
            {
                e.Handled = false;
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            UpdateModifiers();

            if (IsModifierKey(key))
            {
                UpdateDisplayText();
                return;
            }

            int vkCode = KeyInterop.VirtualKeyFromKey(key);
            if (vkCode > 0)
            {
                SetCurrentValue(VirtualKeyCodeProperty, vkCode);
                SetCurrentValue(ModifiersProperty, _currentModifiers);
                _hasNonModifierKey = true;

                ValidateHotkey();
                UpdateDisplayText();

                HotkeyCaptured?.Invoke(this, new HotkeyBinding(vkCode, _currentModifiers));
            }
        }

        /// <inheritdoc/>
        protected override void OnPreviewKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            if (!IsCapturing)
            {
                base.OnPreviewKeyUp(e);
                return;
            }

            e.Handled = true;

            UpdateModifiers();

            if (!_hasNonModifierKey)
            {
                UpdateDisplayText();
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                // Prevent the containing ScrollViewer from taking focus as the click bubbles.
                e.Handled = Focus();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears the captured hotkey and resets the control.
        /// </summary>
        public void Clear()
        {
            SetCurrentValue(VirtualKeyCodeProperty, 0);
            SetCurrentValue(ModifiersProperty, HotkeyModifier.None);
            _currentModifiers = HotkeyModifier.None;
            _hasNonModifierKey = false;
            IsValid = false;
            ValidationMessage = string.Empty;
            UpdateDisplayText();

            HotkeyCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the hotkey binding programmatically.
        /// </summary>
        /// <param name="binding">The hotkey binding to set.</param>
        public void SetBinding(HotkeyBinding binding)
        {
            SetCurrentValue(VirtualKeyCodeProperty, binding.VirtualKeyCode);
            SetCurrentValue(ModifiersProperty, binding.Modifiers);
            _hasNonModifierKey = binding.VirtualKeyCode > 0;
            ValidateHotkey();
            UpdateDisplayText();
        }

        /// <summary>
        /// Gets the current hotkey binding.
        /// </summary>
        /// <returns>The current <see cref="HotkeyBinding"/>.</returns>
        public HotkeyBinding GetBinding()
        {
            return new HotkeyBinding(VirtualKeyCode, Modifiers);
        }

        #endregion

        #region Private Methods

        private void StartCapturing()
        {
            IsCapturing = true;
            _currentModifiers = HotkeyModifier.None;
            UpdateDisplayText();
        }

        private void StopCapturing()
        {
            IsCapturing = false;
            _currentModifiers = HotkeyModifier.None;
            UpdateDisplayText();
        }

        private void UpdateModifiers()
        {
            _currentModifiers = HotkeyModifier.None;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                _currentModifiers |= HotkeyModifier.Ctrl;
            }

            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                _currentModifiers |= HotkeyModifier.Alt;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                _currentModifiers |= HotkeyModifier.Shift;
            }

            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            {
                _currentModifiers |= HotkeyModifier.Win;
            }
        }

        private void UpdateDisplayText()
        {
            if (VirtualKeyCode > 0)
            {
                DisplayText = FormatHotkey(VirtualKeyCode, Modifiers);
            }
            else if (IsCapturing && _currentModifiers != HotkeyModifier.None)
            {
                DisplayText = FormatModifiers(_currentModifiers) + "...";
            }
            else if (IsCapturing)
            {
                DisplayText = PlaceholderText;
            }
            else
            {
                DisplayText = VirtualKeyCode > 0 ? FormatHotkey(VirtualKeyCode, Modifiers) : PlaceholderText;
            }
        }

        private void ValidateHotkey()
        {
            if (VirtualKeyCode == 0)
            {
                IsValid = false;
                ValidationMessage = "No key captured";
                return;
            }

            if (RequireModifier && Modifiers == HotkeyModifier.None)
            {
                IsValid = false;
                ValidationMessage = "At least one modifier key (Ctrl, Alt, Shift, Win) is required";
                return;
            }

            if (IsReservedKey(VirtualKeyCode, Modifiers))
            {
                IsValid = false;
                ValidationMessage = "This key combination is reserved by the system";
                return;
            }

            IsValid = true;
            ValidationMessage = string.Empty;
        }

        private static bool IsModifierKey(Key key)
        {
            return key is Key.LeftCtrl or Key.RightCtrl or
                         Key.LeftAlt or Key.RightAlt or
                         Key.LeftShift or Key.RightShift or
                         Key.LWin or Key.RWin;
        }

        private static bool IsReservedKey(int vkCode, HotkeyModifier modifiers)
        {
            if (vkCode == 0x2E && modifiers == (HotkeyModifier.Ctrl | HotkeyModifier.Alt))
            {
                return true;
            }

            if (vkCode == 0x09 && modifiers == HotkeyModifier.Alt)
            {
                return true;
            }

            return false;
        }

        private static string FormatHotkey(int vkCode, HotkeyModifier modifiers)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(HotkeyModifier.Ctrl)) parts.Add("Ctrl");
            if (modifiers.HasFlag(HotkeyModifier.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(HotkeyModifier.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(HotkeyModifier.Win)) parts.Add("Win");

            parts.Add(GetKeyName(vkCode));

            return string.Join("+", parts);
        }

        private static string FormatModifiers(HotkeyModifier modifiers)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(HotkeyModifier.Ctrl)) parts.Add("Ctrl");
            if (modifiers.HasFlag(HotkeyModifier.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(HotkeyModifier.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(HotkeyModifier.Win)) parts.Add("Win");

            return parts.Count > 0 ? string.Join("+", parts) : string.Empty;
        }

        private static string GetKeyName(int vkCode)
        {
            return vkCode switch
            {
                0 => string.Empty,
                >= 0x70 and <= 0x7B => $"F{vkCode - 0x70 + 1}",
                >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),
                >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),
                >= 0x60 and <= 0x69 => $"Num{vkCode - 0x60}",
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x1B => "Escape",
                0x20 => "Space",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2D => "Insert",
                0x2E => "Delete",
                0x6A => "Num*",
                0x6B => "Num+",
                0x6D => "Num-",
                0x6E => "Num.",
                0x6F => "Num/",
                0x90 => "NumLock",
                0x91 => "ScrollLock",
                0xBA => ";",
                0xBB => "=",
                0xBC => ",",
                0xBD => "-",
                0xBE => ".",
                0xBF => "/",
                0xC0 => "`",
                0xDB => "[",
                0xDC => "\\",
                0xDD => "]",
                0xDE => "'",
                _ => $"Key(0x{vkCode:X2})"
            };
        }

        private static void OnBindingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyCaptureBox captureBox)
            {
                captureBox._hasNonModifierKey = captureBox.VirtualKeyCode > 0;
                captureBox.ValidateHotkey();
                captureBox.UpdateDisplayText();
            }
        }

        #endregion
    }
}
