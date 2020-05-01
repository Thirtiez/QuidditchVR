namespace QuidditchVR
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class HUDController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Text speedText;
        [SerializeField] private Text angularVelocityText;

        public void Awake()
        {
            playerController.VelocityChanged += VelocityChanged;
            playerController.AngularVelocityChanged += AngularVelocityChanged;
        }

        private void VelocityChanged(Vector3 currentVelocity)
        {
            speedText.text = $"{currentVelocity.magnitude.ToString("0.0")} m/s";
        }

        private void AngularVelocityChanged(Vector3 currentAngularVelocity)
        {
            angularVelocityText.text = $"{currentAngularVelocity}";
        }
    }
}
