using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dokdo.Web
{
    public static class BrowserKeyboardSteering
    {
        // W, A, S, D match the shell's four-bit state. Opposites cancel.
        public static Vector2 Direction(int mask)
        {
            return new Vector2(((mask & 8) != 0 ? 1 : 0) -
                ((mask & 2) != 0 ? 1 : 0),
                ((mask & 1) != 0 ? 1 : 0) -
                ((mask & 4) != 0 ? 1 : 0)).normalized;
        }

        public static float Heading(Vector2 direction, Transform camera)
        {
            var right = Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized;
            // A perpendicular basis keeps diagonals at 45 degrees even when
            // the gameplay camera is pitched toward the water.
            var forward = Vector3.Cross(right, Vector3.up);
            var world = right * direction.x + forward * direction.y;
            return Mathf.Atan2(world.x, world.z) * Mathf.Rad2Deg;
        }
    }

    // The recovered ShipController has the default execution order (zero).
    [DefaultExecutionOrder(-100)]
    public sealed class BrowserKeyboardSteeringRuntime : MonoBehaviour
    {
        private const BindingFlags Fields = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;
        private Component ship;
        private Transform steering;
        private Transform gameplayCamera;
        private GameObject mouseIndicator;
        private FieldInfo touch, autoSail, doubleClick, parking, dead, outing;
        private FieldInfo engine, turn;
        private FieldInfo popupInstance, popupRoot, preloaderRoot;
        private readonly List<GameObject> blockingPanels = new();
        private bool bindingsReady;
        private bool lifecycleBlocked;
        private bool inputBlocked;
        private int mask;
        private Action<bool> reportEnabled;
        private bool reportedEnabled;

        public bool OwnsSteering { get; private set; }

        public void Configure(Action<bool> report)
        {
            reportEnabled = report;
            reportedEnabled = false;
            reportEnabled?.Invoke(false);
        }

        public void Refresh()
        {
            Cancel();
            SetAvailable(false);
            bindingsReady = false;
            var type = Type.GetType("ShipController, Assembly-CSharp", false);
            ship = FindSceneComponent(type);
            var camera = FindSceneComponent(Type.GetType(
                "CameraZoom, Assembly-CSharp", false));
            gameplayCamera = camera != null ? camera.transform : null;
            steering = Read<Transform>(type, "controller", ship);
            var indicator = Read<Transform>(type, "mousePos", ship);
            mouseIndicator = indicator != null ? indicator.gameObject : null;
            touch = type?.GetField("isTouch", Fields);
            autoSail = type?.GetField("isDouble", Fields);
            doubleClick = type?.GetField("doubleclickT", Fields);
            parking = type?.GetField("isParking", Fields);
            dead = type?.GetField("isDead", Fields);
            outing = type?.GetField("isOuting", Fields);
            engine = type?.GetField("v", Fields);
            turn = type?.GetField("s", Fields);
            var popup = Type.GetType("OfflinePopupView, Assembly-CSharp", false);
            popupInstance = popup?.GetField("instance", BindingFlags.Static |
                BindingFlags.NonPublic);
            popupRoot = popup?.GetField("popupRoot", Fields);
            preloaderRoot = popup?.GetField("preloaderRoot", Fields);
            blockingPanels.Clear();
            BindPanels(ship, "IslandRewardPanel", "DeadPanel", "Dead_Arena");
            BindPanels(FindSceneComponent(Type.GetType("UIManager, Assembly-CSharp", false)),
                "hiddenpop", "ReRewardPanel", "Loading");
            bindingsReady = ship != null && steering != null &&
                gameplayCamera != null && mouseIndicator != null &&
                touch != null && autoSail != null && doubleClick != null &&
                parking != null && dead != null && outing != null &&
                engine != null && turn != null && popupInstance != null &&
                popupRoot != null && preloaderRoot != null;
        }

        public void SetLifecycleBlocked(bool blocked)
        {
            lifecycleBlocked = blocked;
            CheckAvailability();
        }

        public void Receive(string payload)
        {
            if (!int.TryParse(payload, out var next) || next < 0 || next > 15)
            {
                Cancel();
                return;
            }
            if (next == 0)
            {
                Cancel();
                return;
            }
            if (!CheckAvailability()) return;
            mask = next;
            if (!OwnsSteering)
            {
                OwnsSteering = true;
                autoSail.SetValue(ship, false);
                doubleClick.SetValue(ship, 0f);
            }
            ApplyInput();
        }

        private void Update()
        {
            // Update keeps running when a map/menu has stopped FixedUpdate.
            CheckAvailability();
        }

        private void FixedUpdate()
        {
            if (CheckAvailability() && OwnsSteering) ApplyInput();
        }

        private void LateUpdate()
        {
            // Also catch menus opened by later Update callbacks this frame.
            CheckAvailability();
            if (OwnsSteering && mouseIndicator != null)
                mouseIndicator.SetActive(false);
        }

        private bool CheckAvailability()
        {
            var available = CanSteer();
            if (!available) Cancel();
            SetAvailable(available);
            return available;
        }

        private bool CanSteer()
        {
            if (!isActiveAndEnabled || !bindingsReady || lifecycleBlocked ||
                inputBlocked || Time.timeScale <= 0f || ship == null ||
                steering == null || gameplayCamera == null ||
                !ship.gameObject.activeInHierarchy ||
                (ship is Behaviour behaviour && !behaviour.enabled) ||
                (bool)parking.GetValue(ship) || (bool)dead.GetValue(ship) ||
                (bool)outing.GetValue(ship) || TouchScreenKeyboard.visible ||
                !string.IsNullOrEmpty(Input.compositionString)) return false;
            foreach (var panel in blockingPanels)
                if (IsActive(panel)) return false;
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.GetComponent<InputField>() is InputField input &&
                input.isFocused) return false;
            var popup = popupInstance.GetValue(null) as Component;
            return popup == null ||
                (!IsActive(popupRoot.GetValue(popup) as GameObject) &&
                 !IsActive(preloaderRoot.GetValue(popup) as GameObject));
        }

        private void SetAvailable(bool available)
        {
            if (reportedEnabled == available) return;
            reportedEnabled = available;
            // Update our state first: disabling can synchronously send zero
            // back through the browser bridge.
            reportEnabled?.Invoke(available);
        }

        private void ApplyInput()
        {
            var direction = BrowserKeyboardSteering.Direction(mask);
            var moving = direction != Vector2.zero;
            if (moving)
            {
                var heading = BrowserKeyboardSteering.Heading(direction, gameplayCamera);
                // ShipController maps local Z + 90 onto a rotation about down.
                steering.localRotation = Quaternion.Euler(0f, 0f, -heading - 90f);
            }
            touch.SetValue(ship, moving);
            if (!moving)
            {
                engine.SetValue(ship, 0f);
                turn.SetValue(ship, 0f);
            }
        }

        private void Cancel()
        {
            mask = 0;
            if (!OwnsSteering) return;
            OwnsSteering = false;
            if (ship == null) return;
            touch.SetValue(ship, false);
            engine.SetValue(ship, 0f);
            turn.SetValue(ship, 0f);
            if (mouseIndicator != null) mouseIndicator.SetActive(false);
        }

        private void OnApplicationFocus(bool focused)
        {
            inputBlocked = !focused;
            CheckAvailability();
        }

        private void OnDisable()
        {
            Cancel();
            SetAvailable(false);
        }

        private void BindPanels(Component target, params string[] names)
        {
            if (target == null) return;
            foreach (var name in names)
            {
                var panel = Read<GameObject>(target.GetType(), name, target);
                if (panel != null) blockingPanels.Add(panel);
            }
        }

        private static bool IsActive(GameObject value) =>
            value != null && value.activeInHierarchy;

        private static T Read<T>(Type type, string name, Component target) where T : class =>
            target != null ? type?.GetField(name, Fields)?.GetValue(target) as T : null;

        private static Component FindSceneComponent(Type type)
        {
            if (type == null) return null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll(type))
                if (candidate is Component component && component.gameObject.scene.IsValid())
                    return component;
            return null;
        }
    }
}
