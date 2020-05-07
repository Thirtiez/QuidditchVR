using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class QuaffleSpawn : MonoBehaviour
{
    [SerializeField] private VRTK_ControllerEvents controllerEvents;
    [SerializeField] private QuaffleController Quaffle;
    [SerializeField] private Rigidbody playerRigidbody;

    private QuaffleController currentQuaffle;

    public void Awake()
    {
        controllerEvents = controllerEvents ?? VRTK_DeviceFinder.GetControllerRightHand().GetComponentInChildren<VRTK_ControllerEvents>();
        controllerEvents.ButtonTwoPressed += ButtonTwoPressed;
    }

    private void ButtonTwoPressed(object sender, ControllerInteractionEventArgs e)
    {
        DestroyQuaffle();
        SpawnQuaffle();
    }
    public void DestroyQuaffle()
    {
        if (currentQuaffle != null)
        {
            Destroy(currentQuaffle.gameObject);
        }
    }

    public void SpawnQuaffle()
    {
        currentQuaffle = Instantiate(Quaffle, transform);
        currentQuaffle.SetPlayerRigidbody(playerRigidbody);
    }
}
