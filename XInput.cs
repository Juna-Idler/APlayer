using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APlayer
{
    public static partial class XInput
    {
        [Flags]
        public enum Buttons : ushort
        {
            UP = 0x0001,
            DOWN = 0x0002,
            LEFT = 0x0004,
            RIGHT = 0x0008,
            START = 0x0010,
            BACK = 0x0020,
            THUMB_LEFT = 0x0040,
            THUMB_RIGHT = 0x0080,
            SHOULDER_LEFT = 0x0100,
            SHOULDER_RIGHT = 0x0200,
            A = 0x1000,
            B = 0x2000,
            X = 0x4000,
            Y = 0x8000,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct GAMEPAD
        {
            public Buttons wButtons;    // 押されているボタンの情報
            public byte bLeftTrigger;  // 左トリガーの入力情報
            public byte bRightTrigger; // 右トリガーの入力情報
            public short sThumbLX;     // 左スティックのX軸(横)の入力情報
            public short sThumbLY;     // 左スティックのY軸(縦)の入力情報
            public short sThumbRX;     // 右スティックのX軸(横)の入力情報
            public short sThumbRY;     // 右スティックのY軸(縦)の入力情報
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct STATE
        {
            public uint dwPacketNumber;    // パケット番号
            public GAMEPAD Gamepad; // 入力情報
        }

        [LibraryImport("Xinput1_4.dll")]
        public static partial uint XInputGetState(uint dwUserIndex, ref STATE pState);




        public class EventGenerator
        {
            public event EventHandler<(Buttons pressed,Buttons released, AnalogButtons a_pressed, AnalogButtons a_released)>? ButtonsChanged;

            [Flags]
            public enum AnalogButtons : ushort
            {
                TriggerLeft = 1,
                TriggerRight = 2,
                LeftStickLeft = 4,
                LeftStickRight = 8,
                LeftStickUp = 16,
                LeftStickDown = 32,
                RightStickLeft = 64,
                RightStickRight = 128,
                RightStickUp = 256,
                RightStickDown = 512,
            }

            public byte TriggerButtonThreshold { get; set; } = 128;
            public short StickButtonThreshold { get; set; } = 0x4000;


            private readonly System.Timers.Timer timer;

            public STATE State;
            public STATE LastState { get; private set; }
            private AnalogButtons last_analog_button_state = 0;

            public void CopyLastStateFrom(EventGenerator from)
            {
                LastState = from.LastState;
                last_analog_button_state = from.last_analog_button_state;
            }

            public EventGenerator(uint dwUserIndex,TimeSpan interval)
            {
                UserIndex = dwUserIndex;
                timer = new System.Timers.Timer(interval.TotalMilliseconds);

                timer.Elapsed += Timer_Elapsed;
            }

            private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
            {
                XInputGetState(UserIndex, ref State);

                AnalogButtons analog_button_changed = 0;
                AnalogButtons a_pressed = 0;
                AnalogButtons a_released = 0;
                if ((LastState.Gamepad.bLeftTrigger != State.Gamepad.bLeftTrigger) ||
                    (LastState.Gamepad.bRightTrigger != State.Gamepad.bRightTrigger) ||
                    (LastState.Gamepad.sThumbLY != State.Gamepad.sThumbLY) ||
                    (LastState.Gamepad.sThumbLX != State.Gamepad.sThumbLX) ||
                    (LastState.Gamepad.sThumbRY != State.Gamepad.sThumbRY) ||
                    (LastState.Gamepad.sThumbRX != State.Gamepad.sThumbRX))
                    {
                    bool left_trigger = State.Gamepad.bLeftTrigger >= TriggerButtonThreshold;
                    bool right_trigger = State.Gamepad.bRightTrigger >= TriggerButtonThreshold;
                    bool left_left = State.Gamepad.sThumbLX < -StickButtonThreshold;
                    bool left_right = State.Gamepad.sThumbLX >= StickButtonThreshold;
                    bool left_up = State.Gamepad.sThumbLY >= StickButtonThreshold;
                    bool left_down = State.Gamepad.sThumbLY < -StickButtonThreshold;
                    bool right_left = State.Gamepad.sThumbRX < -StickButtonThreshold;
                    bool right_right = State.Gamepad.sThumbRX >= StickButtonThreshold;
                    bool right_up = State.Gamepad.sThumbRY >= StickButtonThreshold;
                    bool right_down = State.Gamepad.sThumbRY < -StickButtonThreshold;

                    AnalogButtons analog_button_state =
                        (left_trigger ? AnalogButtons.TriggerLeft : 0) | (right_trigger ? AnalogButtons.TriggerRight : 0) |
                        (left_left ? AnalogButtons.LeftStickLeft : 0) | (left_right ? AnalogButtons.LeftStickRight : 0) |
                        (left_up ? AnalogButtons.LeftStickUp : 0) | (left_down ? AnalogButtons.LeftStickDown : 0) |
                        (right_left ? AnalogButtons.RightStickLeft : 0) | (right_right ? AnalogButtons.RightStickRight : 0) |
                        (right_up ? AnalogButtons.RightStickUp : 0) | (right_down ? AnalogButtons.RightStickDown : 0);

                    analog_button_changed = analog_button_state ^ last_analog_button_state;
                    a_pressed = analog_button_changed & analog_button_state;
                    a_released = analog_button_changed & last_analog_button_state;
                    last_analog_button_state = analog_button_state;
                }

                Buttons button_changed = (State.Gamepad.wButtons ^ LastState.Gamepad.wButtons);

                if (button_changed != 0 || analog_button_changed != 0)
                {
                    Buttons pressed = button_changed & State.Gamepad.wButtons;
                    Buttons rereased = button_changed & LastState.Gamepad.wButtons;
                    ButtonsChanged?.Invoke(this, (pressed, rereased,a_pressed,a_released));
                }
                LastState = State;
            }

            public bool IsPolling { get => timer.Enabled; }

            public void Start() {timer.Start(); }

            public void Stop() { timer.Stop(); }

            public TimeSpan Interval {
                get => TimeSpan.FromMilliseconds(timer.Interval);
                set { timer.Interval = value.TotalMilliseconds;
                }
            }

            public uint UserIndex { get; set; } = 0;

        }


    }
}
