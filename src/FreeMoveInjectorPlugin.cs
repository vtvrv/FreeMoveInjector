//Solution and Project configuration borrowed from
//https://github.com/sinai-dev/BepInExConfigManager

//Movement and Rotation Code
//https://gist.github.com/FreyaHolmer/650ecd551562352120445513efa1d952

using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UniverseLib.Input;

namespace BehaviourInjector
{
    [BepInPlugin("com.vtvrvxiv.FreeMoveInjector", "FreeMoveInjector", "1.0.0")]
    public class FreeMovePlugin

#if MONO
        : BaseUnityPlugin
#else
        : BepInEx.IL2CPP.BasePlugin
#endif
    {
        public static ConfigEntry<KeyCode> KB_MoveForward { get; private set; }
        public static ConfigEntry<KeyCode> KB_MoveBackward { get; private set; }
        public static ConfigEntry<KeyCode> KB_MoveLeft { get; private set; }
        public static ConfigEntry<KeyCode> KB_MoveRight { get; private set; }
        public static ConfigEntry<KeyCode> KB_MoveUp { get; private set; }
        public static ConfigEntry<KeyCode> KB_MoveDown { get; private set; }


        public static ConfigEntry<float> acceleration { get; private set; }
        public static ConfigEntry<float> dampingCoefficient { get; private set; }


        public static ConfigEntry<KeyCode> KB_IncreaseAcceleration { get; private set; }
        public static ConfigEntry<KeyCode> KB_DecreaseAcceleration { get; private set; }

        public static ConfigEntry<KeyCode> KB_IncreaseDampening { get; private set; }
        public static ConfigEntry<KeyCode> KB_DecreaseDampening { get; private set; }


        public static ConfigEntry<MouseToggle> toggleMouse { get; private set; }
        public static ConfigEntry<float> lookSensitivity { get; private set; }

        public static ConfigEntry<bool> swapMouseAxis { get; private set; }
        public static ConfigEntry<bool> invertMouseHorz { get; private set; }
        public static ConfigEntry<bool> invertMouseVert { get; private set; }



#if MONO
        internal void Awake()
        {
            ConfigEntrySetup();
        }

        internal void Update()
        {

        }
#else
        public override void Load()
        {
            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<FreeMove>();
            ConfigEntrySetup();
        }
#endif

        private void ConfigEntrySetup()
        {
            acceleration = Config.Bind<float>("Keyboard Movement", "acceleration", 300.0f, "how fast you accelerate");
            dampingCoefficient = Config.Bind<float>("Keyboard Movement", "dampingCoefficient", 10.0f, "how quickly you break to a halt after you stop your input");

            KB_MoveForward = Config.Bind("Keyboard Movement", "Move Forward", KeyCode.I);
            KB_MoveBackward = Config.Bind("Keyboard Movement", "Move Backward", KeyCode.K);
            KB_MoveLeft = Config.Bind("Keyboard Movement", "Move Left", KeyCode.J);
            KB_MoveRight = Config.Bind("Keyboard Movement", "Move Right", KeyCode.L);
            KB_MoveUp = Config.Bind("Keyboard Movement", "Move Up", KeyCode.O);
            KB_MoveDown = Config.Bind("Keyboard Movement", "Move Down", KeyCode.U);

            KB_IncreaseAcceleration = Config.Bind("Keyboard Movement", "Increase Acceleration", KeyCode.RightBracket);
            KB_DecreaseAcceleration = Config.Bind("Keyboard Movement", "Decrease Acceleration", KeyCode.LeftBracket);

            KB_IncreaseDampening = Config.Bind("Keyboard Movement", "Increase Dampening", KeyCode.Quote);
            KB_DecreaseDampening = Config.Bind("Keyboard Movement", "Decrease Dampening", KeyCode.Semicolon);

            swapMouseAxis = Config.Bind("Mouse Look", "Swap Mouse Axes", false);
            invertMouseHorz = Config.Bind("Mouse Look", "Invert Horizontal Mouse Movement", false);
            invertMouseVert = Config.Bind("Mouse Look", "Invert Vertical Mouse Movement", false);

            toggleMouse = Config.Bind("Mouse Look", "Toggle", MouseToggle.Left_Mouse_Button);
            lookSensitivity = Config.Bind("Mouse Look", "look Sensitivity", 0.5f);
        }
    }


