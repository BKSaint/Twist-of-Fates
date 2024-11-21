using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CardSelect : MonoBehaviour
{
    public static event Action<int> OnCardClicked;
    public int cardIndex;
    
    void OnMouseDown()
    {
        OnCardClicked?.Invoke(cardIndex);
    }

    //private void DetectHover()
    //{
    //    // Create a ray from the mouse position
    //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    RaycastHit hit;

    //    // Perform the raycast
    //    if (Physics.Raycast(ray, out hit))
    //    {
    //        // Check if the object hit by the ray has a collider
    //        if (hit.collider != null)
    //        {
    //            Debug.Log($"Mouse is hovering over: {hit.collider.gameObject.name}");
    //        }
    //    }
    //}
}