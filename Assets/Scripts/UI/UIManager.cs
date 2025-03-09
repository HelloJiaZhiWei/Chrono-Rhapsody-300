using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : Singleton<UIManager>
{
    [System.Serializable]
    public class HeartSprites
    {
        public Sprite empty;
        //public Sprite quarter;
        public Sprite half;
        //public Sprite threeQuarters;
        public Sprite full;
    }
    #region UI References
    [Header("UI Components")]
    [SerializeField] private Button menuButton;
    public GameObject heartPrefab;
    public Transform heartsContainer;
    public List<Image> healthBar;
    public HeartSprites heartSprites;
    [SerializeField] private Slider postureSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject pauseMenu;
    private PlayerCharacter player;
    #endregion

    #region Time Formatting
    private const float TotalTime = 300f; // 5分钟 = 300秒
    private string FormatTime(float seconds)
    {
        //int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds);
        int fraction = Mathf.FloorToInt((seconds * 100) % 100);
        //return $"{minutes:00}:{secs:00}.{fraction:00}";
        return $"{secs:000}.{fraction:00}";
    }
    #endregion

    #region Initialization
    private void Start()
    {
        player = GameManager.Instance.player.GetComponent<PlayerCharacter>();
        // 初始化组件
        menuButton.onClick.AddListener(TogglePauseMenu);
        pauseMenu.SetActive(false);
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        healthBar.Clear();
        for (int i = 0; i < Mathf.CeilToInt(player.maxHealth); i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            healthBar.Add(heart.GetComponent<Image>());
        }
        // 初始化血条
        UpdateHealthBar(player.maxHealth);
        UpdatePostureBar(player.CurrentPosture);
        // 注册事件
        GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
        player.OnHealthChanged += UpdateHealthBar;
    }
    #endregion

    #region Update Methods
    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameStates.GamePlay)
        {
            UpdateTimerDisplay();
            UpdatePostureBar(player.CurrentPosture);
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText)
        {
            float remainingTime = TotalTime - GameManager.Instance.GameTime;
            timerText.text = FormatTime(remainingTime);
        }
    }
    #endregion

    #region Event Handlers
    private void HandleGameStateChange(GameStates newState)
    {
        if (pauseMenu && menuButton)
        {
            pauseMenu.SetActive(newState == GameStates.GamePause);
            menuButton.interactable = newState != GameStates.GameStop;
        }

    }

    private void UpdateHealthBar(float currentHealth)
    {
        float remainingHealth = currentHealth;
        for (int i = 0; i < healthBar.Count; i++)
        {
            Image heartImage = healthBar[i];
            bool isLastHeart = i == healthBar.Count - 1;

            // 计算当前红心的血量值（0-1）
            float heartValue = Mathf.Clamp01(remainingHealth);
            remainingHealth -= 1;

            // 根据血量段设置不同精灵
            heartImage.sprite = GetHeartSprite(heartValue, isLastHeart);
        }
    }
    Sprite GetHeartSprite(float value, bool isLastHeart)
    {
        if (value >= 1) return heartSprites.full;
        if (!isLastHeart && value <= 0) return heartSprites.empty;

        float preciseValue = Mathf.Round(value * 2) / 2;

        return preciseValue switch
        {
            0.5f => heartSprites.half,
            0f => heartSprites.empty,
            _ => heartSprites.empty
        };
    }
    private void UpdatePostureBar(float currentPosture)
    {
        if (postureSlider)
        {
            postureSlider.value = currentPosture / player.MaxPostureValue;
        }
    }
    #endregion

    #region UI Control
    public void TogglePauseMenu()
    {
        if (pauseMenu)
        {
            bool shouldPause = !pauseMenu.activeSelf;
            pauseMenu.SetActive(shouldPause);
        }
        if (pauseMenu.activeSelf) GameManager.Instance.PauseGame();
        else GameManager.Instance.ResumeGame();
    }
    #endregion

    #region Cleanup
    protected override void OnDestroy()
    {
        if (!GameManager.Instance) return;
        GameManager.Instance.OnGameStateChanged -= HandleGameStateChange;
        player.OnHealthChanged -= UpdateHealthBar;
    }
    #endregion
}