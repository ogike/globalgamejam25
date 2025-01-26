using System;
using Player;
using UnityEngine;

namespace Map
{
    public class BubbleRefillZone : MonoBehaviour
    {
        public int amount;
        
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.tag == "Player")
            {
                PlayerMovement.Instance.StartRefillDash(amount);
                SfxManager.Instance.RefillSound();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.tag == "Player")
            {
                PlayerMovement.Instance.StopRefillDash();
                SfxManager.Instance.StopRefillSound();
            }
        }
    }
}
