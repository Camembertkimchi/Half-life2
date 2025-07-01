using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerSingleton : MonoBehaviour
{
    public static SceneManagerSingleton Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 현재 씬 다시 로드
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadStage()
    {
        SceneManager.LoadScene("StageScene");
    }
    public void LoadTitle()
    {
        SceneManager.LoadScene("Main");
    }
  
}