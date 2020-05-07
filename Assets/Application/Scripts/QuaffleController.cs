using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using VRTK.GrabAttachMechanics;

public class QuaffleController : MonoBehaviour
{
    [SerializeField] private VRTK_InteractableObject interactableObject;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private float spawnForce = 10.0f;

    private Rigidbody playerRigidbody;

    public void Awake()
    {
        interactableObject = interactableObject ?? GetComponent<VRTK_InteractableObject>();
        rigidbody = rigidbody ?? GetComponent<Rigidbody>();
        trailRenderer = trailRenderer ?? GetComponent<TrailRenderer>();

        interactableObject.InteractableObjectGrabbed += InteractableObjectGrabbed;
        interactableObject.InteractableObjectUngrabbed += InteractableObjectUngrabbed;
    }

    public void Start()
    {
        SpawnThrow();
    }

    public void SetPlayerRigidbody(Rigidbody playerRigidbody)
    {
        this.playerRigidbody = playerRigidbody;
    }

    private void SpawnThrow()
    {
        rigidbody.AddRelativeForce(Vector3.up * spawnForce);
        InheritVelocity();
    }

    private void InheritVelocity()
    {
        rigidbody.velocity += playerRigidbody.velocity;
    }

    private void InteractableObjectGrabbed(object sender, InteractableObjectEventArgs e)
    {
        trailRenderer.enabled = false;
    }

    private void InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
    {
        trailRenderer.enabled = true;
        InheritVelocity();
    }
}
