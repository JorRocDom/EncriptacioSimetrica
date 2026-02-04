using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

// Models de dades
[Serializable] public class TextData { public string text; public string encrypted; }
[Serializable] public class PasswordData { public string password; public string hash; public bool ok; }

public class ApiClient : MonoBehaviour
{
    private TextField _textInput, _passInput;
    private Label _resultLabel;
    private Button _btnEnc, _btnDec, _btnHash, _btnVer;
    private string _lastEncrypted;

    void OnEnable()
    {
        Debug.Log("🔌 ApiClient: Intentant connectar UI...");
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Buscar elements
        _textInput = root.Q<TextField>("TextInput");
        _passInput = root.Q<TextField>("PassInput");
        _resultLabel = root.Q<Label>("ResultLabel");
        
        _btnEnc = root.Q<Button>("BtnEncrypt");
        _btnDec = root.Q<Button>("BtnDecrypt");
        _btnHash = root.Q<Button>("BtnHash");
        _btnVer = root.Q<Button>("BtnVerify");

        // Verificació d'elements
        if (_btnEnc == null) Debug.LogError("❌ No s'ha trobat 'BtnEncrypt' al UXML!");
        else {
            Debug.Log("✅ Botons trobats. Assignant esdeveniments.");
            _btnEnc.clicked += OnEncrypt;
            _btnDec.clicked += OnDecrypt;
            _btnHash.clicked += OnHash;
            _btnVer.clicked += OnVerify;
        }
    }

    void OnDisable()
    {
        // Netegem per evitar l'error de "Invalid GC handle"
        if (_btnEnc != null) _btnEnc.clicked -= OnEncrypt;
        if (_btnDec != null) _btnDec.clicked -= OnDecrypt;
        if (_btnHash != null) _btnHash.clicked -= OnHash;
        if (_btnVer != null) _btnVer.clicked -= OnVerify;
    }

    private void OnEncrypt() => StartCoroutine(PostRequest("encrypt", new TextData { text = _textInput.value }));
    private void OnDecrypt() => StartCoroutine(PostRequest("decrypt", new TextData { encrypted = _lastEncrypted }));
    private void OnHash() => StartCoroutine(PostRequest("hash", new PasswordData { password = _passInput.value }));
    private void OnVerify() => StartCoroutine(PostRequest("verify", new PasswordData { password = _passInput.value }));

    IEnumerator PostRequest(string route, object data)
    {
        string url = $"http://localhost:3005/{route}";
        string json = JsonUtility.ToJson(data);
        Debug.Log($"📡 Enviant a {url}: {json}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error API ({route}): {request.error}");
                _resultLabel.text = "Error: " + request.error;
            }
            else
            {
                string response = request.downloadHandler.text;
                Debug.Log($"📥 Resposta rebuda ({route}): {response}");
                ProcessResponse(route, response);
            }
        }
    }

    void ProcessResponse(string route, string json)
    {
        if (route == "encrypt") {
            var d = JsonUtility.FromJson<TextData>(json);
            _lastEncrypted = d.encrypted;
            _resultLabel.text = "Encrypted: " + d.encrypted;
        } else if (route == "decrypt") {
            var d = JsonUtility.FromJson<TextData>(json);
            _resultLabel.text = "Decrypted: " + d.text;
        } else if (route == "hash") {
            var d = JsonUtility.FromJson<PasswordData>(json);
            _resultLabel.text = "Hash: " + d.hash;
        } else if (route == "verify") {
            var d = JsonUtility.FromJson<PasswordData>(json);
            _resultLabel.text = d.ok ? "✅ Password Correcte" : "❌ Incorrecte";
        }
    }
}