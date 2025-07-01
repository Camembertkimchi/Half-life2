using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Destiantion : MonoBehaviour
{
    public SceneManagerSingleton manager;
    [SerializeField] GameObject canvas;
    Color panelAlpha;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!canvas.activeSelf)
            {
                StartCoroutine(PanelAlphaCtrl());
                canvas.SetActive(true);
                Invoke(nameof(LoadMainScene), 2f);
            }
        }
    }

    IEnumerator PanelAlphaCtrl()
    {
        Color color = panelAlpha;
        while(panelAlpha.a < 254)
        {
            color.a -= 2 * Time.deltaTime;
            panelAlpha = color;
            yield return null;
        }
       
    }

    void LoadMainScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        manager.LoadTitle();
    }
}
