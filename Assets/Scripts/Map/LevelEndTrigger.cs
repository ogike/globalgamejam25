using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Player")
        {
            StartCoroutine(LevelEnd());
        }
    }

    private IEnumerator LevelEnd()
    {
        SfxManager.Instance.RefillSound();

        GameManager.Instance.FadeOut();
        yield return new WaitForSeconds(GameManager.Instance.fadeOutTime);

        int curScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(curScene + 1);
    }
}
