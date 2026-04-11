using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class RuntimeAudioClipLoader
    {
        public static async Task<AudioClip> LoadClipAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                return null;
            }

            AudioType audioType = getAudioType(filePath);

            if (audioType == AudioType.UNKNOWN)
            {
                Debug.LogWarning($"Unsupported audio format for '{filePath}'.");
                return null;
            }

            string absoluteUri = new Uri(filePath).AbsoluteUri;

            using UnityWebRequest unityWebRequest =
                UnityWebRequestMultimedia.GetAudioClip(absoluteUri, audioType);
            UnityWebRequestAsyncOperation requestOperation = unityWebRequest.SendWebRequest();

            while (requestOperation.isDone == false)
            {
                await Task.Yield();
            }

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"Failed to load audio clip from '{filePath}': {unityWebRequest.error}");
                return null;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(unityWebRequest);

            if (audioClip != null)
            {
                audioClip.name = Path.GetFileNameWithoutExtension(filePath);
            }

            return audioClip;
        }

        private static AudioType getAudioType(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath)?.ToLowerInvariant();

            return fileExtension switch
            {
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.UNKNOWN,
            };
        }
    }
}
