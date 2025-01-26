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

        if (SceneManager.loadedSceneCount + 1 <= SceneManager.sceneCount)
        {
            SceneManager.LoadScene(SceneManager.loadedSceneCount + 1);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}
