using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace putragonist_drawing_game { 
    /// <summary>
    /// Do Save Game
    /// </summary>
    public class SaveLoad
    {

        static string fileName = "savegame.sav";
        static BinaryFormatter b_formatter = new BinaryFormatter();

        /// <summary>
        /// Save Texture
        /// </summary>
        /// <param name="saveTexture">texture to be saved</param>
        public static void SaveTexture(Texture2D saveTexture)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "Save");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string dataPath = Path.Combine(folderPath, "save.sav");
            Debug.Log("Write on " + dataPath);
            File.WriteAllBytes(dataPath, saveTexture.EncodeToPNG());
        }

        /// <summary>
        /// Load Texture
        /// </summary>
        /// <returns>Texture2D</returns>
        public static Texture2D LoadTextureToFile()
        {
            byte[] bytes;
            string folderPath = Path.Combine(Application.persistentDataPath, "Save");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string dataPath = Path.Combine(folderPath, "save.sav");
            Debug.Log("Load on " + dataPath);
            bytes = System.IO.File.ReadAllBytes(dataPath);
            Texture2D text = new Texture2D(1, 1);
            text.LoadImage(bytes);
            return text;
        }
    }
}