    public enum MouseToggle
    {
        Left_Mouse_Button = 0,
        Right_Mouse_Button = 1,
        Middle_Mouse_Button = 2,
        Mouse_4 = 3,
        Mouse_5 = 4,
        Always_On = 10,
        Always_Off = 11
    }


    public class FreeMove : MonoBehaviour
    {
        private Vector3 prevMouse = Vector3.zero;
        internal static FreeMove Instance { get; private set; }

        internal void Start()
        {
            UnityEngine.Debug.Log("FreeMoveInjector Starting...");
        }

        Vector3 velocity; // current velocity

#if CPP
        public FreeMove(IntPtr ptr) : base(ptr) { }
#endif
        internal void Update()
        {
            // Input
            UpdateInput();

            // Physics
            velocity = Vector3.Lerp(velocity, Vector3.zero, FreeMovePlugin.dampingCoefficient.Value * 0.01665f); //"0.01665f" (60fps) in place of Time.DeltaTime. DeltaTime causes issues when game is paused.
            base.gameObject.transform.position += velocity * 0.01665f; //"0.01665f" in place of Time.DeltaTime.
        }

        void UpdateInput()
        {
            //some hotkeys
            if (InputManager.GetKey(FreeMovePlugin.KB_IncreaseAcceleration.Value))
                FreeMovePlugin.acceleration.Value *= 1.1f;
            if (InputManager.GetKey(FreeMovePlugin.KB_DecreaseAcceleration.Value))
                FreeMovePlugin.acceleration.Value *= 0.9f;

            if (InputManager.GetKey(FreeMovePlugin.KB_IncreaseDampening.Value))
                FreeMovePlugin.dampingCoefficient.Value *= 1.05f;
            if (InputManager.GetKey(FreeMovePlugin.KB_DecreaseDampening.Value))
                FreeMovePlugin.dampingCoefficient.Value *= 0.95f;

            //hacky mouse input.
            //GetAxis() causes problems with games that use custom axis names
            Vector3 mousePos = InputManager.MousePosition;
            Vector3 mouseDelta = (mousePos - prevMouse) * FreeMovePlugin.lookSensitivity.Value;
            prevMouse = mousePos;

            // Rotation
            if (InputManager.GetMouseButton((int)FreeMovePlugin.toggleMouse.Value)
                || FreeMovePlugin.toggleMouse.Value == MouseToggle.Always_On)
            {
                if (FreeMovePlugin.invertMouseHorz.Value)
                    mouseDelta.x = -mouseDelta.x;
                if (FreeMovePlugin.invertMouseVert.Value)
                    mouseDelta.y = -mouseDelta.y;
                if (FreeMovePlugin.swapMouseAxis.Value)
                    mouseDelta = new Vector3(mouseDelta.y, mouseDelta.x, mouseDelta.z);

                // Rotation; Does anyone know how I can make this smooth?
                Quaternion rotation = transform.rotation;
                Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
                Quaternion vert = Quaternion.AngleAxis(-mouseDelta.y, Vector3.right);
                transform.rotation = horiz * rotation * vert;
            }

            // Position
            velocity += GetAccelerationVector() * 0.01665f; //"0.01665f" in place of Time.DeltaTime
        }

        Vector3 GetAccelerationVector()
        {
            Vector3 moveInput = default;

            void AddMovement(KeyCode key, Vector3 dir)
            {
                if (InputManager.GetKey(key))
                    moveInput += dir;
            }

            AddMovement(FreeMovePlugin.KB_MoveForward.Value, Vector3.forward);
            AddMovement(FreeMovePlugin.KB_MoveBackward.Value, Vector3.back);
            AddMovement(FreeMovePlugin.KB_MoveRight.Value, Vector3.right);
            AddMovement(FreeMovePlugin.KB_MoveLeft.Value, Vector3.left);
            AddMovement(FreeMovePlugin.KB_MoveUp.Value, Vector3.up);
            AddMovement(FreeMovePlugin.KB_MoveDown.Value, Vector3.down);
            Vector3 direction = transform.TransformVector(moveInput.normalized);

            return direction * FreeMovePlugin.acceleration.Value;
        }
    }
}