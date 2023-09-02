using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostPad : MonoBehaviour
{
    public float boostSpeed = 35f;

    private void OnTriggerEnter(Collider other)
    {
        KartController kart = other.GetComponentInParent<KartController>();
        if(kart != null)
        {
            StartCoroutine(kart.Boost(boostSpeed));
        }
    }
}
