using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Trello_Report.Runtime
{
    [Serializable]
    public class TrelloCardReporter
    {
        public string url="https://api.trello.com/1/cards";
        public string apiKey;
        public string token;

        public IEnumerator CreateTrelloCard(string cardTitle, string description, string listId,
            Action<bool, string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("name", cardTitle);
            form.AddField("desc", description);
            form.AddField("idList", listId);
            form.AddField("keepFromSource", "all");
            form.AddField("key", apiKey);
            form.AddField("token", token);

            using UnityWebRequest www = UnityWebRequest.Post(url, form);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Connection error or protocol error: " + www.error);
                callback(false, null);
            }
            else
            {
                string responseText = www.downloadHandler.text;
                var responseJson = JsonUtility.FromJson<TrelloCardResponse>(responseText);
                callback(true, responseJson.id);
            }
        }

        public IEnumerator UploadScreenshot(string cardId, byte[] screenshotBytes, Action<bool> callback)
        {
            WWWForm imageForm = new WWWForm();
            imageForm.AddBinaryData("file", screenshotBytes, "screenshot.png", "image/png");
            imageForm.AddField("key", apiKey);
            imageForm.AddField("token", token);

            string imageUrl = $"{url}/{cardId}/attachments";

            using UnityWebRequest imageUploadRequest = UnityWebRequest.Post(imageUrl, imageForm);
            yield return imageUploadRequest.SendWebRequest();

            if (imageUploadRequest.result == UnityWebRequest.Result.ConnectionError ||
                imageUploadRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Image upload error: " + imageUploadRequest.error);
                callback(false);
            }
            else
            {
                Debug.Log("Screenshot uploaded to Trello successfully!");
                callback(true);
            }
        }

        [Serializable]
        private class TrelloCardResponse
        {
            public string id;
        }
    }
}