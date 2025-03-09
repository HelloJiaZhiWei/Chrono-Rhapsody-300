using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("游戏状态")]
    [SerializeField] private GameStates _currentState = GameStates.GamePlay;
    public GameStates CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnGameStateChanged?.Invoke(value);
            }
        }
    }
    public delegate void GameStateHandler(GameStates newState);
    public event GameStateHandler OnGameStateChanged;
    public GameObject player;
    [Header("游戏时间")]
    [SerializeField] private float _gameTime;
    public float GameTime => _gameTime;

    protected override void Awake()
    {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player");
        InitializeManagers();
        ResetGameTime();
    }
    void Update()
    {
        if (CurrentState == GameStates.GamePlay)
        {
            _gameTime += Time.deltaTime;
        }
    }

    void Start()
    {

    }
    void InitializeManagers()
    {
        if (FindObjectOfType<EventManager>() == null)
        {
            gameObject.AddComponent<EventManager>();
        }
    }

    public void StartNewGame()
    {
        ResetGameTime();
        SetGameState(GameStates.GamePlay);
    }
    public void PauseGame()
    {
        if (CurrentState == GameStates.GamePlay)
        {
            SetGameState(GameStates.GamePause);
        }
    }
    public void ResumeGame()
    {
        if (CurrentState == GameStates.GamePause)
        {
            SetGameState(GameStates.GamePlay);
        }
    }
    public void EndGame()
    {
        SetGameState(GameStates.GameStop);
    }
    private void ResetGameTime()
    {
        _gameTime = 0f;
    }
    private void SetGameState(GameStates newState)
    {
        CurrentState = newState;
    }
}
