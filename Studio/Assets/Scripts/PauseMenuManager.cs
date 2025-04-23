using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button returnToMainMenuButton;
    [SerializeField] private Button exitRoomButton;
    [SerializeField] private Button settingsButton;
    
    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button backFromSettingsButton;
    
    private bool isPaused = false;
    private bool isInSettings = false;
    
    private void Awake()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
            
        // Add listeners to buttons
        if (returnToMainMenuButton != null)
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
        if (exitRoomButton != null)
            exitRoomButton.onClick.AddListener(ExitRoom);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
            
        if (backFromSettingsButton != null)
            backFromSettingsButton.onClick.AddListener(CloseSettings);
    }
    
    private void Update()
    {
        // Check for Esc key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInSettings)
            {
                // If in settings panel, close it and return to pause menu
                CloseSettings();
            }
            else
            {
                // Toggle pause state
                TogglePause();
            }
        }
    }
    
    private void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }
    
    private void PauseGame()
    {
        Time.timeScale = 0f; // Freeze the game
        pauseMenuPanel.SetActive(true);
        
        // Optional: Play pause sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("PauseMenu");
        }
    }
    
    private void ResumeGame()
    {
        Time.timeScale = 1f; // Resume normal time
        pauseMenuPanel.SetActive(false);
        
        // Optional: Play resume sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("ResumeGame");
        }
    }
    
    private void ReturnToMainMenu()
    {
        // Resume normal time scale before loading new scene
        Time.timeScale = 1f;
        
        // Load main menu scene - replace "MainMenu" with your actual scene name
        SceneManager.LoadScene("Start");
    }
    
    private void ExitRoom()
    {
        // Resume normal time scale before loading new scene
        Time.timeScale = 1f;
        
        // This would typically return to a lobby or previous level
        // Replace "Lobby" with your actual scene name
        SceneManager.LoadScene("Lobby");
    }
    
    private void OpenSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        isInSettings = true;
    }
    
    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
        isInSettings = false;
    }
    
    // Public method to force unpause (useful when called from other scripts)
    public void ForceUnpause()
    {
        if (isPaused)
        {
            isPaused = false;
            ResumeGame();
        }
        
        if (isInSettings)
        {
            isInSettings = false;
            settingsPanel.SetActive(false);
        }
    }
} 