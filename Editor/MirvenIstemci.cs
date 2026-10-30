using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine.Networking;

namespace Mirven.Unity.Editor
{
    public sealed class Yanit
    {
        public long Kod;
        public object Veri;
        public string Hata = "";
        public bool Tamam => string.IsNullOrEmpty(Hata);
    }

    /// <summary>Mirven'in yerel HTTP API'si. İstekler editörü kilitlemez; sonuç EditorApplication.update içinde döner.</summary>
    public static class MirvenIstemci
    {
        const int ZamanAsimi = 8;
        static readonly List<(UnityWebRequest istek, Action<Yanit> bitince)> Bekleyen = new List<(UnityWebRequest, Action<Yanit>)>();

        public static void Iste(string yontem, string yol, object govde, Action<Yanit> bitince)
        {
            var istek = Hazirla(yontem, yol, govde);
            istek.SendWebRequest();
            if (Bekleyen.Count == 0) EditorApplication.update += Yokla;
            Bekleyen.Add((istek, bitince));
        }

        /// <summary>Bekleyerek ister. Yalnız batchmode doğrulaması ve -executeMethod için.</summary>
        public static Yanit IsteBekle(string yontem, string yol, object govde = null)
        {
            using (var istek = Hazirla(yontem, yol, govde))
            {
                istek.SendWebRequest();
                while (!istek.isDone) Thread.Sleep(20);
                return Coz(istek);
            }
        }

        static UnityWebRequest Hazirla(string yontem, string yol, object govde)
        {
            var istek = new UnityWebRequest(MirvenAyar.Adres + yol, yontem) { downloadHandler = new DownloadHandlerBuffer(), timeout = ZamanAsimi };
            if (govde != null)
            {
                istek.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Yaz(govde)));
                istek.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            }
            var anahtar = MirvenAyar.Anahtar();
            if (anahtar.Length > 0) istek.SetRequestHeader("X-Mirven-Anahtar", anahtar);
            return istek;
        }

        static void Yokla()
        {
            for (var i = Bekleyen.Count - 1; i >= 0; i--)
            {
                var (istek, bitince) = Bekleyen[i];
                if (!istek.isDone) continue;
                Bekleyen.RemoveAt(i);
                var y = Coz(istek);
                istek.Dispose();
                try { bitince?.Invoke(y); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
            if (Bekleyen.Count == 0) EditorApplication.update -= Yokla;
        }

        static Yanit Coz(UnityWebRequest istek)
        {
            var y = new Yanit { Kod = istek.responseCode };
            if (istek.result == UnityWebRequest.Result.ConnectionError)
            {
                y.Hata = "Mirven'e ulaşılamadı (" + MirvenAyar.Adres + "). Mirven açık mı?";
                return y;
            }
            try { y.Veri = Json.Oku(istek.downloadHandler.text); }
            catch (FormatException) { y.Veri = null; }
            if (y.Kod >= 200 && y.Kod < 300) return y;
            var sunucu = Json.Dize(y.Veri, "hata");
            if (y.Kod == 401)
                y.Hata = "Mirven giriş istiyor: yerel anahtar bulunamadı ya da yanlış. Mirven'in kurulum komutunu çalıştır ya da Ayarlar'dan data/.anahtar dosyasını seç.";
            else if (y.Kod == 403 && Json.Dogru(y.Veri, "kurulum"))
                y.Hata = "Mirven'in ilk kurulumu bitmemiş.";
            else
                y.Hata = string.IsNullOrEmpty(sunucu) ? "Mirven " + y.Kod + " döndü." : sunucu;
            return y;
        }
    }
}
