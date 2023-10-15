/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using UnityEngine;

namespace GONet.Sample
{
    [RequireComponent(typeof(GONetLocal))]
    public partial class GONetSampleInputSync : GONetParticipantCompanionBehaviour
    {
        private const string GONET_SAMPLE_INPUT_BLEND = "_GONet_Sample_Input_Blend";
        private const string GONET_SAMPLE_INPUT_IMMEDIATE = "_GONet_Sample_Input_Immediate";

        /// <summary>
        /// Set this to true if some other code will manually set the values for the input sync variables herein instead of the input values being set "automatically" via monitoring inputs in <see cref="MonitorInputChanges"/>.
        /// </summary>
        public bool IsBeingControlledViaManualOverride;

        #region input sync variables

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_W { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_A { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_S { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_D { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_UpArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_DownArrow { get; set; }

        #region unused stuff left in for reference
        /* Comment all this out since we are not using it...just putting it here for convenience if you need it.
         * 
         * WARNING: Code generation will not currently support more than 256 sync items per script!  Uncommenting all this will result in code generation hell!
         * 
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_A { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_B { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_C { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_D { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_E { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_G { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_H { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_I { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_J { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_K { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_L { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_M { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_N { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_O { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_P { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Q { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_R { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_S { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_T { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_U { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_V { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_W { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_X { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Y { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Z { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Alpha9 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Keypad9 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetMouseButton_0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetMouseButton_1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetMouseButton_2 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Mouse6 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_BLEND)] public Vector2 mousePosition { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_AltGr { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Ampersand { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Asterisk { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_At { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_BackQuote { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Backslash { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Backspace { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Break { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_CapsLock { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Caret { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Clear { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Colon { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Comma { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Delete { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Dollar { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_DoubleQuote { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_End { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Equals { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Escape { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Exclaim { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Greater { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Hash { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Help { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Home { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Insert { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadDivide { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadEnter { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadEquals { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadMinus { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadMultiply { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadPeriod { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_KeypadPlus { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftAlt { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftApple { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftBracket { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftCommand { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftControl { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftCurlyBracket { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftParen { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftShift { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftWindows { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Less { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Menu { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Minus { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_None { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Numlock { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_PageDown { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_PageUp { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Pause { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Percent { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Period { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Pipe { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Plus { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Print { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Question { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Quote { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Return { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightAlt { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightApple { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightBracket { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightCommand { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightControl { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightCurlyBracket { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightParen { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightShift { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightWindows { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_ScrollLock { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Semicolon { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Slash { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Space { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_SysReq { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Tab { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Tilde { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Underscore { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_LeftArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_RightArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_UpArrow { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_DownArrow { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_F12 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_JoystickButton19 { get; set; }

        / * There are far less used:
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick1Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick2Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick3Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick4Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick5Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick6Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick7Button19 { get; set; }

        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button0 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button1 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button2 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button3 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button4 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button5 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button6 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button7 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button8 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button9 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button10 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button11 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button12 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button13 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button14 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button15 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button16 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button17 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button18 { get; set; }
        [GONetAutoMagicalSync(GONET_SAMPLE_INPUT_IMMEDIATE)] public bool GetKey_Joystick8Button19 { get; set; }
        * /
        *
        */

        #endregion

        #endregion

        private void Update()
        {
            if (gonetParticipant.IsMine && !IsBeingControlledViaManualOverride)
            {
                MonitorInputChanges();
            }
        }

        private void MonitorInputChanges()
        {
            //GetKey_W = Input.GetKey(KeyCode.W);
            //GetKey_A = Input.GetKey(KeyCode.A);
            //GetKey_S = Input.GetKey(KeyCode.S);
            //GetKey_D = Input.GetKey(KeyCode.D);

            //GetKey_LeftArrow = Input.GetKey(KeyCode.LeftArrow);
            //GetKey_RightArrow = Input.GetKey(KeyCode.RightArrow);
            //GetKey_UpArrow = Input.GetKey(KeyCode.UpArrow);
            //GetKey_DownArrow = Input.GetKey(KeyCode.DownArrow);

            #region unused stuff left in for reference
            /* Comment all this out since we are not using it...just putting it here for convenience if you need it.
                GetKey_A = Input.GetKey(KeyCode.A);
                GetKey_B = Input.GetKey(KeyCode.B);
                GetKey_C = Input.GetKey(KeyCode.C);
                GetKey_D = Input.GetKey(KeyCode.D);
                GetKey_E = Input.GetKey(KeyCode.E);
                GetKey_F = Input.GetKey(KeyCode.F);
                GetKey_G = Input.GetKey(KeyCode.G);
                GetKey_H = Input.GetKey(KeyCode.H);
                GetKey_I = Input.GetKey(KeyCode.I);
                GetKey_J = Input.GetKey(KeyCode.J);
                GetKey_K = Input.GetKey(KeyCode.K);
                GetKey_L = Input.GetKey(KeyCode.L);
                GetKey_M = Input.GetKey(KeyCode.M);
                GetKey_N = Input.GetKey(KeyCode.N);
                GetKey_O = Input.GetKey(KeyCode.O);
                GetKey_P = Input.GetKey(KeyCode.P);
                GetKey_Q = Input.GetKey(KeyCode.Q);
                GetKey_R = Input.GetKey(KeyCode.R);
                GetKey_S = Input.GetKey(KeyCode.S);
                GetKey_T = Input.GetKey(KeyCode.T);
                GetKey_U = Input.GetKey(KeyCode.U);
                GetKey_V = Input.GetKey(KeyCode.V);
                GetKey_W = Input.GetKey(KeyCode.W);
                GetKey_X = Input.GetKey(KeyCode.X);
                GetKey_Y = Input.GetKey(KeyCode.Y);
                GetKey_Z = Input.GetKey(KeyCode.Z);

                GetKey_Alpha0 = Input.GetKey(KeyCode.Alpha0);
                GetKey_Alpha1 = Input.GetKey(KeyCode.Alpha1);
                GetKey_Alpha2 = Input.GetKey(KeyCode.Alpha2);
                GetKey_Alpha3 = Input.GetKey(KeyCode.Alpha3);
                GetKey_Alpha4 = Input.GetKey(KeyCode.Alpha4);
                GetKey_Alpha5 = Input.GetKey(KeyCode.Alpha5);
                GetKey_Alpha6 = Input.GetKey(KeyCode.Alpha6);
                GetKey_Alpha7 = Input.GetKey(KeyCode.Alpha7);
                GetKey_Alpha8 = Input.GetKey(KeyCode.Alpha8);
                GetKey_Alpha9 = Input.GetKey(KeyCode.Alpha9);

                GetKey_Keypad0 = Input.GetKey(KeyCode.Keypad0);
                GetKey_Keypad1 = Input.GetKey(KeyCode.Keypad1);
                GetKey_Keypad2 = Input.GetKey(KeyCode.Keypad2);
                GetKey_Keypad3 = Input.GetKey(KeyCode.Keypad3);
                GetKey_Keypad4 = Input.GetKey(KeyCode.Keypad4);
                GetKey_Keypad5 = Input.GetKey(KeyCode.Keypad5);
                GetKey_Keypad6 = Input.GetKey(KeyCode.Keypad6);
                GetKey_Keypad7 = Input.GetKey(KeyCode.Keypad7);
                GetKey_Keypad8 = Input.GetKey(KeyCode.Keypad8);
                GetKey_Keypad9 = Input.GetKey(KeyCode.Keypad9);

                GetMouseButton_0 = Input.GetMouseButton(0); // left
                GetMouseButton_1 = Input.GetMouseButton(1); // right
                GetMouseButton_2 = Input.GetMouseButton(2); // middle

                GetKey_Mouse0 = Input.GetKey(KeyCode.Mouse0);
                GetKey_Mouse1 = Input.GetKey(KeyCode.Mouse1);
                GetKey_Mouse2 = Input.GetKey(KeyCode.Mouse2);
                GetKey_Mouse3 = Input.GetKey(KeyCode.Mouse3);
                GetKey_Mouse4 = Input.GetKey(KeyCode.Mouse4);
                GetKey_Mouse5 = Input.GetKey(KeyCode.Mouse5);
                GetKey_Mouse6 = Input.GetKey(KeyCode.Mouse6);

                mousePosition = Input.mousePosition;

                GetKey_AltGr = Input.GetKey(KeyCode.AltGr);
                GetKey_Ampersand = Input.GetKey(KeyCode.Ampersand);
                GetKey_Asterisk = Input.GetKey(KeyCode.Asterisk);
                GetKey_At = Input.GetKey(KeyCode.At);
                GetKey_BackQuote = Input.GetKey(KeyCode.BackQuote);
                GetKey_Backslash = Input.GetKey(KeyCode.Backslash);
                GetKey_Backspace = Input.GetKey(KeyCode.Backspace);
                GetKey_Break = Input.GetKey(KeyCode.Break);
                GetKey_CapsLock = Input.GetKey(KeyCode.CapsLock);
                GetKey_Caret = Input.GetKey(KeyCode.Caret);
                GetKey_Clear = Input.GetKey(KeyCode.Clear);
                GetKey_Colon = Input.GetKey(KeyCode.Colon);
                GetKey_Comma = Input.GetKey(KeyCode.Comma);
                GetKey_Delete = Input.GetKey(KeyCode.Delete);
                GetKey_Dollar = Input.GetKey(KeyCode.Dollar);
                GetKey_DoubleQuote = Input.GetKey(KeyCode.DoubleQuote);
                GetKey_End = Input.GetKey(KeyCode.End);
                GetKey_Equals = Input.GetKey(KeyCode.Equals);
                GetKey_Escape = Input.GetKey(KeyCode.Escape);
                GetKey_Exclaim = Input.GetKey(KeyCode.Exclaim);
                GetKey_Greater = Input.GetKey(KeyCode.Greater);
                GetKey_Hash = Input.GetKey(KeyCode.Hash);
                GetKey_Help = Input.GetKey(KeyCode.Help);
                GetKey_Home = Input.GetKey(KeyCode.Home);
                GetKey_Insert = Input.GetKey(KeyCode.Insert);
                GetKey_KeypadDivide = Input.GetKey(KeyCode.KeypadDivide);
                GetKey_KeypadEnter = Input.GetKey(KeyCode.KeypadEnter);
                GetKey_KeypadEquals = Input.GetKey(KeyCode.KeypadEquals);
                GetKey_KeypadMinus = Input.GetKey(KeyCode.KeypadMinus);
                GetKey_KeypadMultiply = Input.GetKey(KeyCode.KeypadMultiply);
                GetKey_KeypadPeriod = Input.GetKey(KeyCode.KeypadPeriod);
                GetKey_KeypadPlus = Input.GetKey(KeyCode.KeypadPlus);
                GetKey_LeftAlt = Input.GetKey(KeyCode.LeftAlt);
                GetKey_LeftApple = Input.GetKey(KeyCode.LeftApple);
                GetKey_LeftBracket = Input.GetKey(KeyCode.LeftBracket);
                GetKey_LeftCommand = Input.GetKey(KeyCode.LeftCommand);
                GetKey_LeftControl = Input.GetKey(KeyCode.LeftControl);
                GetKey_LeftCurlyBracket = Input.GetKey(KeyCode.LeftCurlyBracket);
                GetKey_LeftParen = Input.GetKey(KeyCode.LeftParen);
                GetKey_LeftShift = Input.GetKey(KeyCode.LeftShift);
                GetKey_LeftWindows = Input.GetKey(KeyCode.LeftWindows);
                GetKey_Less = Input.GetKey(KeyCode.Less);
                GetKey_Menu = Input.GetKey(KeyCode.Menu);
                GetKey_Minus = Input.GetKey(KeyCode.Minus);
                GetKey_None = Input.GetKey(KeyCode.None);
                GetKey_Numlock = Input.GetKey(KeyCode.Numlock);
                GetKey_PageDown = Input.GetKey(KeyCode.PageDown);
                GetKey_PageUp = Input.GetKey(KeyCode.PageUp);
                GetKey_Pause = Input.GetKey(KeyCode.Pause);
                GetKey_Percent = Input.GetKey(KeyCode.Percent);
                GetKey_Period = Input.GetKey(KeyCode.Period);
                GetKey_Pipe = Input.GetKey(KeyCode.Pipe);
                GetKey_Plus = Input.GetKey(KeyCode.Plus);
                GetKey_Print = Input.GetKey(KeyCode.Print);
                GetKey_Question = Input.GetKey(KeyCode.Question);
                GetKey_Quote = Input.GetKey(KeyCode.Quote);
                GetKey_Return = Input.GetKey(KeyCode.Return);
                GetKey_RightAlt = Input.GetKey(KeyCode.RightAlt);
                GetKey_RightApple = Input.GetKey(KeyCode.RightApple);
                GetKey_RightBracket = Input.GetKey(KeyCode.RightBracket);
                GetKey_RightCommand = Input.GetKey(KeyCode.RightCommand);
                GetKey_RightControl = Input.GetKey(KeyCode.RightControl);
                GetKey_RightCurlyBracket = Input.GetKey(KeyCode.RightCurlyBracket);
                GetKey_RightParen = Input.GetKey(KeyCode.RightParen);
                GetKey_RightShift = Input.GetKey(KeyCode.RightShift);
                GetKey_RightWindows = Input.GetKey(KeyCode.RightWindows);
                GetKey_ScrollLock = Input.GetKey(KeyCode.ScrollLock);
                GetKey_Semicolon = Input.GetKey(KeyCode.Semicolon);
                GetKey_Slash = Input.GetKey(KeyCode.Slash);
                GetKey_Space = Input.GetKey(KeyCode.Space);
                GetKey_SysReq = Input.GetKey(KeyCode.SysReq);
                GetKey_Tab = Input.GetKey(KeyCode.Tab);
                GetKey_Tilde = Input.GetKey(KeyCode.Tilde);
                GetKey_Underscore = Input.GetKey(KeyCode.Underscore);

                GetKey_LeftArrow = Input.GetKey(KeyCode.LeftArrow);
                GetKey_RightArrow = Input.GetKey(KeyCode.RightArrow);
                GetKey_UpArrow = Input.GetKey(KeyCode.UpArrow);
                GetKey_DownArrow = Input.GetKey(KeyCode.DownArrow);

                GetKey_F1 = Input.GetKey(KeyCode.F1);
                GetKey_F2 = Input.GetKey(KeyCode.F2);
                GetKey_F3 = Input.GetKey(KeyCode.F3);
                GetKey_F4 = Input.GetKey(KeyCode.F4);
                GetKey_F5 = Input.GetKey(KeyCode.F5);
                GetKey_F6 = Input.GetKey(KeyCode.F6);
                GetKey_F7 = Input.GetKey(KeyCode.F7);
                GetKey_F8 = Input.GetKey(KeyCode.F8);
                GetKey_F9 = Input.GetKey(KeyCode.F9);
                GetKey_F10 = Input.GetKey(KeyCode.F10);
                GetKey_F11 = Input.GetKey(KeyCode.F11);
                GetKey_F12 = Input.GetKey(KeyCode.F12);

                GetKey_JoystickButton0 = Input.GetKey(KeyCode.JoystickButton0);
                GetKey_JoystickButton1 = Input.GetKey(KeyCode.JoystickButton1);
                GetKey_JoystickButton2 = Input.GetKey(KeyCode.JoystickButton2);
                GetKey_JoystickButton3 = Input.GetKey(KeyCode.JoystickButton3);
                GetKey_JoystickButton4 = Input.GetKey(KeyCode.JoystickButton4);
                GetKey_JoystickButton5 = Input.GetKey(KeyCode.JoystickButton5);
                GetKey_JoystickButton6 = Input.GetKey(KeyCode.JoystickButton6);
                GetKey_JoystickButton7 = Input.GetKey(KeyCode.JoystickButton7);
                GetKey_JoystickButton8 = Input.GetKey(KeyCode.JoystickButton8);
                GetKey_JoystickButton9 = Input.GetKey(KeyCode.JoystickButton9);
                GetKey_JoystickButton10 = Input.GetKey(KeyCode.JoystickButton10);
                GetKey_JoystickButton11 = Input.GetKey(KeyCode.JoystickButton11);
                GetKey_JoystickButton12 = Input.GetKey(KeyCode.JoystickButton12);
                GetKey_JoystickButton13 = Input.GetKey(KeyCode.JoystickButton13);
                GetKey_JoystickButton14 = Input.GetKey(KeyCode.JoystickButton14);
                GetKey_JoystickButton15 = Input.GetKey(KeyCode.JoystickButton15);
                GetKey_JoystickButton16 = Input.GetKey(KeyCode.JoystickButton16);
                GetKey_JoystickButton17 = Input.GetKey(KeyCode.JoystickButton17);
                GetKey_JoystickButton18 = Input.GetKey(KeyCode.JoystickButton18);
                GetKey_JoystickButton19 = Input.GetKey(KeyCode.JoystickButton19);

                / * Have to comment this out mainly because code generation will not currently support more than 256 sync items per script and secondly because these are much less used:
                    GetKey_Joystick1Button0 = Input.GetKey(KeyCode.Joystick1Button0);
                    GetKey_Joystick1Button1 = Input.GetKey(KeyCode.Joystick1Button1);
                    GetKey_Joystick1Button2 = Input.GetKey(KeyCode.Joystick1Button2);
                    GetKey_Joystick1Button3 = Input.GetKey(KeyCode.Joystick1Button3);
                    GetKey_Joystick1Button4 = Input.GetKey(KeyCode.Joystick1Button4);
                    GetKey_Joystick1Button5 = Input.GetKey(KeyCode.Joystick1Button5);
                    GetKey_Joystick1Button6 = Input.GetKey(KeyCode.Joystick1Button6);
                    GetKey_Joystick1Button7 = Input.GetKey(KeyCode.Joystick1Button7);
                    GetKey_Joystick1Button8 = Input.GetKey(KeyCode.Joystick1Button8);
                    GetKey_Joystick1Button9 = Input.GetKey(KeyCode.Joystick1Button9);
                    GetKey_Joystick1Button10 = Input.GetKey(KeyCode.Joystick1Button10);
                    GetKey_Joystick1Button11 = Input.GetKey(KeyCode.Joystick1Button11);
                    GetKey_Joystick1Button12 = Input.GetKey(KeyCode.Joystick1Button12);
                    GetKey_Joystick1Button13 = Input.GetKey(KeyCode.Joystick1Button13);
                    GetKey_Joystick1Button14 = Input.GetKey(KeyCode.Joystick1Button14);
                    GetKey_Joystick1Button15 = Input.GetKey(KeyCode.Joystick1Button15);
                    GetKey_Joystick1Button16 = Input.GetKey(KeyCode.Joystick1Button16);
                    GetKey_Joystick1Button17 = Input.GetKey(KeyCode.Joystick1Button17);
                    GetKey_Joystick1Button18 = Input.GetKey(KeyCode.Joystick1Button18);
                    GetKey_Joystick1Button19 = Input.GetKey(KeyCode.Joystick1Button19);

                    GetKey_Joystick2Button0 = Input.GetKey(KeyCode.Joystick2Button0);
                    GetKey_Joystick2Button1 = Input.GetKey(KeyCode.Joystick2Button1);
                    GetKey_Joystick2Button2 = Input.GetKey(KeyCode.Joystick2Button2);
                    GetKey_Joystick2Button3 = Input.GetKey(KeyCode.Joystick2Button3);
                    GetKey_Joystick2Button4 = Input.GetKey(KeyCode.Joystick2Button4);
                    GetKey_Joystick2Button5 = Input.GetKey(KeyCode.Joystick2Button5);
                    GetKey_Joystick2Button6 = Input.GetKey(KeyCode.Joystick2Button6);
                    GetKey_Joystick2Button7 = Input.GetKey(KeyCode.Joystick2Button7);
                    GetKey_Joystick2Button8 = Input.GetKey(KeyCode.Joystick2Button8);
                    GetKey_Joystick2Button9 = Input.GetKey(KeyCode.Joystick2Button9);
                    GetKey_Joystick2Button10 = Input.GetKey(KeyCode.Joystick2Button10);
                    GetKey_Joystick2Button11 = Input.GetKey(KeyCode.Joystick2Button11);
                    GetKey_Joystick2Button12 = Input.GetKey(KeyCode.Joystick2Button12);
                    GetKey_Joystick2Button13 = Input.GetKey(KeyCode.Joystick2Button13);
                    GetKey_Joystick2Button14 = Input.GetKey(KeyCode.Joystick2Button14);
                    GetKey_Joystick2Button15 = Input.GetKey(KeyCode.Joystick2Button15);
                    GetKey_Joystick2Button16 = Input.GetKey(KeyCode.Joystick2Button16);
                    GetKey_Joystick2Button17 = Input.GetKey(KeyCode.Joystick2Button17);
                    GetKey_Joystick2Button18 = Input.GetKey(KeyCode.Joystick2Button18);
                    GetKey_Joystick2Button19 = Input.GetKey(KeyCode.Joystick2Button19);

                    GetKey_Joystick3Button0 = Input.GetKey(KeyCode.Joystick3Button0);
                    GetKey_Joystick3Button1 = Input.GetKey(KeyCode.Joystick3Button1);
                    GetKey_Joystick3Button2 = Input.GetKey(KeyCode.Joystick3Button2);
                    GetKey_Joystick3Button3 = Input.GetKey(KeyCode.Joystick3Button3);
                    GetKey_Joystick3Button4 = Input.GetKey(KeyCode.Joystick3Button4);
                    GetKey_Joystick3Button5 = Input.GetKey(KeyCode.Joystick3Button5);
                    GetKey_Joystick3Button6 = Input.GetKey(KeyCode.Joystick3Button6);
                    GetKey_Joystick3Button7 = Input.GetKey(KeyCode.Joystick3Button7);
                    GetKey_Joystick3Button8 = Input.GetKey(KeyCode.Joystick3Button8);
                    GetKey_Joystick3Button9 = Input.GetKey(KeyCode.Joystick3Button9);
                    GetKey_Joystick3Button10 = Input.GetKey(KeyCode.Joystick3Button10);
                    GetKey_Joystick3Button11 = Input.GetKey(KeyCode.Joystick3Button11);
                    GetKey_Joystick3Button12 = Input.GetKey(KeyCode.Joystick3Button12);
                    GetKey_Joystick3Button13 = Input.GetKey(KeyCode.Joystick3Button13);
                    GetKey_Joystick3Button14 = Input.GetKey(KeyCode.Joystick3Button14);
                    GetKey_Joystick3Button15 = Input.GetKey(KeyCode.Joystick3Button15);
                    GetKey_Joystick3Button16 = Input.GetKey(KeyCode.Joystick3Button16);
                    GetKey_Joystick3Button17 = Input.GetKey(KeyCode.Joystick3Button17);
                    GetKey_Joystick3Button18 = Input.GetKey(KeyCode.Joystick3Button18);
                    GetKey_Joystick3Button19 = Input.GetKey(KeyCode.Joystick3Button19);

                    GetKey_Joystick4Button0 = Input.GetKey(KeyCode.Joystick4Button0);
                    GetKey_Joystick4Button1 = Input.GetKey(KeyCode.Joystick4Button1);
                    GetKey_Joystick4Button2 = Input.GetKey(KeyCode.Joystick4Button2);
                    GetKey_Joystick4Button3 = Input.GetKey(KeyCode.Joystick4Button3);
                    GetKey_Joystick4Button4 = Input.GetKey(KeyCode.Joystick4Button4);
                    GetKey_Joystick4Button5 = Input.GetKey(KeyCode.Joystick4Button5);
                    GetKey_Joystick4Button6 = Input.GetKey(KeyCode.Joystick4Button6);
                    GetKey_Joystick4Button7 = Input.GetKey(KeyCode.Joystick4Button7);
                    GetKey_Joystick4Button8 = Input.GetKey(KeyCode.Joystick4Button8);
                    GetKey_Joystick4Button9 = Input.GetKey(KeyCode.Joystick4Button9);
                    GetKey_Joystick4Button10 = Input.GetKey(KeyCode.Joystick4Button10);
                    GetKey_Joystick4Button11 = Input.GetKey(KeyCode.Joystick4Button11);
                    GetKey_Joystick4Button12 = Input.GetKey(KeyCode.Joystick4Button12);
                    GetKey_Joystick4Button13 = Input.GetKey(KeyCode.Joystick4Button13);
                    GetKey_Joystick4Button14 = Input.GetKey(KeyCode.Joystick4Button14);
                    GetKey_Joystick4Button15 = Input.GetKey(KeyCode.Joystick4Button15);
                    GetKey_Joystick4Button16 = Input.GetKey(KeyCode.Joystick4Button16);
                    GetKey_Joystick4Button17 = Input.GetKey(KeyCode.Joystick4Button17);
                    GetKey_Joystick4Button18 = Input.GetKey(KeyCode.Joystick4Button18);
                    GetKey_Joystick4Button19 = Input.GetKey(KeyCode.Joystick4Button19);

                    GetKey_Joystick5Button0 = Input.GetKey(KeyCode.Joystick5Button0);
                    GetKey_Joystick5Button1 = Input.GetKey(KeyCode.Joystick5Button1);
                    GetKey_Joystick5Button2 = Input.GetKey(KeyCode.Joystick5Button2);
                    GetKey_Joystick5Button3 = Input.GetKey(KeyCode.Joystick5Button3);
                    GetKey_Joystick5Button4 = Input.GetKey(KeyCode.Joystick5Button4);
                    GetKey_Joystick5Button5 = Input.GetKey(KeyCode.Joystick5Button5);
                    GetKey_Joystick5Button6 = Input.GetKey(KeyCode.Joystick5Button6);
                    GetKey_Joystick5Button7 = Input.GetKey(KeyCode.Joystick5Button7);
                    GetKey_Joystick5Button8 = Input.GetKey(KeyCode.Joystick5Button8);
                    GetKey_Joystick5Button9 = Input.GetKey(KeyCode.Joystick5Button9);
                    GetKey_Joystick5Button10 = Input.GetKey(KeyCode.Joystick5Button10);
                    GetKey_Joystick5Button11 = Input.GetKey(KeyCode.Joystick5Button11);
                    GetKey_Joystick5Button12 = Input.GetKey(KeyCode.Joystick5Button12);
                    GetKey_Joystick5Button13 = Input.GetKey(KeyCode.Joystick5Button13);
                    GetKey_Joystick5Button14 = Input.GetKey(KeyCode.Joystick5Button14);
                    GetKey_Joystick5Button15 = Input.GetKey(KeyCode.Joystick5Button15);
                    GetKey_Joystick5Button16 = Input.GetKey(KeyCode.Joystick5Button16);
                    GetKey_Joystick5Button17 = Input.GetKey(KeyCode.Joystick5Button17);
                    GetKey_Joystick5Button18 = Input.GetKey(KeyCode.Joystick5Button18);
                    GetKey_Joystick5Button19 = Input.GetKey(KeyCode.Joystick5Button19);

                    GetKey_Joystick6Button0 = Input.GetKey(KeyCode.Joystick6Button0);
                    GetKey_Joystick6Button1 = Input.GetKey(KeyCode.Joystick6Button1);
                    GetKey_Joystick6Button2 = Input.GetKey(KeyCode.Joystick6Button2);
                    GetKey_Joystick6Button3 = Input.GetKey(KeyCode.Joystick6Button3);
                    GetKey_Joystick6Button4 = Input.GetKey(KeyCode.Joystick6Button4);
                    GetKey_Joystick6Button5 = Input.GetKey(KeyCode.Joystick6Button5);
                    GetKey_Joystick6Button6 = Input.GetKey(KeyCode.Joystick6Button6);
                    GetKey_Joystick6Button7 = Input.GetKey(KeyCode.Joystick6Button7);
                    GetKey_Joystick6Button8 = Input.GetKey(KeyCode.Joystick6Button8);
                    GetKey_Joystick6Button9 = Input.GetKey(KeyCode.Joystick6Button9);
                    GetKey_Joystick6Button10 = Input.GetKey(KeyCode.Joystick6Button10);
                    GetKey_Joystick6Button11 = Input.GetKey(KeyCode.Joystick6Button11);
                    GetKey_Joystick6Button12 = Input.GetKey(KeyCode.Joystick6Button12);
                    GetKey_Joystick6Button13 = Input.GetKey(KeyCode.Joystick6Button13);
                    GetKey_Joystick6Button14 = Input.GetKey(KeyCode.Joystick6Button14);
                    GetKey_Joystick6Button15 = Input.GetKey(KeyCode.Joystick6Button15);
                    GetKey_Joystick6Button16 = Input.GetKey(KeyCode.Joystick6Button16);
                    GetKey_Joystick6Button17 = Input.GetKey(KeyCode.Joystick6Button17);
                    GetKey_Joystick6Button18 = Input.GetKey(KeyCode.Joystick6Button18);
                    GetKey_Joystick6Button19 = Input.GetKey(KeyCode.Joystick6Button19);

                    GetKey_Joystick7Button0 = Input.GetKey(KeyCode.Joystick7Button0);
                    GetKey_Joystick7Button1 = Input.GetKey(KeyCode.Joystick7Button1);
                    GetKey_Joystick7Button2 = Input.GetKey(KeyCode.Joystick7Button2);
                    GetKey_Joystick7Button3 = Input.GetKey(KeyCode.Joystick7Button3);
                    GetKey_Joystick7Button4 = Input.GetKey(KeyCode.Joystick7Button4);
                    GetKey_Joystick7Button5 = Input.GetKey(KeyCode.Joystick7Button5);
                    GetKey_Joystick7Button6 = Input.GetKey(KeyCode.Joystick7Button6);
                    GetKey_Joystick7Button7 = Input.GetKey(KeyCode.Joystick7Button7);
                    GetKey_Joystick7Button8 = Input.GetKey(KeyCode.Joystick7Button8);
                    GetKey_Joystick7Button9 = Input.GetKey(KeyCode.Joystick7Button9);
                    GetKey_Joystick7Button10 = Input.GetKey(KeyCode.Joystick7Button10);
                    GetKey_Joystick7Button11 = Input.GetKey(KeyCode.Joystick7Button11);
                    GetKey_Joystick7Button12 = Input.GetKey(KeyCode.Joystick7Button12);
                    GetKey_Joystick7Button13 = Input.GetKey(KeyCode.Joystick7Button13);
                    GetKey_Joystick7Button14 = Input.GetKey(KeyCode.Joystick7Button14);
                    GetKey_Joystick7Button15 = Input.GetKey(KeyCode.Joystick7Button15);
                    GetKey_Joystick7Button16 = Input.GetKey(KeyCode.Joystick7Button16);
                    GetKey_Joystick7Button17 = Input.GetKey(KeyCode.Joystick7Button17);
                    GetKey_Joystick7Button18 = Input.GetKey(KeyCode.Joystick7Button18);
                    GetKey_Joystick7Button19 = Input.GetKey(KeyCode.Joystick7Button19);

                    GetKey_Joystick8Button0 = Input.GetKey(KeyCode.Joystick8Button0);
                    GetKey_Joystick8Button1 = Input.GetKey(KeyCode.Joystick8Button1);
                    GetKey_Joystick8Button2 = Input.GetKey(KeyCode.Joystick8Button2);
                    GetKey_Joystick8Button3 = Input.GetKey(KeyCode.Joystick8Button3);
                    GetKey_Joystick8Button4 = Input.GetKey(KeyCode.Joystick8Button4);
                    GetKey_Joystick8Button5 = Input.GetKey(KeyCode.Joystick8Button5);
                    GetKey_Joystick8Button6 = Input.GetKey(KeyCode.Joystick8Button6);
                    GetKey_Joystick8Button7 = Input.GetKey(KeyCode.Joystick8Button7);
                    GetKey_Joystick8Button8 = Input.GetKey(KeyCode.Joystick8Button8);
                    GetKey_Joystick8Button9 = Input.GetKey(KeyCode.Joystick8Button9);
                    GetKey_Joystick8Button10 = Input.GetKey(KeyCode.Joystick8Button10);
                    GetKey_Joystick8Button11 = Input.GetKey(KeyCode.Joystick8Button11);
                    GetKey_Joystick8Button12 = Input.GetKey(KeyCode.Joystick8Button12);
                    GetKey_Joystick8Button13 = Input.GetKey(KeyCode.Joystick8Button13);
                    GetKey_Joystick8Button14 = Input.GetKey(KeyCode.Joystick8Button14);
                    GetKey_Joystick8Button15 = Input.GetKey(KeyCode.Joystick8Button15);
                    GetKey_Joystick8Button16 = Input.GetKey(KeyCode.Joystick8Button16);
                    GetKey_Joystick8Button17 = Input.GetKey(KeyCode.Joystick8Button17);
                    GetKey_Joystick8Button18 = Input.GetKey(KeyCode.Joystick8Button18);
                    GetKey_Joystick8Button19 = Input.GetKey(KeyCode.Joystick8Button19);
            * /
            *
            */

            /*
            // TODO: code generate all the user defined axis (in partial class):
            //       Input.GetAxis()
            //       Input.GetButton()


            using UnityEngine;
 using System.Collections;

 using UnityEditor;

 public class ReadInputManager
 {
     public static void ReadAxes()
     {
         var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];

         SerializedObject obj = new SerializedObject(inputManager);

         SerializedProperty axisArray = obj.FindProperty("m_Axes");

         if (axisArray.arraySize == 0)
             Debug.Log("No Axes");

         for( int i = 0; i < axisArray.arraySize; ++i )
         {
             var axis = axisArray.GetArrayElementAtIndex(i);

             var name = axis.FindPropertyRelative("m_Name").stringValue;
             var axisVal = axis.FindPropertyRelative("axis").intValue;
             var inputType = (InputType)axis.FindPropertyRelative("type").intValue;

             Debug.Log(name);
             Debug.Log(axisVal);
             Debug.Log(inputType);
         }
     }

     public enum InputType
     {
         KeyOrMouseButton,
         MouseMovement,
         JoystickAxis,
     };

     [MenuItem("Assets/ReadInputManager")]
     public static void DoRead()
     {
         ReadAxes();
     }

 }

        */

            #endregion
        }
    }
}