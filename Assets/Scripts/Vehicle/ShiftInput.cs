using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftStorm
{
    // The same inputs in every form (Input System only). Bindings start from chastain_rush's VehicleInput (triggers, dead zones, Y / R reset);
    // ShiftStorm adds boost, the secondary button, the vertical axis and the invert setting.
    //   Pad:      RT accelerate, LT brake / reverse, left stick X steer, left stick Y climb / dive (pull back = up; Invert flips it), A boost,
    //             RB secondary: drift on land / tighter carve on water, LB wings (Round 7: the choice jump only), X camera view (Round 6), right stick look around, Y reset, View (Back) invert on / off, D-pad left / right: next start.
    //   Keyboard: W / Up accelerate, S / Down brake / reverse, A / D or Left / Right steer, I / K climb / dive (I = up), Space boost,
    //             Left Shift secondary, E wings (choice jump), V camera view, R reset, F2 invert on / off, F3 / F4 previous / next start.
    // Invert is saved in settings.json (Settings; Round 3, was PlayerPrefs "invertVertical"); also on the menu's Settings page.
    // Round 2, the course menu: Menu (Start) / Esc opens and closes it; D-pad up / down or Up / Down (W / S) move, A / Enter / Space choose,
    // B / Backspace go back. While it is open the race is paused, so these never reach the vehicle.
    // Round 2b: R3 (right stick click) / F1 shows or hides the FPS + speed readout; Alt+Enter switches full screen / windowed.
    public sealed class ShiftInput : MonoBehaviour
    {
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public float Vertical { get; private set; }   // + up (after the invert setting)
        public bool Boost { get; private set; }
        public bool Secondary { get; private set; }
        public static bool Invert { get => Settings.InvertVertical; set => Settings.InvertVertical = value; }
        // Round 6: per player (player 2 has their own invert setting, settings.json "invertVertical2")
        public bool MyInvert { get => player == 2 ? Settings.InvertVertical2 : Invert; set { if (player == 2) Settings.InvertVertical2 = value; else Invert = value; } }

        InputAction throttle, brake, steer, vertical, boost, secondary, wings, reset, invert, nextStart, prevStart, menu, menuUp, menuDown, confirm, back, stats, enter, view, look;
        bool viewRequested, wingsRequested;
        public Vector2 Look { get; private set; }   // Round 6: the right stick looks around (CameraViews)
        bool resetRequested; int startStep;
        public bool MenuPressed { get; private set; }
        public int MenuMove { get; private set; }
        public bool ConfirmPressed { get; private set; }
        public bool BackPressed { get; private set; }
        public bool StatsPressed { get; private set; }
        public bool FullscreenPressed { get; private set; }

        // Checks and the AI rivals drive through this same path (ShiftVehicle reads only these properties). sRawStick: sVertical is a stick
        // position (pushed forward = +, as the pad gives it) and goes through the invert setting like a real stick (the invert check).
        // The one-shot menu presses (sMenu, sMove, sConfirm, sBack) let a check work the menu as a player would.
        [System.NonSerialized] public bool Scripted, sRawStick, sMenu, sConfirm, sBack;
        [System.NonSerialized] public float sThrottle, sBrake, sSteer, sVertical; [System.NonSerialized] public bool sBoost, sSecondary, sView, sWings; [System.NonSerialized] public int sMove; [System.NonSerialized] public Vector2 sLook;

        void Awake() => Create(null);

        // Round 6 (split screen, from chastain_rush's VehicleInput.Bind): each player's vehicle answers only its own device (null = every
        // device, as before). A gamepad's bindings are rewritten to that pad's own path; the keyboard bindings exist only for the keyboard.
        public InputDevice Device { get; private set; }
        public int player;   // 0: the one player (as before); 1 or 2 in split screen (whose invert setting and camera view)
        public void Bind(InputDevice device)
        {
            if (device == Device && throttle != null) return;
            bool on = isActiveAndEnabled; if (throttle != null) { foreach (var x in All) { x.Disable(); x.Dispose(); } }
            Create(device); if (on) OnEnable();
            Debug.Log($"[Input] player {player} bound to {(device == null ? "every device" : device.displayName + " (" + device.path + ")")}");
        }
        // The device that last pressed a menu button here (the 2 Players page: player 1 is whoever opened it)
        public InputDevice LastDevice { get; private set; }

        void Create(InputDevice device)
        {
            Device = device; bool pad = device == null || device is Gamepad, keys = device == null || device is Keyboard;
            string P(string path) => device is Gamepad ? path.Replace("<Gamepad>", device.path) : path;
            InputAction A(string name, InputActionType type = InputActionType.Button) => new InputAction(name, type);
            void Add(InputAction a, string path, string processor = null) { if (path.Contains("<Gamepad>") ? !pad : !keys) return; var bb = a.AddBinding(P(path)); if (processor != null) bb.WithProcessor(processor); }
            void Axis(InputAction a, string neg, string pos) { if (keys) a.AddCompositeBinding("1DAxis").With("Negative", neg).With("Positive", pos); }
            throttle = A("Accelerate", InputActionType.Value);
            Add(throttle, "<Gamepad>/rightTrigger", "axisDeadzone(min=0.1,max=1)"); Add(throttle, "<Keyboard>/w"); Add(throttle, "<Keyboard>/upArrow");
            brake = A("Brake", InputActionType.Value);
            Add(brake, "<Gamepad>/leftTrigger", "axisDeadzone(min=0.1,max=1)"); Add(brake, "<Keyboard>/s"); Add(brake, "<Keyboard>/downArrow");
            steer = A("Steer", InputActionType.Value);
            Add(steer, "<Gamepad>/leftStick/x", "axisDeadzone(min=0.12,max=0.95)");
            Axis(steer, "<Keyboard>/a", "<Keyboard>/d"); Axis(steer, "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            vertical = A("Climb / dive", InputActionType.Value);
            Add(vertical, "<Gamepad>/leftStick/y", "axisDeadzone(min=0.15,max=0.95)"); Axis(vertical, "<Keyboard>/i", "<Keyboard>/k");
            boost = A("Boost"); Add(boost, "<Gamepad>/buttonSouth"); Add(boost, "<Keyboard>/space");
            // Round 6: X is the camera-view button now (as in Chastain Rush / Woodstock Rush); drift / carve stays on RB and Left Shift
            secondary = A("Drift / carve"); Add(secondary, "<Gamepad>/rightShoulder"); Add(secondary, "<Keyboard>/leftShift");
            // Round 7: LB / E opens the wings, only on the choice jump (ShiftVehicle.Stunts ignores it anywhere else)
            wings = A("Wings"); Add(wings, "<Gamepad>/leftShoulder"); Add(wings, "<Keyboard>/e");
            view = A("Change view"); Add(view, "<Gamepad>/buttonWest"); Add(view, "<Keyboard>/v");
            look = new InputAction("Look around", InputActionType.Value, expectedControlType: "Vector2"); Add(look, "<Gamepad>/rightStick", "stickDeadzone(min=0.2,max=0.95)");
            reset = A("Reset"); Add(reset, "<Gamepad>/buttonNorth"); Add(reset, "<Keyboard>/r");
            invert = A("Invert"); Add(invert, "<Gamepad>/select"); Add(invert, "<Keyboard>/f2");
            nextStart = A("Next start"); Add(nextStart, "<Gamepad>/dpad/right"); Add(nextStart, "<Keyboard>/f4");
            prevStart = A("Previous start"); Add(prevStart, "<Gamepad>/dpad/left"); Add(prevStart, "<Keyboard>/f3");
            menu = A("Menu"); Add(menu, "<Gamepad>/start"); Add(menu, "<Keyboard>/escape");
            menuUp = A("Menu up"); Add(menuUp, "<Gamepad>/dpad/up"); Add(menuUp, "<Keyboard>/upArrow"); Add(menuUp, "<Keyboard>/w");
            menuDown = A("Menu down"); Add(menuDown, "<Gamepad>/dpad/down"); Add(menuDown, "<Keyboard>/downArrow"); Add(menuDown, "<Keyboard>/s");
            confirm = A("Confirm"); Add(confirm, "<Gamepad>/buttonSouth"); Add(confirm, "<Keyboard>/enter"); Add(confirm, "<Keyboard>/space");
            back = A("Back"); Add(back, "<Gamepad>/buttonEast"); Add(back, "<Keyboard>/backspace");
            stats = A("FPS readout"); Add(stats, "<Gamepad>/rightStickPress"); Add(stats, "<Keyboard>/f1");
            enter = A("Enter"); Add(enter, "<Keyboard>/enter"); Add(enter, "<Keyboard>/numpadEnter");
            foreach (var x in new[] { confirm, menu, back, menuUp, menuDown }) x.performed += c => LastDevice = c.control.device;
        }
        InputAction[] All => new[] { throttle, brake, steer, vertical, boost, secondary, wings, reset, invert, nextStart, prevStart, menu, menuUp, menuDown, confirm, back, stats, enter, view, look };
        void OnEnable() { foreach (var a in All) a.Enable(); }
        void OnDisable() { foreach (var a in All) a.Disable(); Throttle = Brake = Steer = Vertical = 0; Boost = Secondary = false; Look = Vector2.zero; viewRequested = false; }
        void OnDestroy() { foreach (var a in All) a.Dispose(); }

        void Update()
        {
            MenuPressed = menu.WasPressedThisFrame() && !Scripted; ConfirmPressed = confirm.WasPressedThisFrame(); BackPressed = back.WasPressedThisFrame();
            MenuMove = (menuDown.WasPressedThisFrame() ? 1 : 0) - (menuUp.WasPressedThisFrame() ? 1 : 0);
            StatsPressed = stats.WasPressedThisFrame() && !Scripted;
            var kb = Keyboard.current; bool alt = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
            FullscreenPressed = alt && enter.WasPressedThisFrame() && !Scripted;
            if (FullscreenPressed) ConfirmPressed = false;   // Alt+Enter is never a menu "choose"
            if (Scripted)
            {
                if (sMenu) MenuPressed = true; if (sConfirm) ConfirmPressed = true; if (sBack) BackPressed = true; if (sMove != 0) MenuMove = sMove;
                sMenu = sConfirm = sBack = false; sMove = 0;
                Throttle = sThrottle; Brake = sBrake; Steer = sSteer; Boost = sBoost; Secondary = sSecondary;
                Vertical = sRawStick ? (MyInvert ? sVertical : -sVertical) : sVertical;
                Look = sLook; viewRequested |= sView; sView = false; wingsRequested |= sWings; sWings = false;
                return;
            }
            Throttle = throttle.ReadValue<float>(); Brake = brake.ReadValue<float>(); Steer = Mathf.Clamp(steer.ReadValue<float>(), -1, 1);
            // The pad's stick and the keys give "stick pushed up" as +; pulled back (up for the vehicle) is the default, so the sign flips here.
            float v = -Mathf.Clamp(vertical.ReadValue<float>(), -1, 1);
            Vertical = MyInvert ? -v : v;
            Boost = boost.IsPressed(); Secondary = secondary.IsPressed();
            resetRequested |= reset.WasPressedThisFrame();
            if (invert.WasPressedThisFrame()) MyInvert = !MyInvert;
            Look = look.ReadValue<Vector2>(); viewRequested |= view.WasPressedThisFrame(); wingsRequested |= wings.WasPressedThisFrame();
            if (nextStart.WasPressedThisFrame()) startStep++;
            if (prevStart.WasPressedThisFrame()) startStep--;
        }
        public bool ConsumeReset() { bool r = resetRequested; resetRequested = false; return r; }
        public void RequestReset() => resetRequested = true;
        public bool ConsumeWings() { bool r = wingsRequested; wingsRequested = false; return r; }
        public void RequestWings() => wingsRequested = true;
        public bool ConsumeView() { bool r = viewRequested; viewRequested = false; return r; }
        public int ConsumeStartStep() { int s = startStep; startStep = 0; return s; }
    }
}
