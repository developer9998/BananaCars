using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace BananaCars
{
    [HarmonyPatch(typeof(Player))]
    public class Patches
    {
        private static bool? useSteamInput;

        private static LayerMask layerMask;

        private static Rigidbody rigidbody;

        private static Vector3 HeadDirection, RollDirection;

        private static Vector3 left_joystick;

        private static bool left_click;

        private static float Speed = 0f;
        private static float AbsoluteSpeed => Mathf.Abs(Speed);

        private static float Limit => Configuration.Limit.Value;
        private static float Acceleration => Configuration.Accceleration.Value;
        private static float Deadzone => Configuration.DeadZone.Value;

        private static float driftTransition = 0;

        [HarmonyPatch("LateUpdate"), HarmonyPrefix, HarmonyWrapSafe]
        public static void LatePatch(Player __instance)
        {
            layerMask = __instance.locomotionEnabledLayers;
            rigidbody = __instance.bodyCollider.attachedRigidbody;

            bool hasDownPlane = Physics.Raycast(__instance.bodyCollider.transform.position + (Vector3.down * (__instance.bodyCollider.height / 2f)), Vector3.down, out RaycastHit ray, 0.08f, layerMask); // def is 0.1

            // check what platform we are on if we haven't
            if (useSteamInput == null || !useSteamInput.HasValue)
            {
                // use the "platform" field in the PlayFabAuthenticator class, reflection is needed as this field is private
                useSteamInput = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue().ToString().ToLower() == "steam";
            }

            if (useSteamInput.Value)
            {
                // ControllerInputPoller.leftControllerPrimary2DAxis is not set using steamvr actions for the steam build
                left_joystick = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
                left_click = SteamVR_Actions.gorillaTag_LeftJoystickClick.GetState(SteamVR_Input_Sources.LeftHand);
            }
            else
            {
                // ControllerInputPoller.leftControllerPrimary2DAxis is set using the traditional oculus method for all builds
                left_joystick = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out left_click);
            }

            HeadDirection = __instance.headCollider.transform.forward;
            Vector3 projection = Vector3.ProjectOnPlane(HeadDirection, ray.normal);

            float joystick = left_joystick.y;
            float joystickAbs = Mathf.Abs(joystick);

            bool isDrifting = left_click;

            if (joystickAbs < Deadzone) // not moving
            {
                if (AbsoluteSpeed > 0.01f)
                {
                    // change speed to gradually slow down and stop all together
                    Speed = Mathf.MoveTowards(Speed, 0f, Time.unscaledDeltaTime * Acceleration);
                }
                else
                {
                    // determine our roll (what direction the player is facing)
                    RollDirection = projection;
                    driftTransition = 1;
                    // reset the speed so the player won't move
                    Speed = 0f;
                }
            }
            else // moving
            {
                if (isDrifting) // drifting
                {
                    // drift
                    driftTransition = 0;
                    RollDirection = Vector3.Slerp(projection, RollDirection, Mathf.Clamp(AbsoluteSpeed / 7.5f * 0.985f, 0f, 0.985f));
                }
                else // not drifting, i dont know what it's called to not drift
                {
                    // no drift, go from current to updated projection direction in 0.333 seconds
                    driftTransition = Mathf.Clamp01(driftTransition + (Time.unscaledDeltaTime * 3f));
                    RollDirection = Vector3.Slerp(RollDirection, projection, driftTransition);
                }
                RollDirection.y = projection.y;
                // change speed to align with whatever direction the stick is being pulled to
                Speed = Mathf.MoveTowards(Speed, joystick >= 0 ? Limit : -Limit, Time.unscaledDeltaTime * Acceleration * joystickAbs);
            }

            // if speed is enough to proceed moving and we are sat down on an object
            if (AbsoluteSpeed > 0.02f && hasDownPlane)
            {
                // move the player on the ground based on where we're looking, then factor in the speed and scale of the player
                rigidbody.velocity = RollDirection.normalized * Speed * Player.Instance.scale;
            }

            // if a hand is touching or sliding on an object
            if (__instance.IsHandTouching(true) || __instance.IsHandTouching(false) || __instance.IsHandSliding(true) || __instance.IsHandSliding(false))
            {
                // change speed to swiftly slow down and stop all together
                Speed = Mathf.MoveTowards(Speed, 0f, 1.2f * Time.unscaledDeltaTime);
            }
        }

        [HarmonyPatch("FixedUpdate"), HarmonyPrefix]
        public static bool FixedPatch()
        {
            // TODO: patch this out when the player is moving (in the car) but perform non swimming and levitation calculations
            return true;
        }
    }
}
