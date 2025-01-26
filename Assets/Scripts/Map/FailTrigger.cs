using System.Collections;
using Player;
using UnityEngine;

namespace Map
{
    public class FailTrigger : MonoBehaviour
    {
        public Transform teleportTo;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.tag == "Player")
            {
                StartCoroutine(Fail());
            }
        }

        private IEnumerator Fail()
        {
            SfxManager.Instance.RetrySound();
            
            GameManager.Instance.SetFailState(true);
            GameManager.Instance.FadeOut();
            yield return new WaitForSeconds(GameManager.Instance.fadeOutTime);
            
            GameManager.Instance.RetryAt(teleportTo.position);
            yield return new WaitForSeconds(GameManager.Instance.fullBlackTime);

            GameManager.Instance.SetFailState(false);
            GameManager.Instance.FadeIn();
        }
    }
}
