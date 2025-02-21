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
            public event EventHandler<(Buttons pressed,Buttons released)>? ButtonsChanged;

            [Flags]
            public enum TriggerButtons
            {
                Left = 1,
                Right = 2,
            }

            public event EventHandler<(TriggerButtons pressed, TriggerButtons released)>? TriggerButtonsChanged;

            public byte TriggerButtonThreshold { get; set; } = 128;

            private readonly System.Timers.Timer timer;

            public STATE State;
            public STATE LastState;

            public EventGenerator(uint dwUserIndex,TimeSpan interval)
            {
                UserIndex = dwUserIndex;
                timer = new System.Timers.Timer(interval.TotalMilliseconds);
                TriggerButtons last_trigger_button_state = 0;
                timer.Elapsed += (sender, e) =>
                {
                    XInputGetState(UserIndex, ref State);

                    Buttons button_changed = (State.Gamepad.wButtons ^ LastState.Gamepad.wButtons);
                    if (button_changed != 0)
                    {
                        Buttons pressed = button_changed & State.Gamepad.wButtons;
                        Buttons rereased = button_changed & LastState.Gamepad.wButtons;
                        ButtonsChanged?.Invoke(this, (pressed,rereased));
                    }
                    if ((LastState.Gamepad.bLeftTrigger != State.Gamepad.bLeftTrigger) ||
                        (LastState.Gamepad.bRightTrigger != State.Gamepad.bRightTrigger))
                    {
                        bool left_on = State.Gamepad.bLeftTrigger > TriggerButtonThreshold;
                        bool right_on = State.Gamepad.bRightTrigger > TriggerButtonThreshold;
                        TriggerButtons trigger_button_state = (left_on ? TriggerButtons.Left : 0) | (right_on ? TriggerButtons.Right : 0);
                        TriggerButtons trigger_changed = trigger_button_state ^ last_trigger_button_state;
                        if (trigger_changed != 0)
                        {
                            TriggerButtonsChanged?.Invoke(this, (
                                trigger_changed & trigger_button_state,
                                trigger_changed & last_trigger_button_state));
                        }
                    }

                    LastState = State;
                };

            }

            public void Start() {timer.Start();}

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
