using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } 
    
    [Header("Fade to black UI")]
    public Image fadeToBlackScreen;

    public float fadeInTime;
    public float fadeOutTime;
    public float fullBlackTime;

    public AnimationCurve fadeInCurve;
    public AnimationCurve fadeOutCurve;

    public int startingBubbleAmount;
    
    public bool IsInFailState { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple GameManagers in the scene!");
            return;
        }

        Instance = this;
        IsInFailState = false;
        SetFadeToBlackColor(0);
        FadeIn();
    }

    private void Start()
    {
        PlayerMovement.Instance.SetDashAmount(startingBubbleAmount);
    }

    public void SetFailState(bool value)
    {
        IsInFailState = value;
    }

    public void RetryAt(Vector3 position)
    {
        PlayerMovement.Instance.TeleportTo(position);
        PlayerMovement.Instance.SetDashAmount(startingBubbleAmount);
    }
    
    public void SetFadeToBlackColor(float opacity)
    {
        Color color = fadeToBlackScreen.color;
        color.a = opacity;
        fadeToBlackScreen.color = color;
    }
    
    public void FadeIn()
    {
        Debug.Log("Start fade in");
        StartCoroutine(FadeInCoroutine());
    }

    public IEnumerator FadeInCoroutine()
    {
        float time = 0.0f;
        while (time <= fadeInTime)
        {
            float opacity = fadeInCurve.Evaluate(time / fadeInTime);
            SetFadeToBlackColor(opacity);
            time += Time.deltaTime;
            yield return new WaitForSeconds(0);
        }
        SetFadeToBlackColor(0);
    }
    
    public void FadeOut()
    {
        Debug.Log("Start fade out");
        StartCoroutine(FadeOutCoroutine());
    }
    
    public IEnumerator FadeOutCoroutine()
    {
        float time = 0;
        while (time <= fadeOutTime)
        {
            float opacity = fadeOutCurve.Evaluate(time / fadeInTime);
            SetFadeToBlackColor(opacity);
            time += Time.deltaTime;
            yield return new WaitForSeconds(0);
        }
        SetFadeToBlackColor(1);
    }
}
