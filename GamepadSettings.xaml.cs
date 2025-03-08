using APlayer.SaveData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GamepadSettings : Page
    {
        private readonly ComboBox[] SP_Comboboxes;
        private readonly ComboBox[] MPP_Comboboxes;
        private readonly ComboBox[] MPS_Comboboxes;

        public void SetTabIndex(int index)
        {
            TabView.SelectedIndex = index;
        }

        public GamepadSettings()
        {
            this.InitializeComponent();
            SP_Comboboxes = [
                SP_LT, SP_RT, SP_LB, SP_RB,
                SP_D_Up, SP_D_Down, SP_D_Left, SP_D_Right,
                SP_A,SP_B,SP_X,SP_Y,
                SP_Back,SP_Start,
                SP_LS_Button,SP_LS_Up,SP_LS_Down,SP_LS_Left,SP_LS_Right,
                SP_RS_Button,SP_RS_Up,SP_RS_Down,SP_RS_Left,SP_RS_Right,
            ];
            MPP_Comboboxes = [
                MPP_LT, MPP_RT, MPP_LB, MPP_RB,
                MPP_D_Up, MPP_D_Down, MPP_D_Left, MPP_D_Right,
                MPP_A,MPP_B,MPP_X,MPP_Y,
                MPP_Back,MPP_Start,
                MPP_LS_Button,MPP_LS_Up,MPP_LS_Down,MPP_LS_Left,MPP_LS_Right,
                MPP_RS_Button,MPP_RS_Up,MPP_RS_Down,MPP_RS_Left,MPP_RS_Right,
            ];
            MPS_Comboboxes = [
                MPS_LT, MPS_RT, MPS_LB, MPS_RB,
                MPS_D_Up, MPS_D_Down, MPS_D_Left, MPS_D_Right,
                MPS_A,MPS_B,MPS_X,MPS_Y,
                MPS_Back,MPS_Start,
                MPS_LS_Button,MPS_LS_Up,MPS_LS_Down,MPS_LS_Left,MPS_LS_Right,
                MPS_RS_Button,MPS_RS_Up,MPS_RS_Down,MPS_RS_Left,MPS_RS_Right,
            ];
            var sp_actions = Enum.GetValues<GamepadAssign.StartPageGamepadAction>();

            foreach (var i in SP_Comboboxes)
            {
                i.ItemsSource = sp_actions;
                i.SelectedIndex = 0;
            }
            var mp_actions = Enum.GetValues<GamepadAssign.MainPageGamepadAction>();
            foreach (var i in MPP_Comboboxes)
            {
                i.ItemsSource = mp_actions;
            }
            foreach (var i in MPS_Comboboxes)
            {
                i.ItemsSource = mp_actions;
            }
            Load();
            foreach (var i in MPP_Comboboxes)
            {
                i.SelectionChanged += MP_SelectionChanged;
            }
            foreach (var i in MPS_Comboboxes)
            {
                i.SelectionChanged += MP_SelectionChanged;
            }
        }

        private void MP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb)
                return;
            if (e.AddedItems.Count > 0)
            {
                if ((GamepadAssign.MainPageGamepadAction)cb.SelectedIndex == GamepadAssign.MainPageGamepadAction.Shift)
                {
                    int index = Array.IndexOf(MPP_Comboboxes, cb);
                    if (index >= 0)
                    {
                        if ((GamepadAssign.MainPageGamepadAction)MPS_Comboboxes[index].SelectedIndex != GamepadAssign.MainPageGamepadAction.Shift)
                            MPS_Comboboxes[index].SelectedIndex = (int)GamepadAssign.MainPageGamepadAction.Shift;
                        return;
                    }
                    index = Array.IndexOf(MPS_Comboboxes, cb);
                    if (index >= 0)
                    {
                        if ((GamepadAssign.MainPageGamepadAction)MPP_Comboboxes[index].SelectedIndex != GamepadAssign.MainPageGamepadAction.Shift)
                            MPP_Comboboxes[index].SelectedIndex = (int)GamepadAssign.MainPageGamepadAction.Shift;
                    }
                    return;
                }
            }
            if (e.RemovedItems.Count > 0)
            {
                if ((GamepadAssign.MainPageGamepadAction)e.RemovedItems[0] == GamepadAssign.MainPageGamepadAction.Shift)
                {
                    int index = Array.IndexOf(MPP_Comboboxes, cb);
                    if (index >= 0)
                    {
                        if ((GamepadAssign.MainPageGamepadAction)MPS_Comboboxes[index].SelectedIndex == GamepadAssign.MainPageGamepadAction.Shift)
                            MPS_Comboboxes[index].SelectedIndex = (int)GamepadAssign.MainPageGamepadAction.NoAction;
                        return;
                    }
                    index = Array.IndexOf(MPS_Comboboxes, cb);
                    if (index >= 0)
                    {
                        if ((GamepadAssign.MainPageGamepadAction)MPP_Comboboxes[index].SelectedIndex == GamepadAssign.MainPageGamepadAction.Shift)
                            MPP_Comboboxes[index].SelectedIndex = (int)GamepadAssign.MainPageGamepadAction.NoAction;
                    }
                    return;
                }

            }
        }

        private void Load()
        {
            {
                var data = App.AssignData.StartPage;
                SP_LT.SelectedIndex = (int)data.TriggerLeft;
                SP_RT.SelectedIndex = (int)data.TriggerRight;
                SP_LB.SelectedIndex = (int)data.ShoulderLeft;
                SP_RB.SelectedIndex = (int)data.ShoulderRight;
                SP_D_Up.SelectedIndex = (int)data.Up;
                SP_D_Down.SelectedIndex = (int)data.Down;
                SP_D_Left.SelectedIndex = (int)data.Left;
                SP_D_Right.SelectedIndex = (int)data.Right;
                SP_A.SelectedIndex = (int)data.A;
                SP_B.SelectedIndex = (int)data.B;
                SP_X.SelectedIndex = (int)data.X;
                SP_Y.SelectedIndex = (int)data.Y;
                SP_Back.SelectedIndex = (int)data.Back;
                SP_Start.SelectedIndex = (int)data.Start;
                SP_LS_Button.SelectedIndex = (int)data.ThumbLeft;
                SP_LS_Up.SelectedIndex = (int)data.LeftStickUp;
                SP_LS_Down.SelectedIndex = (int)data.LeftStickDown;
                SP_LS_Left.SelectedIndex = (int)data.LeftStickLeft;
                SP_LS_Right.SelectedIndex = (int)data.LeftStickRight;
                SP_RS_Button.SelectedIndex = (int)data.ThumbRight;
                SP_RS_Up.SelectedIndex = (int)data.RightStickUp;
                SP_RS_Down.SelectedIndex = (int)data.RightStickDown;
                SP_RS_Left.SelectedIndex = (int)data.RightStickLeft;
                SP_RS_Right.SelectedIndex = (int)data.RightStickRight;
            }

            {
                var data = App.AssignData.MainPage;
                MPP_LT.SelectedIndex = (int)data.TriggerLeft;
                MPP_RT.SelectedIndex = (int)data.TriggerRight;
                MPP_LB.SelectedIndex = (int)data.ShoulderLeft;
                MPP_RB.SelectedIndex = (int)data.ShoulderRight;
                MPP_D_Up.SelectedIndex = (int)data.Up;
                MPP_D_Down.SelectedIndex = (int)data.Down;
                MPP_D_Left.SelectedIndex = (int)data.Left;
                MPP_D_Right.SelectedIndex = (int)data.Right;
                MPP_A.SelectedIndex = (int)data.A;
                MPP_B.SelectedIndex = (int)data.B;
                MPP_X.SelectedIndex = (int)data.X;
                MPP_Y.SelectedIndex = (int)data.Y;
                MPP_Back.SelectedIndex = (int)data.Back;
                MPP_Start.SelectedIndex = (int)data.Start;
                MPP_LS_Button.SelectedIndex = (int)data.ThumbLeft;
                MPP_LS_Up.SelectedIndex = (int)data.LeftStickUp;
                MPP_LS_Down.SelectedIndex = (int)data.LeftStickDown;
                MPP_LS_Left.SelectedIndex = (int)data.LeftStickLeft;
                MPP_LS_Right.SelectedIndex = (int)data.LeftStickRight;
                MPP_RS_Button.SelectedIndex = (int)data.ThumbRight;
                MPP_RS_Up.SelectedIndex = (int)data.RightStickUp;
                MPP_RS_Down.SelectedIndex = (int)data.RightStickDown;
                MPP_RS_Left.SelectedIndex = (int)data.RightStickLeft;
                MPP_RS_Right.SelectedIndex = (int)data.RightStickRight;
            }
            {
                var data = App.AssignData.MainPageShift;
                MPS_LT.SelectedIndex = (int)data.TriggerLeft;
                MPS_RT.SelectedIndex = (int)data.TriggerRight;
                MPS_LB.SelectedIndex = (int)data.ShoulderLeft;
                MPS_RB.SelectedIndex = (int)data.ShoulderRight;
                MPS_D_Up.SelectedIndex = (int)data.Up;
                MPS_D_Down.SelectedIndex = (int)data.Down;
                MPS_D_Left.SelectedIndex = (int)data.Left;
                MPS_D_Right.SelectedIndex = (int)data.Right;
                MPS_A.SelectedIndex = (int)data.A;
                MPS_B.SelectedIndex = (int)data.B;
                MPS_X.SelectedIndex = (int)data.X;
                MPS_Y.SelectedIndex = (int)data.Y;
                MPS_Back.SelectedIndex = (int)data.Back;
                MPS_Start.SelectedIndex = (int)data.Start;
                MPS_LS_Button.SelectedIndex = (int)data.ThumbLeft;
                MPS_LS_Up.SelectedIndex = (int)data.LeftStickUp;
                MPS_LS_Down.SelectedIndex = (int)data.LeftStickDown;
                MPS_LS_Left.SelectedIndex = (int)data.LeftStickLeft;
                MPS_LS_Right.SelectedIndex = (int)data.LeftStickRight;
                MPS_RS_Button.SelectedIndex = (int)data.ThumbRight;
                MPS_RS_Up.SelectedIndex = (int)data.RightStickUp;
                MPS_RS_Down.SelectedIndex = (int)data.RightStickDown;
                MPS_RS_Left.SelectedIndex = (int)data.RightStickLeft;
                MPS_RS_Right.SelectedIndex = (int)data.RightStickRight;
            }

        }

        public void Save(Type lastDataType)
        {
            var sp = new Gamepad.AssignData<GamepadAssign.StartPageGamepadAction>();
            {
                sp.TriggerLeft = (GamepadAssign.StartPageGamepadAction)SP_LT.SelectedIndex;
                sp.TriggerRight = (GamepadAssign.StartPageGamepadAction)SP_RT.SelectedIndex;
                sp.ShoulderLeft = (GamepadAssign.StartPageGamepadAction)SP_LB.SelectedIndex;
                sp.ShoulderRight = (GamepadAssign.StartPageGamepadAction)SP_RB.SelectedIndex;
                sp.Up = (GamepadAssign.StartPageGamepadAction)SP_D_Up.SelectedIndex;
                sp.Down = (GamepadAssign.StartPageGamepadAction)SP_D_Down.SelectedIndex;
                sp.Left = (GamepadAssign.StartPageGamepadAction)SP_D_Left.SelectedIndex;
                sp.Right = (GamepadAssign.StartPageGamepadAction)SP_D_Right.SelectedIndex;
                sp.A = (GamepadAssign.StartPageGamepadAction)SP_A.SelectedIndex;
                sp.B = (GamepadAssign.StartPageGamepadAction)SP_B.SelectedIndex;
                sp.X = (GamepadAssign.StartPageGamepadAction)SP_X.SelectedIndex;
                sp.Y = (GamepadAssign.StartPageGamepadAction)SP_Y.SelectedIndex;
                sp.Back = (GamepadAssign.StartPageGamepadAction)SP_Back.SelectedIndex;
                sp.Start = (GamepadAssign.StartPageGamepadAction)SP_Start.SelectedIndex;
                sp.ThumbLeft = (GamepadAssign.StartPageGamepadAction)SP_LS_Button.SelectedIndex;
                sp.LeftStickUp = (GamepadAssign.StartPageGamepadAction)SP_LS_Up.SelectedIndex;
                sp.LeftStickDown = (GamepadAssign.StartPageGamepadAction)SP_LS_Down.SelectedIndex;
                sp.LeftStickLeft = (GamepadAssign.StartPageGamepadAction)SP_LS_Left.SelectedIndex;
                sp.LeftStickRight = (GamepadAssign.StartPageGamepadAction)SP_LS_Right.SelectedIndex;
                sp.ThumbRight = (GamepadAssign.StartPageGamepadAction)SP_RS_Button.SelectedIndex;
                sp.RightStickUp = (GamepadAssign.StartPageGamepadAction)SP_RS_Up.SelectedIndex;
                sp.RightStickDown = (GamepadAssign.StartPageGamepadAction)SP_RS_Down.SelectedIndex;
                sp.RightStickLeft = (GamepadAssign.StartPageGamepadAction)SP_RS_Left.SelectedIndex;
                sp.RightStickRight = (GamepadAssign.StartPageGamepadAction)SP_RS_Right.SelectedIndex;
            }

            var mpp = new Gamepad.AssignData<GamepadAssign.MainPageGamepadAction>();
            {
                mpp.TriggerLeft = (GamepadAssign.MainPageGamepadAction)MPP_LT.SelectedIndex;
                mpp.TriggerRight = (GamepadAssign.MainPageGamepadAction)MPP_RT.SelectedIndex;
                mpp.ShoulderLeft = (GamepadAssign.MainPageGamepadAction)MPP_LB.SelectedIndex;
                mpp.ShoulderRight = (GamepadAssign.MainPageGamepadAction)MPP_RB.SelectedIndex;
                mpp.Up = (GamepadAssign.MainPageGamepadAction)MPP_D_Up.SelectedIndex;
                mpp.Down = (GamepadAssign.MainPageGamepadAction)MPP_D_Down.SelectedIndex;
                mpp.Left = (GamepadAssign.MainPageGamepadAction)MPP_D_Left.SelectedIndex;
                mpp.Right = (GamepadAssign.MainPageGamepadAction)MPP_D_Right.SelectedIndex;
                mpp.A = (GamepadAssign.MainPageGamepadAction)MPP_A.SelectedIndex;
                mpp.B = (GamepadAssign.MainPageGamepadAction)MPP_B.SelectedIndex;
                mpp.X = (GamepadAssign.MainPageGamepadAction)MPP_X.SelectedIndex;
                mpp.Y = (GamepadAssign.MainPageGamepadAction)MPP_Y.SelectedIndex;
                mpp.Back = (GamepadAssign.MainPageGamepadAction)MPP_Back.SelectedIndex;
                mpp.Start = (GamepadAssign.MainPageGamepadAction)MPP_Start.SelectedIndex;
                mpp.ThumbLeft = (GamepadAssign.MainPageGamepadAction)MPP_LS_Button.SelectedIndex;
                mpp.LeftStickUp = (GamepadAssign.MainPageGamepadAction)MPP_LS_Up.SelectedIndex;
                mpp.LeftStickDown = (GamepadAssign.MainPageGamepadAction)MPP_LS_Down.SelectedIndex;
                mpp.LeftStickLeft = (GamepadAssign.MainPageGamepadAction)MPP_LS_Left.SelectedIndex;
                mpp.LeftStickRight = (GamepadAssign.MainPageGamepadAction)MPP_LS_Right.SelectedIndex;
                mpp.ThumbRight = (GamepadAssign.MainPageGamepadAction)MPP_RS_Button.SelectedIndex;
                mpp.RightStickUp = (GamepadAssign.MainPageGamepadAction)MPP_RS_Up.SelectedIndex;
                mpp.RightStickDown = (GamepadAssign.MainPageGamepadAction)MPP_RS_Down.SelectedIndex;
                mpp.RightStickLeft = (GamepadAssign.MainPageGamepadAction)MPP_RS_Left.SelectedIndex;
                mpp.RightStickRight = (GamepadAssign.MainPageGamepadAction)MPP_RS_Right.SelectedIndex;
            }
            var mps = new Gamepad.AssignData<GamepadAssign.MainPageGamepadAction>();
            {
                mps.TriggerLeft = (GamepadAssign.MainPageGamepadAction)MPS_LT.SelectedIndex;
                mps.TriggerRight = (GamepadAssign.MainPageGamepadAction)MPS_RT.SelectedIndex;
                mps.ShoulderLeft = (GamepadAssign.MainPageGamepadAction)MPS_LB.SelectedIndex;
                mps.ShoulderRight = (GamepadAssign.MainPageGamepadAction)MPS_RB.SelectedIndex;
                mps.Up = (GamepadAssign.MainPageGamepadAction)MPS_D_Up.SelectedIndex;
                mps.Down = (GamepadAssign.MainPageGamepadAction)MPS_D_Down.SelectedIndex;
                mps.Left = (GamepadAssign.MainPageGamepadAction)MPS_D_Left.SelectedIndex;
                mps.Right = (GamepadAssign.MainPageGamepadAction)MPS_D_Right.SelectedIndex;
                mps.A = (GamepadAssign.MainPageGamepadAction)MPS_A.SelectedIndex;
                mps.B = (GamepadAssign.MainPageGamepadAction)MPS_B.SelectedIndex;
                mps.X = (GamepadAssign.MainPageGamepadAction)MPS_X.SelectedIndex;
                mps.Y = (GamepadAssign.MainPageGamepadAction)MPS_Y.SelectedIndex;
                mps.Back = (GamepadAssign.MainPageGamepadAction)MPS_Back.SelectedIndex;
                mps.Start = (GamepadAssign.MainPageGamepadAction)MPS_Start.SelectedIndex;
                mps.ThumbLeft = (GamepadAssign.MainPageGamepadAction)MPS_LS_Button.SelectedIndex;
                mps.LeftStickUp = (GamepadAssign.MainPageGamepadAction)MPS_LS_Up.SelectedIndex;
                mps.LeftStickDown = (GamepadAssign.MainPageGamepadAction)MPS_LS_Down.SelectedIndex;
                mps.LeftStickLeft = (GamepadAssign.MainPageGamepadAction)MPS_LS_Left.SelectedIndex;
                mps.LeftStickRight = (GamepadAssign.MainPageGamepadAction)MPS_LS_Right.SelectedIndex;
                mps.ThumbRight = (GamepadAssign.MainPageGamepadAction)MPS_RS_Button.SelectedIndex;
                mps.RightStickUp = (GamepadAssign.MainPageGamepadAction)MPS_RS_Up.SelectedIndex;
                mps.RightStickDown = (GamepadAssign.MainPageGamepadAction)MPS_RS_Down.SelectedIndex;
                mps.RightStickLeft = (GamepadAssign.MainPageGamepadAction)MPS_RS_Left.SelectedIndex;
                mps.RightStickRight = (GamepadAssign.MainPageGamepadAction)MPS_RS_Right.SelectedIndex;
            }

            App.SetAssignData(new GamepadAssign.SaveData() { StartPage = sp, MainPage = mpp, MainPageShift = mps },lastDataType);
        }

    }
}
