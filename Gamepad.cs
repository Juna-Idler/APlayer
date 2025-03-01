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

    }
}
