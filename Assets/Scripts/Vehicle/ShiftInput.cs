using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftStorm
{
    // The same inputs in every form (Input System only). Bindings start from chastain_rush's VehicleInput (triggers, dead zones, Y / R reset);
    // ShiftStorm adds boost, the secondary button, the vertical axis and the invert setting.
    //   Pad:      RT accelerate, LT brake / reverse, left stick X steer, left stick Y climb / dive (pull back = up; Invert flips it), A boost,
    //             RB (or X) secondary: drift on land / tighter carve on water, Y reset, View (Back) invert on / off, D-pad left / right: next start.
    //   Keyboard: W / Up accelerate, S / Down brake / reverse, A / D or Left / Right steer, I / K climb / dive (I = up), Space boost,
    //             Left Shift secondary, R reset, F2 invert on / off, F3 / F4 previous / next start.
    // Invert is saved in PlayerPrefs ("invertVertical"); a check run never writes it.
    // Round 2, the course menu: Menu (Start) / Esc opens and closes it; D-pad up / down or Up / Down (W / S) move, A / Enter / Space choose,
    // B / Backspace go back. While it is open the race is paused, so these never reach the vehicle.
    public sealed class ShiftInput : MonoBehaviour
    {
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public float Vertical { get; private set; }   // + up (after the invert setting)
        public bool Boost { get; private set; }
        public bool Secondary { get; private set; }
        public static bool Invert { get; private set; }

        InputAction throttle, brake, steer, vertical, boost, secondary, reset, invert, nextStart, prevStart, menu, menuUp, menuDown, confirm, back;
        bool resetRequested; int startStep;
        public bool MenuPressed { get; private set; }
        public int MenuMove { get; private set; }
        public bool ConfirmPressed { get; private set; }
        public bool BackPressed { get; private set; }

        // Checks drive through this same path (ShiftVehicle reads only these properties).
        [System.NonSerialized] public bool Scripted;
        [System.NonSerialized] public float sThrottle, sBrake, sSteer, sVertical; [System.NonSerialized] public bool sBoost, sSecondary;

        void Awake()
        {
            Invert = !QuietRun.Check && PlayerPrefs.GetInt("invertVertical", 0) == 1;
            throttle = new InputAction("Accelerate", InputActionType.Value);
            throttle.AddBinding("<Gamepad>/rightTrigger").WithProcessor("axisDeadzone(min=0.1,max=1)"); throttle.AddBinding("<Keyboard>/w"); throttle.AddBinding("<Keyboard>/upArrow");
            brake = new InputAction("Brake", InputActionType.Value);
            brake.AddBinding("<Gamepad>/leftTrigger").WithProcessor("axisDeadzone(min=0.1,max=1)"); brake.AddBinding("<Keyboard>/s"); brake.AddBinding("<Keyboard>/downArrow");
            steer = new InputAction("Steer", InputActionType.Value);
            steer.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone(min=0.12,max=0.95)");
            steer.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            steer.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            vertical = new InputAction("Climb / dive", InputActionType.Value);
            vertical.AddBinding("<Gamepad>/leftStick/y").WithProcessor("axisDeadzone(min=0.15,max=0.95)");
            vertical.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/i").With("Positive", "<Keyboard>/k");
            boost = new InputAction("Boost", InputActionType.Button); boost.AddBinding("<Gamepad>/buttonSouth"); boost.AddBinding("<Keyboard>/space");
            secondary = new InputAction("Drift / carve", InputActionType.Button);
            secondary.AddBinding("<Gamepad>/rightShoulder"); secondary.AddBinding("<Gamepad>/buttonWest"); secondary.AddBinding("<Keyboard>/leftShift");
            reset = new InputAction("Reset", InputActionType.Button); reset.AddBinding("<Gamepad>/buttonNorth"); reset.AddBinding("<Keyboard>/r");
            invert = new InputAction("Invert", InputActionType.Button); invert.AddBinding("<Gamepad>/select"); invert.AddBinding("<Keyboard>/f2");
            nextStart = new InputAction("Next start", InputActionType.Button); nextStart.AddBinding("<Gamepad>/dpad/right"); nextStart.AddBinding("<Keyboard>/f4");
            prevStart = new InputAction("Previous start", InputActionType.Button); prevStart.AddBinding("<Gamepad>/dpad/left"); prevStart.AddBinding("<Keyboard>/f3");
            menu = new InputAction("Menu", InputActionType.Button); menu.AddBinding("<Gamepad>/start"); menu.AddBinding("<Keyboard>/escape");
            menuUp = new InputAction("Menu up", InputActionType.Button); menuUp.AddBinding("<Gamepad>/dpad/up"); menuUp.AddBinding("<Keyboard>/upArrow"); menuUp.AddBinding("<Keyboard>/w");
            menuDown = new InputAction("Menu down", InputActionType.Button); menuDown.AddBinding("<Gamepad>/dpad/down"); menuDown.AddBinding("<Keyboard>/downArrow"); menuDown.AddBinding("<Keyboard>/s");
            confirm = new InputAction("Confirm", InputActionType.Button); confirm.AddBinding("<Gamepad>/buttonSouth"); confirm.AddBinding("<Keyboard>/enter"); confirm.AddBinding("<Keyboard>/space");
            back = new InputAction("Back", InputActionType.Button); back.AddBinding("<Gamepad>/buttonEast"); back.AddBinding("<Keyboard>/backspace");
        }
        InputAction[] All => new[] { throttle, brake, steer, vertical, boost, secondary, reset, invert, nextStart, prevStart, menu, menuUp, menuDown, confirm, back };
        void OnEnable() { foreach (var a in All) a.Enable(); }
        void OnDisable() { foreach (var a in All) a.Disable(); Throttle = Brake = Steer = Vertical = 0; Boost = Secondary = false; }
        void OnDestroy() { foreach (var a in All) a.Dispose(); }

        void Update()
        {
            MenuPressed = menu.WasPressedThisFrame() && !Scripted; ConfirmPressed = confirm.WasPressedThisFrame(); BackPressed = back.WasPressedThisFrame();
            MenuMove = (menuDown.WasPressedThisFrame() ? 1 : 0) - (menuUp.WasPressedThisFrame() ? 1 : 0);
            if (Scripted)
            {
                Throttle = sThrottle; Brake = sBrake; Steer = sSteer; Vertical = sVertical; Boost = sBoost; Secondary = sSecondary;
                return;
            }
            Throttle = throttle.ReadValue<float>(); Brake = brake.ReadValue<float>(); Steer = Mathf.Clamp(steer.ReadValue<float>(), -1, 1);
            // The pad's stick and the keys give "stick pushed up" as +; pulled back (up for the vehicle) is the default, so the sign flips here.
            float v = -Mathf.Clamp(vertical.ReadValue<float>(), -1, 1);
            Vertical = Invert ? -v : v;
            Boost = boost.IsPressed(); Secondary = secondary.IsPressed();
            resetRequested |= reset.WasPressedThisFrame();
            if (invert.WasPressedThisFrame()) { Invert = !Invert; PlayerPrefs.SetInt("invertVertical", Invert ? 1 : 0); PlayerPrefs.Save(); }
            if (nextStart.WasPressedThisFrame()) startStep++;
            if (prevStart.WasPressedThisFrame()) startStep--;
        }
        public bool ConsumeReset() { bool r = resetRequested; resetRequested = false; return r; }
        public void RequestReset() => resetRequested = true;
        public int ConsumeStartStep() { int s = startStep; startStep = 0; return s; }
    }
}
