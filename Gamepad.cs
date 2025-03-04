using System;
using System.Collections.Generic;
using System.Linq;

namespace APlayer
{
    public class Gamepad
    {
        public XInput.STATE GetState()
        {
            XInput.STATE state = new();
            XInput.XInputGetState(Generator.UserIndex, ref state);
            return state;
        }


        private XInput.EventGenerator Generator { get; set; }


        public Gamepad(uint user_index, TimeSpan interval)
        {
            Generator = new XInput.EventGenerator(user_index, interval);
            Generator.ButtonsChanged += Generator_ButtonsChanged;
            Generator.Start();
        }

        public uint UserIndex { get => Generator.UserIndex; set => Generator.UserIndex = value; }

        public TimeSpan Interval { get => Generator.Interval; set => Generator.Interval = value; }

        public Assign NormalAssign { get; private set; } = NullAssign;
        public Assign ShiftedAssign { get; private set; } = NullAssign;


        public void SetAssign(Assign normal,Assign shifted)
        {
            NormalAssign = normal;
            ShiftedAssign = shifted;
            ShiftCount = 0;
        }
        public void SetAssign( Assign normal)
        {
            if (normal.ShiftButtons.Length > 0)
                throw new InvalidOperationException();
            NormalAssign = normal;
            ShiftedAssign = NullAssign;
            ShiftCount = 0;
        }
        public void ResetAssign()
        {
            NormalAssign = NullAssign;
            ShiftedAssign = NullAssign;
            ShiftCount = 0;
        }


        private static readonly Assign NullAssign = new(typeof(void));
        private int ShiftCount = 0;



