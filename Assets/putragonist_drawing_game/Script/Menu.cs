using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace putragonist_drawing_game
{
    public class Menu : MonoBehaviour
    {
        /// <summary>
        /// Loading previous save
        /// </summary>
        public void DoLoad()
        {
            Info.isLoad = true;
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Go Back To Menu
        /// </summary>
        public void GoToMenu()
        {
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Empty drawing scene
        /// </summary>
        public void NewGame()
        {
            SceneManager.LoadScene(1);
        }

        /// <summary>
        /// Exit the game
        /// </summary>
        public void Exit()
        {
            Application.Quit();
        }
    }
}
