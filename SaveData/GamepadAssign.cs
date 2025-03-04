using APlayer.StartPage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static APlayer.SaveData.GamepadAssign;

namespace APlayer.SaveData
{
    public static class GamepadAssign
    {
        public enum StartPageGamepadAction
        {
            None,
            NextFolder,
            PrevFolder,
            NextTab,
            PrevTab,
            Select,
        }

        public enum MainPageGamepadAction
        {
            None,
            Shift,

            GainUp,
            GainDown,
            Forward,
            Backward,
            PlayPause,
            NextTrack,
            PreviousTrack,

            Up,
            Down,
            Left,
            Right,
            Select,
        }

        public static readonly Gamepad.AssignData<StartPageGamepadAction> StartPageDefault = new()
        {
            Up = StartPageGamepadAction.PrevFolder,
            Down = StartPageGamepadAction.NextFolder,
            Left = StartPageGamepadAction.PrevTab,
            Right = StartPageGamepadAction.NextTab,
            ShoulderLeft = StartPageGamepadAction.Select,
            A = StartPageGamepadAction.Select,
        };

        public static readonly Gamepad.AssignData<MainPageGamepadAction> MainPageDefault = new()
        {
            TriggerLeft = MainPageGamepadAction.Shift,

            Up = MainPageGamepadAction.Up,
            Down = MainPageGamepadAction.Down,
            Left = MainPageGamepadAction.Left,
            Right = MainPageGamepadAction.Right,
            ShoulderLeft = MainPageGamepadAction.Select,
            A = MainPageGamepadAction.Select,

        };
        public static readonly Gamepad.AssignData<MainPageGamepadAction> MainPageDefaultShift = new()
        {
            TriggerLeft = MainPageGamepadAction.Shift,

            Up = MainPageGamepadAction.GainUp,
            Down = MainPageGamepadAction.GainDown,
            Left = MainPageGamepadAction.Backward,
            Right = MainPageGamepadAction.Forward,
            ShoulderLeft = MainPageGamepadAction.PlayPause,
            A = MainPageGamepadAction.PlayPause,
        };

    }

    [JsonSerializable(typeof(Gamepad.AssignData<StartPageGamepadAction>))]
    internal partial class StartPageGamepadContext : JsonSerializerContext { }


    [JsonSerializable(typeof(Gamepad.AssignData<MainPageGamepadAction>))]
    internal partial class MainPageGamepadContext : JsonSerializerContext { }

}
