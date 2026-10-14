using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundRepeat : MonoBehaviour
{
    private float spriteWidth;  // ancho del sprite

    void Start()
    {
        BoxCollider2D groundCollider = GetComponent<BoxCollider2D>(); // acá llamo al componente BoxCollider2D del suelo
        spriteWidth = groundCollider.size.x; // acá le digo que el ancho del sprite es igual al ancho del collider
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.x < -spriteWidth) // acá le digo que si la posición del suelo es menor al ancho del sprite, se repita
        {
            transform.Translate(Vector2.right * spriteWidth * 2f); // acá le digo que se repita
        }
    }
}
