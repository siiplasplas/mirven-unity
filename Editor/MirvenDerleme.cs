using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Mirven.Unity.Editor
{
    /// <summary>
    /// Derleme hatalarını CompilationPipeline'dan toplar ve Mirven'e `POST /api/m1/studyo/&lt;pid&gt;/derleme` ile gönderir.
    /// Başarılı derleme alan yeniden yüklemesiyle biter; rapor SessionState'te durur, gönderim yüklemeden sonra yapılır.
    /// Bu paketin kendi assembly'si ayrı olduğundan projedeki kod derlenmese de çalışır.
    /// </summary>
    [InitializeOnLoad]
    public static class MirvenDerleme
    {
        const string RaporAnahtari = "Mirven.Derleme.Rapor";
        const string BekleyenAnahtari = "Mirven.Derleme.Bekleyen";
        const string GonderimAnahtari = "Mirven.Derleme.Gonderim";
        const string PanelAnahtari = "Mirven.Derleme.PanelBildirildi";
        const int HataSiniri = 200;

        static readonly List<object> Toplanan = new List<object>();
        static int uyari;
        static bool gonderiliyor;

        public static event Action Degisti;

        static MirvenDerleme()
        {
            CompilationPipeline.compilationStarted += _ => { Toplanan.Clear(); uyari = 0; };
            CompilationPipeline.assemblyCompilationFinished += AssemblyBitti;
            CompilationPipeline.compilationFinished += _ => Bitti();
            if (!Application.isBatchMode) EditorApplication.delayCall += () => MirvenBaglanti.Yenile();
        }

        /// <summary>Son derlemenin raporu (bu editör oturumunda). Yoksa null.</summary>
        public static Dictionary<string, object> Son
        {
            get
            {
                var m = SessionState.GetString(RaporAnahtari, "");
                try { return m.Length > 0 ? Json.Oku(m) as Dictionary<string, object> : null; }
                catch (FormatException) { return null; }
            }
        }

        public static string GonderimDurumu => SessionState.GetString(GonderimAnahtari, "");

        /// <summary>Proje kodu derlenemiyor ama hatalar bu oturumda okunmadı (editör açılırken olur).</summary>
        public static bool AyrintisizHata => Son == null && EditorUtility.scriptCompilationFailed;

        public static void YenidenDerle() =>
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);

        static void AssemblyBitti(string assembly, CompilerMessage[] mesajlar)
        {
            foreach (var m in mesajlar)
            {
                if (m.type == CompilerMessageType.Warning) { uyari++; continue; }
                if (m.type != CompilerMessageType.Error || Toplanan.Count >= HataSiniri) continue;
                Toplanan.Add(new Dictionary<string, object>
                {
                    ["dosya"] = (m.file ?? "").Replace('\\', '/'),
                    ["satir"] = m.line,
                    ["sutun"] = m.column,
                    ["mesaj"] = Konumsuz(m.message ?? ""),
                    ["assembly"] = System.IO.Path.GetFileNameWithoutExtension(assembly ?? ""),
                });
            }
        }

        /// <summary>"Assets\A.cs(1,41): error CS1002: ; expected" → "CS1002: ; expected". Dosya ve satır zaten ayrı alanda.</summary>
        static string Konumsuz(string mesaj)
        {
            var k = Konum.Match(mesaj);
            return k.Success ? mesaj.Substring(k.Length) : mesaj;
        }

        static readonly System.Text.RegularExpressions.Regex Konum =
            new System.Text.RegularExpressions.Regex(@"^.*?\(\d+,\d+\): (?:error|warning) ");

        static void Bitti()
        {
            var rapor = new Dictionary<string, object>
            {
                ["zaman"] = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0),
                ["hatalar"] = new List<object>(Toplanan),
                ["uyari"] = uyari,
            };
            SessionState.SetString(RaporAnahtari, Json.Yaz(rapor));
            SessionState.SetBool(BekleyenAnahtari, true);
            Degisti?.Invoke();
            // Hata varsa alan yeniden yüklenmez: hemen gönder. Yoksa yükleme sonrası bağlantı gönderir.
            if (Toplanan.Count > 0) EditorApplication.delayCall += BekleyeniGonder;
        }

        /// <summary>
        /// Bekleyen raporu gönderir. Bu oturumda Mirven'e hiç bildirilmediyse rapor olmasa da gönderir
        /// (hatalar=null): Mirven panelin bağlı olduğunu ve Unity sürümünü görür, eski derleme kaydına dokunmaz.
        /// </summary>
        public static void BekleyeniGonder()
        {
            var bekleyen = SessionState.GetBool(BekleyenAnahtari, false);
            var ilk = !SessionState.GetBool(PanelAnahtari, false);
            if (gonderiliyor || (!bekleyen && !ilk)) return;
            if (MirvenBaglanti.Pid == "") { MirvenBaglanti.Yenile(); return; }
            gonderiliyor = true;
            var son = bekleyen ? Son : null;
            MirvenIstemci.Iste("POST", "/api/m1/studyo/" + Uri.EscapeDataString(MirvenBaglanti.Pid) + "/derleme", Govde(son), y =>
            {
                gonderiliyor = false;
                if (y.Tamam)
                {
                    SessionState.SetBool(BekleyenAnahtari, false);
                    SessionState.SetBool(PanelAnahtari, true);
                    SessionState.SetString(GonderimAnahtari, "Mirven'e gönderildi · " + DateTime.Now.ToString("HH:mm:ss"));
                }
                else
                {
                    SessionState.SetString(GonderimAnahtari, "Gönderilemedi: " + y.Hata);
                }
                Degisti?.Invoke();
            });
        }

        /// <summary>Gönderim gövdesi. son null ise yalnız panel/Unity bilgisi gider.</summary>
        public static Dictionary<string, object> Govde(Dictionary<string, object> son) => new Dictionary<string, object>
        {
            ["unity"] = Application.unityVersion,
            ["paket"] = MirvenAyar.PaketSurum,
            ["hatalar"] = son == null ? null : son["hatalar"],
            ["uyari"] = son == null ? 0 : son["uyari"],
            ["derleme_zamani"] = son == null ? null : son["zaman"],
            ["derlenemiyor"] = EditorUtility.scriptCompilationFailed,
        };
    }
}