        private void Generator_ButtonsChanged(object? sender, (XInput.EventGenerator.Buttons pressed, XInput.EventGenerator.Buttons released) e)
        {
            foreach (var button in NormalAssign.ShiftButtons)
            {
                var b = (XInput.EventGenerator.Buttons)button;
                if (e.pressed.HasFlag(b))
                    ShiftCount++;
                if (e.released.HasFlag(b))
                {
                    ShiftCount--;
                    if (ShiftCount < 0)
                        ShiftCount = 0;
                }
            }
            Assign assign = (ShiftCount > 0) ? ShiftedAssign : NormalAssign;

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.UP))
                assign.Up.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.DOWN))
                assign.Down.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.LEFT))
                assign.Left.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.RIGHT))
                assign.Right.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.START))
                assign.Start.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.BACK))
                assign.Back.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.THUMB_LEFT))
                assign.ThumbLeft.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.THUMB_RIGHT))
                assign.ThumbRight.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.SHOULDER_LEFT))
                assign.ShoulderLeft.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.SHOULDER_RIGHT))
                assign.ShoulderRight.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.A))
                assign.A.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.B))
                assign.B.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.X))
                assign.X.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.Y))
                assign.Y.Invoke();


            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.TriggerLeft))
                assign.TriggerLeft.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.TriggerRight))
                assign.TriggerRight.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.LeftStickLeft))
                assign.LeftStickLeft.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.LeftStickRight))
                assign.LeftStickRight.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.LeftStickUp))
                assign.LeftStickUp.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.LeftStickDown))
                assign.LeftStickDown.Invoke();

            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.RightStickLeft))
                assign.RightStickLeft.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.RightStickRight))
                assign.RightStickRight.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.RightStickUp))
                assign.RightStickUp.Invoke();
            if (e.pressed.HasFlag(XInput.EventGenerator.Buttons.RightStickDown))
                assign.RightStickDown.Invoke();

        }

        public class Assign(Type source)
        {
            public static void NoAction() { }
            public static void Shift() { }

            public Type SourceDataType { get; private set; } = source;

            public XInput.EventGenerator.Buttons[] ShiftButtons = [];

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


        public class AssignData<T> where T : struct, Enum
        {
            public Type DataType {  get { return typeof(T); } }

            public delegate Action GetAction(T act);

            public Assign CreateAssign(GetAction getter)
            {
                return new AssignCreator(getter).Create(this);
            }


            public T Up { get; set; }
            public T Down { get; set; }
            public T Left { get; set; }
            public T Right { get; set; }

            public T Start { get; set; }
            public T Back { get; set; }

            public T ThumbLeft { get; set; }
            public T ThumbRight { get; set; }

            public T ShoulderLeft { get; set; }
            public T ShoulderRight { get; set; }

            public T A { get; set; }
            public T B { get; set; }
            public T X { get; set; }
            public T Y { get; set; }

            public T TriggerLeft { get; set; }
            public T TriggerRight { get; set; }

            public T LeftStickLeft { get; set; }
            public T LeftStickRight { get; set; }
            public T LeftStickUp { get; set; }
            public T LeftStickDown { get; set; }

            public T RightStickLeft { get; set; }
            public T RightStickRight { get; set; }
            public T RightStickUp { get; set; }
            public T RightStickDown { get; set; }

            class AssignCreator(GetAction getter)
            {
                private readonly GetAction getter = getter;
                private readonly List<XInput.EventGenerator.Buttons> shift_buttons = [];

                private Action Assort(XInput.EventGenerator.Buttons button, T act)
                {
                    Action action = getter.Invoke(act);
                    if (action == Assign.Shift)
                    {
                        shift_buttons.Add(button);
                        return Assign.Shift;
                    }
                    return action;
                }
                public Assign Create(AssignData<T> data)
                {
                    return new Assign(typeof(T))
                    {
                        Up = Assort(XInput.EventGenerator.Buttons.UP, data.Up),
                        Down = Assort(XInput.EventGenerator.Buttons.DOWN, data.Down),
                        Left = Assort(XInput.EventGenerator.Buttons.LEFT, data.Left),
                        Right = Assort(XInput.EventGenerator.Buttons.RIGHT, data.Right),

                        Start = Assort(XInput.EventGenerator.Buttons.START, data.Start),
                        Back = Assort(XInput.EventGenerator.Buttons.BACK, data.Back),

                        ThumbLeft = Assort(XInput.EventGenerator.Buttons.THUMB_LEFT, data.ThumbLeft),
                        ThumbRight = Assort(XInput.EventGenerator.Buttons.THUMB_RIGHT, data.ThumbRight),
                        ShoulderLeft = Assort(XInput.EventGenerator.Buttons.SHOULDER_LEFT, data.ShoulderLeft),
                        ShoulderRight = Assort(XInput.EventGenerator.Buttons.SHOULDER_RIGHT, data.ShoulderRight),

                        A = Assort(XInput.EventGenerator.Buttons.A, data.A),
                        B = Assort(XInput.EventGenerator.Buttons.B, data.B),
                        X = Assort(XInput.EventGenerator.Buttons.X, data.X),
                        Y = Assort(XInput.EventGenerator.Buttons.Y, data.Y),

                        TriggerLeft = Assort(XInput.EventGenerator.Buttons.TriggerLeft, data.TriggerLeft),
                        TriggerRight = Assort(XInput.EventGenerator.Buttons.TriggerRight, data.TriggerRight),

                        LeftStickLeft = Assort(XInput.EventGenerator.Buttons.LeftStickLeft, data.LeftStickLeft),
                        LeftStickRight = Assort(XInput.EventGenerator.Buttons.LeftStickRight, data.LeftStickRight),
                        LeftStickUp = Assort(XInput.EventGenerator.Buttons.LeftStickUp, data.LeftStickUp),
                        LeftStickDown = Assort(XInput.EventGenerator.Buttons.LeftStickDown, data.LeftStickDown),

                        RightStickLeft = Assort(XInput.EventGenerator.Buttons.RightStickLeft, data.RightStickLeft),
                        RightStickRight = Assort(XInput.EventGenerator.Buttons.RightStickRight, data.RightStickRight),
                        RightStickUp = Assort(XInput.EventGenerator.Buttons.RightStickUp, data.RightStickUp),
                        RightStickDown = Assort(XInput.EventGenerator.Buttons.RightStickDown, data.RightStickDown),

                        ShiftButtons = [.. shift_buttons]
                    };
                }
            }
        }
    }
}
