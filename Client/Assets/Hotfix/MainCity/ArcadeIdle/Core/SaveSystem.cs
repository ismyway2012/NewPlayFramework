using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public static class SaveSystem
    {
        /// <summary>
        /// Saves data to a file in binary format.
        /// The data is serialized and stored in the application's persistent data path.
        /// </summary>
        /// <typeparam name="T">The type of the data to be saved.</typeparam>
        /// <param name="data">The data to be saved.</param>
        /// <param name="fileName">The name of the file to save the data in.</param>
        public static void SaveData<T>(T data, string fileName)
        {
            // Determine the file path where the data will be saved
            string filePath = Application.persistentDataPath + "/" + fileName + ".dat";

            // Create a BinaryFormatter to serialize the data
            BinaryFormatter formatter = new BinaryFormatter();

            // Create a file stream for writing the data
            FileStream fileStream = new FileStream(filePath, FileMode.Create);

            // Serialize the data and save it to the file
            formatter.Serialize(fileStream, data);
            fileStream.Close();
        }

        /// <summary>
        /// Loads data from a file in binary format.
        /// If the file doesn't exist, the default value of the data type is returned.
        /// </summary>
        /// <typeparam name="T">The type of the data to be loaded.</typeparam>
        /// <param name="fileName">The name of the file to load the data from.</param>
        /// <returns>The loaded data, or the default value if the file doesn't exist.</returns>
        public static T LoadData<T>(string fileName)
        {
            // Determine the file path from which the data will be loaded
            string filePath = Application.persistentDataPath + "/" + fileName + ".dat";

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Create a BinaryFormatter to deserialize the data
                BinaryFormatter formatter = new BinaryFormatter();

                // Open the file stream for reading
                FileStream fileStream = new FileStream(filePath, FileMode.Open);

                // Deserialize the data and cast it to the correct type
                T data = (T)formatter.Deserialize(fileStream);
                fileStream.Close();

                return data;
            }
            else
            {
                // Optional:
                // Debug.LogError("Save file not found in " + filePath);

                // Return default value if file doesn't exist
                return default(T);
            }
        }

        /// <summary>
        /// Retrieves the name of the most recently modified save file (without the extension).
        /// </summary>
        /// <returns>The name of the latest save file without its extension, or <c>null</c> if no save files are found.</returns>
        public static string GetLatestSaveFileName()
        {
            // Get all .dat save files from the persistent data path
            var saveFiles = Directory.GetFiles(Application.persistentDataPath, "*.dat");

            // Order the files by their last write time in descending order and return the file name without the extension
            return saveFiles.OrderByDescending(File.GetLastWriteTime)
                            .Select(Path.GetFileNameWithoutExtension)
                            .FirstOrDefault(); // Returns null if no save files are found
        }
    }
}
