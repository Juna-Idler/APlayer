using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APlayer
{
    public class Gamepad
    {
        public XInput.STATE GetState()
        {
            XInput.STATE state = new();
            XInput.XInputGetState(Main.UserIndex, ref state);
            return state;
        }

        public XInput.EventGenerator Main { get; private set; }
        public XInput.EventGenerator Sub { get; private set; }


        public Gamepad(uint user_index,TimeSpan interval)
        {
            Main = new XInput.EventGenerator(user_index, interval);
            Sub = new XInput.EventGenerator(user_index, interval);
            Main.Start();
        }

        public void ChangeUserIndex(uint user_index)
        {
            Main.UserIndex = user_index;
            Sub.UserIndex = user_index;
        }

        public void SwitchToMain()
        {
            Sub.Stop();
            Main.CopyLastStateFrom(Sub);
            Main.Start();
        }
        public void SwitchToSub()
        {
            Main.Stop();
            Sub.CopyLastStateFrom(Main);
            Sub.Start();
        }

        public bool IsMainPolling => Main.IsPolling;
        public bool IsSubPolling => Sub.IsPolling;




        public Assign CurrentAssign {  get; private set; }
        public Assign ShiftedAssign { get; private set; }

        public ulong[] ShiftButtons = [];
        private int ShiftCount = 0;

        public void SetAssign(Assign assign)
        {
            CurrentAssign = assign;
            Main.ButtonsChanged += Main_ButtonsChanged;
        }

        private void Main_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons released, XInput.EventGenerator.AnalogButtons a_pressed, XInput.EventGenerator.AnalogButtons a_released) e)
        {
            foreach (var button in ShiftButtons)
            {
                if (button < 0x1000)
                {
                    var b = (XInput.Buttons)button;
                    if (e.pressed.HasFlag(b))
                        ShiftCount++;
                    if (e.released.HasFlag(b))
                        ShiftCount--;
                }
                else
                {
                    var b = (XInput.EventGenerator.AnalogButtons)(button >> 16);
                    if (e.a_pressed.HasFlag(b))
                        ShiftCount++;
                    if (e.a_released.HasFlag(b))
                        ShiftCount--;
                }
            }
            Assign assign = (ShiftCount > 0) ? ShiftedAssign : CurrentAssign;

            if (e.pressed.HasFlag(XInput.Buttons.UP))
                assign.Up.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                assign.Down.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                assign.Left.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                assign.Right.Invoke();

            if (e.pressed.HasFlag(XInput.Buttons.START))
                assign.Start.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.BACK))
                assign.Back.Invoke();

            if (e.pressed.HasFlag(XInput.Buttons.THUMB_LEFT))
                assign.ThumbLeft.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.THUMB_RIGHT))
                assign.ThumbRight.Invoke();

            if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                assign.ShoulderLeft.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_RIGHT))
                assign.ShoulderRight.Invoke();

            if (e.pressed.HasFlag(XInput.Buttons.A))
                assign.A.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.B))
                assign.B.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.X))
                assign.X.Invoke();
            if (e.pressed.HasFlag(XInput.Buttons.Y))
                assign.Y.Invoke();


            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.TriggerLeft))
                assign.TriggerLeft.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.TriggerRight))
                assign.TriggerRight.Invoke();

            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.LeftStickLeft))
                assign.LeftStickLeft.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.LeftStickRight))
                assign.LeftStickRight.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.LeftStickUp))
                assign.LeftStickUp.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.LeftStickDown))
                assign.LeftStickDown.Invoke();

            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.RightStickLeft))
                assign.RightStickLeft.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.RightStickRight))
                assign.RightStickRight.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.RightStickUp))
                assign.RightStickUp.Invoke();
            if (e.a_pressed.HasFlag(XInput.EventGenerator.AnalogButtons.RightStickDown))
                assign.RightStickDown.Invoke();

        }

        public class Assign
        {
            public static void NoAction() { }

            public Action Up { get; set; } = NoAction;
            public Action Down { get; set; } = NoAction;
            public Action Left { get; set; } = NoAction;
            public Action Right { get; set; } = NoAction;

            public Action Start { get; set; } = NoAction;
            public Action Back { get; set; } = NoAction;

            public Action ThumbLeft { get; set; } = NoAction;
            public Action ThumbRight { get; set; } = NoAction;

            public Action ShoulderLeft { get; set; } = NoAction;
            public Action ShoulderRight { get; set; } = NoAction;

            public Action A { get; set; } = NoAction;
            public Action B { get; set; } = NoAction;
            public Action X { get; set; } = NoAction;
            public Action Y { get; set; } = NoAction;

            public Action TriggerLeft { get; set; } = NoAction;
            public Action TriggerRight { get; set; } = NoAction;

            public Action LeftStickLeft { get; set; } = NoAction;
            public Action LeftStickRight { get; set; } = NoAction;
            public Action LeftStickUp { get; set; } = NoAction;
            public Action LeftStickDown { get; set; } = NoAction;

            public Action RightStickLeft { get; set; } = NoAction;
            public Action RightStickRight { get; set; } = NoAction;
            public Action RightStickUp { get; set; } = NoAction;
            public Action RightStickDown { get; set; } = NoAction;
        }

    }
}
