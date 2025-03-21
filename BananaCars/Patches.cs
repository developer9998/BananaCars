using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace BananaCars
{
    [HarmonyPatch(typeof(GTPlayer))]
    public class Patches
    {
        // Configuration
        private static bool? Steam;
        private static float MaxSpeed => Configuration.Limit.Value;
        private static float Acceleration => Configuration.Accceleration.Value;
        private static float InputDeadzone => Configuration.DeadZone.Value;
        private static bool UseExperimentalMovement => Configuration.Experiment.Value;

        // Locomotion
        private static LayerMask PlayerLayerMask;
        private static Rigidbody Rigidbody;

        // Input
        private static Vector3 ControlStickAxis;
        private static bool ControlStickClick;

        // Speed
        private static float Speed = 0f;
        private static float AbsoluteSpeed => Mathf.Abs(Speed);

        // Movement
        private static Vector3 HeadDirection, RollDirection;
        private static float driftTransition = 0;

        [HarmonyPatch("LateUpdate"), HarmonyPrefix, HarmonyWrapSafe]
        public static void LatePatch(GTPlayer __instance)
        {
            PlayerLayerMask = __instance.locomotionEnabledLayers;
            Rigidbody = __instance.bodyCollider.attachedRigidbody;

            // check what platform we are on if we haven't
            if (Steam == null || !Steam.HasValue)
            {
                // use the "platform" field in the PlayFabAuthenticator class, reflection is needed as this field is private
                Steam = PlayFabAuthenticator.instance.platform.PlatformTag.ToLower() == "steam";
            }

            if (Steam.Value)
            {
                // ControllerInputPoller.leftControllerPrimary2DAxis is not set using steamvr actions for the steam build
                ControlStickAxis = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
                ControlStickClick = SteamVR_Actions.gorillaTag_LeftJoystickClick.GetState(SteamVR_Input_Sources.LeftHand);
            }
            else
            {
                // ControllerInputPoller.leftControllerPrimary2DAxis is set using the traditional oculus method for all builds
                ControlStickAxis = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out ControlStickClick);
            }

            bool hasDownPlane = Physics.Raycast(__instance.bodyCollider.transform.position + (Vector3.down * (__instance.bodyCollider.height / 2f)), Vector3.down, out RaycastHit ray, 0.06f, PlayerLayerMask); // def is 0.1
            HeadDirection = __instance.headCollider.transform.forward;
            Vector3 projection = Vector3.ProjectOnPlane(HeadDirection, ray.normal);

            float joystick = ControlStickAxis.y;
            float joystickAbs = Mathf.Abs(joystick);

            bool isDrifting = ControlStickClick;

            if (UseExperimentalMovement)
            {
                if (joystickAbs < InputDeadzone) // not moving
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
                else
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
                    // change speed to align with whatever direction the stick is being pulled to
                    Speed = Mathf.MoveTowards(Speed, joystick >= 0 ? MaxSpeed : -MaxSpeed, Time.unscaledDeltaTime * Acceleration * joystickAbs);
                }
                RollDirection.y = projection.y;
            }
            else
            {
                RollDirection = projection;

                if (joystickAbs < InputDeadzone) // not moving
                {
                    if (AbsoluteSpeed > 0.01f)
                    {
                        // change speed to gradually slow down and stop all together
                        Speed = Mathf.MoveTowards(Speed, 0f, Time.unscaledDeltaTime * Acceleration);
                    }
                    else
                    {
                        // reset the speed so the player won't move
                        Speed = 0f;
                    }
                }
                else
                {
                    // change speed to align with whatever direction the stick is being pulled to
                    Speed = Mathf.MoveTowards(Speed, joystick >= 0 ? MaxSpeed : -MaxSpeed, Time.unscaledDeltaTime * Acceleration * joystickAbs);
                }
            }

            // if speed is enough to proceed moving and we are sat down on an object
            if (AbsoluteSpeed > 0.02f && hasDownPlane)
            {
                // move the player on the ground based on where we're looking, then factor in the speed and scale of the player
                Rigidbody.velocity = RollDirection.normalized * Speed * GTPlayer.Instance.scale;
            }

            // if a hand is touching or sliding on an object
            if (__instance.IsHandTouching(true) || __instance.IsHandTouching(false) || __instance.IsHandSliding(true) || __instance.IsHandSliding(false))
            {
                // change speed to swiftly slow down and stop all together
                Speed = Mathf.MoveTowards(Speed, 0f, 1.2f * Time.unscaledDeltaTime);
            }
        }

        [HarmonyPatch("FixedUpdate"), HarmonyPrefix]
        public static bool FixedPatch(GTPlayer __instance)
        {
            // TODO: patch this out when the player is moving (in the car) but perform non swimming and levitation calculations
            return true;
        }
    }
}
