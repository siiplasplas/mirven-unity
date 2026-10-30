using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mirven.Unity.Editor
{
    /// <summary>
    /// Mirven adresi ve yerel anahtar dosyası. Sıra: paneldeki elle ayar (EditorPrefs, bu proje için) →
    /// UserSettings/Mirven.json (Mirven'in kurulum komutu yazar; Unity'nin .gitignore'unda) → varsayılan.
    /// Anahtarın kendisi hiçbir yere kopyalanmaz; her istekte Mirven'in data/.anahtar dosyasından okunur.
    /// </summary>
    public static class MirvenAyar
    {
        public const string VarsayilanAdres = "http://127.0.0.1:8791";
        public const string PaketAdi = "com.mirven.unity";
        public const string PaketSurum = "0.1.0";

        public static string ProjeKoku => Normal(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
        static string DosyaYolu => Path.Combine(ProjeKoku, "UserSettings", "Mirven.json");
        static string Onek
        {
            get
            {
                uint h = 2166136261;   // FNV-1a: string.GetHashCode oturumlar arasında sabit sayılmaz
                foreach (var c in ProjeKoku) h = (h ^ c) * 16777619;
                return "Mirven." + h.ToString("x8") + ".";
            }
        }

        public static string Adres
        {
            get
            {
                var el = EditorPrefs.GetString(Onek + "adres", "");
                if (!string.IsNullOrWhiteSpace(el)) return el.Trim().TrimEnd('/');
                var d = Json.Dize(Dosya(), "adres");
                return string.IsNullOrWhiteSpace(d) ? VarsayilanAdres : d.Trim().TrimEnd('/');
            }
            set => EditorPrefs.SetString(Onek + "adres", value ?? "");
        }

        public static string AnahtarDosyasi
        {
            get
            {
                var el = EditorPrefs.GetString(Onek + "anahtar", "");
                return !string.IsNullOrWhiteSpace(el) ? el.Trim() : Json.Dize(Dosya(), "anahtar_dosyasi");
            }
            set => EditorPrefs.SetString(Onek + "anahtar", value ?? "");
        }

        public static bool ElleAyarli =>
            !string.IsNullOrWhiteSpace(EditorPrefs.GetString(Onek + "adres", "")) ||
            !string.IsNullOrWhiteSpace(EditorPrefs.GetString(Onek + "anahtar", ""));

        public static bool KurulumDosyasiVar => File.Exists(DosyaYolu);

        public static void ElleAyariSil()
        {
            EditorPrefs.DeleteKey(Onek + "adres");
            EditorPrefs.DeleteKey(Onek + "anahtar");
        }

        /// <summary>Anahtar yoksa boş döner; Mirven'de hesap yoksa anahtarsız istek de geçer.</summary>
        public static string Anahtar()
        {
            var yol = AnahtarDosyasi;
            if (string.IsNullOrWhiteSpace(yol)) return "";
            try { return File.Exists(yol) ? File.ReadAllText(yol).Trim() : ""; }
            catch (Exception) { return ""; }
        }

        static object Dosya()
        {
            try { return File.Exists(DosyaYolu) ? Json.Oku(File.ReadAllText(DosyaYolu)) : null; }
            catch (Exception) { return null; }
        }

        public static string Normal(string yol)
        {
            if (string.IsNullOrEmpty(yol)) return "";
            var y = yol.Replace('\\', '/').TrimEnd('/');
            return Application.platform == RuntimePlatform.WindowsEditor ? y.ToLowerInvariant() : y;
        }
    }
}
