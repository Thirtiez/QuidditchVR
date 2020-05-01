namespace QuidditchVR
{
    using UnityEngine;
    using System.Collections;
    using VRTK.GrabAttachMechanics;
    using VRTK;

    public struct RotateGrabAttachEventArgs
    {
        public GameObject interactingObject;
        public Vector3 currentRotation;
        public Vector3 rotationSpeed;
    }

    public delegate void RotateGrabAttachEventHandler(object sender, RotateGrabAttachEventArgs e);

    public class RotateGrabAttach : VRTK_BaseGrabAttach
    {
        [Header("Detach Settings")]

        [Tooltip("The maximum distance the grabbing object is away from the Interactable Object before it is automatically dropped.")]
        public float detachDistance = 1f;
        [Tooltip("The distance between grabbing object and the centre of Interactable Object that is considered to be non grabbable. If the grabbing object is within the `Origin Deadzone` distance then it will be automatically ungrabbed.")]
        public float originDeadzone = 0f;

        [Header("Rotation Settings")]

        [Tooltip("The amount of friction to apply when rotating, simulates a tougher rotation.")]
        [Range(1f, 32f)]
        public float rotationFriction = 1f;
        [Tooltip("The speed in which the Interactable Object returns to it's origin rotation when released. If the `Reset To Orign On Release Speed` is `0f` then the rotation will not be reset.")]
        public float resetToOrignOnReleaseSpeed = 0f;

        [Header("Rotation Limits")]

        [Tooltip("The negative and positive limits the X axis can be rotated to.")]
        public Limits2D xAngleLimits = new Limits2D(-180f, 180f);
        [Tooltip("The negative and positive limits the Y axis can be rotated to.")]
        public Limits2D yAngleLimits = new Limits2D(-180f, 180f);
        [Tooltip("The negative and positive limits the Z axis can be rotated to.")]
        public Limits2D zAngleLimits = new Limits2D(-180f, 180f);
        [Tooltip("The threshold the rotation value needs to be within to register a min or max rotation value.")]
        public float minMaxThreshold = 1f;
        [Tooltip("The threshold the normalized rotation value needs to be within to register a min or max normalized rotation value.")]
        [Range(0f, 0.99f)]
        public float minMaxNormalizedThreshold = 0.01f;

        public event RotateGrabAttachEventHandler RotationChanged;

        protected Vector3 initialAttachPointPosition;
        protected Vector3 currentAttachPointPosition;
        protected Bounds grabbedObjectBounds;
        protected Vector3 currentRotationSpeed;
        protected Coroutine updateRotationRoutine;
        protected VRTK_ControllerReference grabbingObjectReference;

        public virtual void OnAngleChanged(RotateGrabAttachEventArgs e)
        {
            if (RotationChanged != null)
            {
                RotationChanged(this, e);
            }
        }

        public override bool StartGrab(GameObject grabbingObject, GameObject givenGrabbedObject, Rigidbody givenControllerAttachPoint)
        {
            CancelUpdateRotation();
            bool grabResult = base.StartGrab(grabbingObject, givenGrabbedObject, givenControllerAttachPoint);
            initialAttachPointPosition  = currentAttachPointPosition = transform.parent.InverseTransformPoint(controllerAttachPoint.transform.position);
            grabbedObjectBounds = VRTK_SharedMethods.GetBounds(givenGrabbedObject.transform);
            CheckAngleLimits();
            grabbingObjectReference = VRTK_ControllerReference.GetControllerReference(grabbingObject);

            return grabResult;
        }

        public override void StopGrab(bool applyGrabbingObjectVelocity)
        {
            base.StopGrab(applyGrabbingObjectVelocity);
            if (resetToOrignOnReleaseSpeed > 0f)
            {
                ResetRotation();
            }
        }

        public override void ProcessUpdate()
        {
            if (trackPoint != null)
            {
                currentAttachPointPosition = transform.parent.InverseTransformPoint(controllerAttachPoint.transform.position);
                float distance = Vector3.Distance(transform.localPosition, currentAttachPointPosition);
                if (StillTouching() && distance >= originDeadzone)
                {
                    Vector3 newRotation = GetNewRotation();
                    currentRotationSpeed = newRotation - transform.localEulerAngles;
                    UpdateRotation(newRotation);
                }
            }
        }

        public virtual void ResetRotation(bool ignoreTransition = false)
        {
            if (resetToOrignOnReleaseSpeed > 0 && !ignoreTransition)
            {
                CancelUpdateRotation();
                updateRotationRoutine = StartCoroutine(RotateToAngle(Vector3.zero, resetToOrignOnReleaseSpeed));
            }
            else
            {
                UpdateRotation(Vector3.zero);
                currentRotationSpeed = Vector3.zero;
            }
        }

        protected virtual void OnDisable()
        {
            CancelUpdateRotation();
        }

        protected override void Initialise()
        {
            tracked = false;
            climbable = false;
            kinematic = true;
            precisionGrab = true;
            transform.localEulerAngles = Vector3.zero;
        }

        protected virtual Vector3 GetNewRotation()
        {
            Vector3 grabbingObjectAngularVelocity = Vector3.zero;
            if (VRTK_ControllerReference.IsValid(grabbingObjectReference))
            {
                grabbingObjectAngularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(grabbingObjectReference) * VRTK_SharedMethods.DividerToMultiplier(rotationFriction);
            }

            Vector3 direction = (currentAttachPointPosition - transform.localPosition);
            Vector3 newRotation = Quaternion.LookRotation(direction).eulerAngles;
            newRotation.z = transform.localEulerAngles.z + grabbingObjectAngularVelocity.z;

            return GetLimitedAngles(newRotation);
        }

        protected virtual float CalculateAngle(Vector3 originPoint, Vector3 previousPoint, Vector3 currentPoint, Vector3 direction)
        {
            Vector3 sideA = previousPoint - originPoint;
            Vector3 sideB = VRTK_SharedMethods.VectorDirection(originPoint, currentPoint);
            return AngleSigned(sideA, sideB, direction);
        }

        protected virtual void UpdateRotation(Vector3 newRotation)
        {
            var clampedRotation = ClampRotationWithinLimits(newRotation);
            transform.localEulerAngles = clampedRotation;
            OnAngleChanged(SetEventPayload());
            //Debug.Log($"New: {newRotation} Clamped: {clampedRotation}");
        }

        protected virtual Vector3 ClampRotationWithinLimits(Vector3 rotation)
        {
            rotation.x = Mathf.Clamp(rotation.x, xAngleLimits.minimum, xAngleLimits.maximum);
            rotation.y = Mathf.Clamp(rotation.y, yAngleLimits.minimum, yAngleLimits.maximum);
            rotation.z = Mathf.Clamp(rotation.z, zAngleLimits.minimum, zAngleLimits.maximum);
            return rotation;
        }

        protected virtual float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        protected virtual bool StillTouching()
        {
            float distance = Vector3.Distance(currentAttachPointPosition, initialAttachPointPosition);
            return (grabbedObjectBounds.Contains(currentAttachPointPosition) || distance <= detachDistance);
        }

        protected virtual void CancelUpdateRotation()
        {
            if (updateRotationRoutine != null)
            {
                StopCoroutine(updateRotationRoutine);
            }
        }

        protected virtual IEnumerator RotateToAngle(Vector3 targetAngle, float rotationSpeed)
        {
            currentRotationSpeed = Vector3.zero;
            while (GetLimitedAngles(transform.localEulerAngles) != targetAngle)
            {
                Vector3 newRotation = Vector3.Lerp(GetLimitedAngles(transform.localEulerAngles), targetAngle, rotationSpeed * Time.deltaTime);
                UpdateRotation(newRotation);
                yield return null;
            }
            UpdateRotation(targetAngle);
        }

        protected virtual Vector3 GetLimitedAngles(Vector3 rotation)
        {
            rotation.x = GetLimitedAngle(rotation.x);
            rotation.y = GetLimitedAngle(rotation.y);
            rotation.z = GetLimitedAngle(rotation.z);
            return rotation;
        }

        protected virtual float GetLimitedAngle(float angle)
        {
            return (angle > 180f ? angle - 360f : angle);
        }

        protected virtual void CheckAngleLimits()
        {
            xAngleLimits.minimum = (xAngleLimits.minimum > 0f ? xAngleLimits.minimum * -1f : xAngleLimits.minimum);
            xAngleLimits.maximum = (xAngleLimits.maximum < 0f ? xAngleLimits.maximum * -1f : xAngleLimits.maximum);
            yAngleLimits.minimum = (yAngleLimits.minimum > 0f ? yAngleLimits.minimum * -1f : yAngleLimits.minimum);
            yAngleLimits.maximum = (yAngleLimits.maximum < 0f ? yAngleLimits.maximum * -1f : yAngleLimits.maximum);
            zAngleLimits.minimum = (zAngleLimits.minimum > 0f ? zAngleLimits.minimum * -1f : zAngleLimits.minimum);
            zAngleLimits.maximum = (zAngleLimits.maximum < 0f ? zAngleLimits.maximum * -1f : zAngleLimits.maximum);
        }

        protected virtual RotateGrabAttachEventArgs SetEventPayload()
        {
            RotateGrabAttachEventArgs e;
            e.interactingObject = (grabbedObjectScript != null ? grabbedObjectScript.GetGrabbingObject() : null);
            e.currentRotation = GetLimitedAngles(transform.localEulerAngles);
            e.rotationSpeed = currentRotationSpeed;
            return e;
        }
    }
}