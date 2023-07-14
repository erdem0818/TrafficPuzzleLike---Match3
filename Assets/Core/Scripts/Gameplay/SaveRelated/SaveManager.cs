using UnityEngine;

namespace Core.Scripts.Gameplay.SaveRelated
{
    public static class SaveManager
    {
        /// <summary>
        /// Invokes this event when new or existing data is written.
        /// </summary>
        public static event System.Action<string, string> OnKeyAdded;

        /// <summary>
        /// Invokes this event when a key is removed from the dictionary.
        /// </summary>
        public static event System.Action<string> OnKeyRemoved;

        /// <summary>
        /// The function to save data.
        /// </summary>
        /// <param name="data">The actual data to be saved</param>
        /// <param name="key">The key to getting data.</param>
        /// <typeparam name="T"></typeparam>
        public static void SaveData<T>(T data, string key)
        {
            if (HasKey(key))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"The key ''{key}'' you wanted to add is already exists - this key's value is overwritten");
#endif
                DeleteSave(key);

                WriteData(data, key);
                return;
            }

            WriteData(data, key);
        }

        private static void WriteData<T>(T data, string key)
        {
            string serializedData = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, serializedData);
            OnKeyAdded?.Invoke(key, serializedData);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The function to get data if data exists or creates if it does not exist.
        /// </summary>
        /// <param name="key">The key used for registration.</param>
        /// <param name="defVal">The default value if there is no key</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetOrCreateData<T>(string key, T defVal = default) where T : new()
        {
            T data;
            if (HasKey(key))
            {
                data = GetExistingData<T>(key);
                return data;
            }

            data = defVal;
            WriteData(data, key);
            return data;
        }

        private static T GetExistingData<T>(string key) where T : new()
        {
            if (!HasKey(key))
            {
#if UNITY_EDITOR
                Debug.LogWarning("There is no save with saved with this key");
#endif
                return default;
            }

            string valueString = PlayerPrefs.GetString(key);
            T data = JsonUtility.FromJson<T>(valueString);
            return data;
        }

        /// <summary>
        /// The function to check if the key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        ///<summary>
        /// Deletes all saves.
        ///</summary>
        public static void DeleteAllSaves()
        {
            PlayerPrefs.DeleteAll();
        }

        /// <summary>
        /// Deletes all keys from dictionary.
        /// </summary>
        /// <param name="key">The key used for registration.</param>
        public static void DeleteSave(string key)
        {
            if (HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                OnKeyRemoved?.Invoke(key);
                return;
            }

#if UNITY_EDITOR
            Debug.LogWarning("The key you entered does not exist.");
#endif
        }
    }
}