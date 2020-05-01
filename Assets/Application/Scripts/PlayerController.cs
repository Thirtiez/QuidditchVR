namespace QuidditchVR
{
    using Oculus.Platform.Samples.VrHoops;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using VRTK;

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private VRTK_ControllerEvents controllerEvents;
        [SerializeField] private BroomController broomController;
        [SerializeField] private Rigidbody rigidbody;
        [SerializeField] private Transform playerGround;
        [SerializeField] private Transform hipAnchor;
        [SerializeField] private Transform footAnchor;

        [SerializeField] private float broomOffset = -0.6f;
        [SerializeField] private float throttleForce = 500.0f;
        [SerializeField] private float pitchForce = 120.0f;
        [SerializeField] private float yawForce = 120.0f;
        [SerializeField] private float rollForce = 80.0f;
        [SerializeField] private float deadZone = 5.0f;
        [SerializeField] private float terminalVelocity = 120.0f;

        public UnityAction<Vector3> VelocityChanged;
        public UnityAction<Vector3> AngularVelocityChanged;

        private bool isHeadsetReady = false;
        private bool isPressingThrottle = false;
        private Vector3 currentSteeringRotation = Vector3.zero;
        private Vector3 previousVelocity = Vector3.zero;
        private Vector3 previousAngularVelocity = Vector3.zero;

        protected void Awake()
        {
            controllerEvents = controllerEvents ?? VRTK_DeviceFinder.GetControllerRightHand().GetComponentInChildren<VRTK_ControllerEvents>();
            rigidbody = rigidbody ?? GetComponent<Rigidbody>();

            controllerEvents.ButtonOneReleased += ButtonOneReleased;
            broomController.ThrottlePressed += ThrottlePressed;
            broomController.ThrottleReleased += ThrottleReleased;
            broomController.Steering += Steering;
        }

        public void Start()
        {
            StartCoroutine(WaitForHeadset());
        }

        private void FixedUpdate()
        {
            if (isPressingThrottle)
            {
                var force = Vector3.forward * throttleForce * Time.deltaTime;
                rigidbody.AddRelativeForce(force);

                //if (rigidbody.velocity.magnitude > terminalVelocity)
                //{
                //    rigidbody.velocity = rigidbody.velocity.normalized * terminalVelocity;
                //}

                Debug.Log($"Now traveling at {rigidbody.velocity.magnitude} m/s");
            }

            if (currentSteeringRotation != Vector3.zero)
            {
                var currentTorque = Vector3.zero;
                currentTorque.x = Mathf.Abs(currentSteeringRotation.x) > deadZone ? currentSteeringRotation.x * pitchForce * Time.deltaTime : 0.0f;
                currentTorque.y = Mathf.Abs(currentSteeringRotation.y) > deadZone ? currentSteeringRotation.y * yawForce * Time.deltaTime : 0.0f;
                currentTorque.z = Mathf.Abs(currentSteeringRotation.z) > deadZone ? currentSteeringRotation.z * rollForce * Time.deltaTime : 0.0f;
                rigidbody.AddRelativeTorque(currentTorque);

                Debug.Log($"Current stering rotation: {currentSteeringRotation}");
                Debug.Log($"Torque applied: {currentTorque}");
                Debug.Log($"Now turning at {rigidbody.angularVelocity.magnitude} m/s");

                currentSteeringRotation = Vector3.zero;
            }

            var velocityDelta = rigidbody.velocity - previousVelocity;
            var angularVelocityDelta = rigidbody.angularVelocity - previousAngularVelocity;
            if (velocityDelta != Vector3.zero)
            {
                VelocityChanged?.Invoke(rigidbody.velocity);
            }
            if (angularVelocityDelta != Vector3.zero)
            {
                AngularVelocityChanged?.Invoke(rigidbody.angularVelocity);
            }
        }

        public void Update()
        {
            if (isHeadsetReady)
            {
                SetPlayAreaCompensatedTransform();
            }
        }

        private IEnumerator WaitForHeadset()
        {
            while (VRTK_DeviceFinder.HeadsetTransform() == null)
            {
                yield return null;
            }
            yield return null;

            isHeadsetReady = true;
        }

        private void SetPlayAreaCompensatedTransform()
        {
            var playAreaTransform = VRTK_DeviceFinder.PlayAreaTransform();
            playAreaTransform.rotation = playerGround.rotation;
            playAreaTransform.position += playerGround.position - footAnchor.position;
        }

        private void SetAnchors()
        {
            var playAreaTransform = VRTK_DeviceFinder.PlayAreaTransform();
            var headsetLocalPosition = VRTK_DeviceFinder.HeadsetTransform().localPosition;

            // Center position
            headsetLocalPosition.y += broomOffset;
            transform.position = hipAnchor.position = playAreaTransform.TransformPoint(headsetLocalPosition);
            transform.eulerAngles = hipAnchor.eulerAngles = playAreaTransform.eulerAngles;

            // Ground position
            headsetLocalPosition.y = 0;
            playerGround.position = footAnchor.position = playAreaTransform.TransformPoint(headsetLocalPosition);
            playerGround.eulerAngles = footAnchor.eulerAngles = playAreaTransform.eulerAngles;

            Debug.Log($"Hip position: {transform.position}\nHip rotation: {transform.rotation}");
            Debug.Log($"Foot position: {playerGround.position}\nFoot rotation: {playerGround.rotation}");
        }

        private void ButtonOneReleased(object sender, ControllerInteractionEventArgs e)
        {
            SetAnchors();
        }

        private void ThrottlePressed()
        {
            isPressingThrottle = true;
        }

        private void ThrottleReleased()
        {
            isPressingThrottle = false;
        }

        private void Steering(Vector3 steeringRotation)
        {
            currentSteeringRotation = steeringRotation;
        }
    }
}
