using System;
using UnityEngine;

public class DisplaceOnHit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Destroy(other.gameObject);
    }
}
