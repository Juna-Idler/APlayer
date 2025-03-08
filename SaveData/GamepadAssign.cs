using APlayer.StartPage;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using static APlayer.SaveData.GamepadAssign;
using static APlayer.StartPage.SavedData.Group;

namespace APlayer.SaveData
{
    public static class GamepadAssign
    {
        public enum StartPageGamepadAction
        {
            NoAction,
            NextFolder,
            PrevFolder,
            NextTab,
            PrevTab,
            Select,
        }

        public enum MainPageGamepadAction
        {
            NoAction,
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

        public class SaveData
        {
            public Gamepad.AssignData<StartPageGamepadAction> StartPage { get; set; } = StartPageDefault;
            public Gamepad.AssignData<MainPageGamepadAction> MainPage { get; set; } = MainPageDefault;
            public Gamepad.AssignData<MainPageGamepadAction> MainPageShift { get; set; } = MainPageDefaultShift;
        }

        public static async Task<SaveData> Load(StorageFolder folder, string file_name = "gamepad_assign.json")
        {
            try
            {
                var file = await folder.GetFileAsync(file_name);
                var json = await FileIO.ReadTextAsync(file);
                var data = JsonSerializer.Deserialize(json, GamepadAssignContext.Default.SaveData);
                if (data != null)
                    return data;
            }
            catch (Exception)
            {
            }
            return new SaveData();
        }
        public static async Task<bool> Save(SaveData data, StorageFolder folder, string file_name = "gamepad_assign.json")
        {
            try
            {
                var json = JsonSerializer.Serialize(data, GamepadAssignContext.Default.SaveData);

                var item = await folder.TryGetItemAsync(file_name);
                if (item == null)
                {
                    var f = await folder.CreateFileAsync(file_name);
                    await FileIO.WriteTextAsync(f, json);
                    return true;
                }
                if (item is StorageFile file)
                {
                    var file_json = await FileIO.ReadTextAsync(file);
                    if (json == file_json)
                    {
                        return true;
                    }
                    await FileIO.WriteTextAsync(file, json);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
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

//    [JsonSerializable(typeof(Gamepad.AssignData<StartPageGamepadAction>))]
//    internal partial class StartPageGamepadContext : JsonSerializerContext { }


//    [JsonSerializable(typeof(Gamepad.AssignData<MainPageGamepadAction>))]
//    internal partial class MainPageGamepadContext : JsonSerializerContext { }


    [JsonSerializable(typeof(GamepadAssign.SaveData))]
    internal partial class GamepadAssignContext : JsonSerializerContext { }
}
