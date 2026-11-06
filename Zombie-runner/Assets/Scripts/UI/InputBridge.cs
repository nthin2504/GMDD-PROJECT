using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace ZombieRunner
{
    /// <summary>
    /// Tiny input abstraction so the game runs whether the project uses the legacy
    /// Input Manager, the new Input System package, or both ("Active Input Handling").
    /// </summary>
    public static class InputBridge
    {
        public static bool GetKey(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key mapped = MapKey(key);
            return mapped != Key.None && kb[mapped].isPressed;
#else
            return false;
#endif
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key mapped = MapKey(key);
            return mapped != Key.None && kb[mapped].wasPressedThisFrame;
#else
            return false;
#endif
        }

        /// <summary>Unified mouse + touch pointer state.</summary>
        public static void GetPointer(out Vector2 position, out bool down, out bool held, out bool up)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;
                if (touch.phase == TouchPhase.Began) down = true;
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) held = true;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) up = true;
            }
#elif ENABLE_INPUT_SYSTEM
            position = Vector2.zero;
            down = false;
            held = false;
            up = false;

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                down = mouse.leftButton.wasPressedThisFrame;
                held = mouse.leftButton.isPressed;
                up = mouse.leftButton.wasReleasedThisFrame;
            }

            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                position = touch.primaryTouch.position.ReadValue();
                if (touch.primaryTouch.press.wasPressedThisFrame) down = true;
                held = true;
            }
            else if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame)
            {
                position = touch.primaryTouch.position.ReadValue();
                up = true;
            }
#else
            position = Vector2.zero;
            down = held = up = false;
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static Key MapKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                default: return Key.None;
            }
        }
#endif
    }
}
