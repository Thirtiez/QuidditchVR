namespace QuidditchVR
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using VRTK;
    using VRTK.GrabAttachMechanics;

    public class BroomController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VRTK_InteractableObject interactableObject;
        [SerializeField] private RotateGrabAttach rotateGrabAttach;

        public UnityAction ThrottlePressed;
        public UnityAction ThrottleReleased;
        public UnityAction<Vector3> Steering;

        protected void Awake()
        {
            interactableObject = interactableObject ?? GetComponent<VRTK_InteractableObject>();
            rotateGrabAttach = rotateGrabAttach ?? GetComponent<RotateGrabAttach>();

            interactableObject.InteractableObjectUsed += InteractableObjectUsed;
            interactableObject.InteractableObjectUnused += InteractableObjectUnused;
            rotateGrabAttach.RotationChanged += RotationChanged;
        }

        private void InteractableObjectUsed(object sender, InteractableObjectEventArgs e)
        {
            ThrottlePressed?.Invoke();
        }

        private void InteractableObjectUnused(object sender, InteractableObjectEventArgs e)
        {
            ThrottleReleased?.Invoke();
        }

        private void RotationChanged(object sender, RotateGrabAttachEventArgs e)
        {
            Steering?.Invoke(e.currentRotation);
        }
    }
}