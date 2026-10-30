using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mirven.Unity.Editor
{
    /// <summary>
    /// Window ▸ Mirven. Bu projenin Mirven Stüdyo kaydını gösterir: istekler, işler, seçili işin dosyaları ve
    /// son derlemenin hataları. Projeye yazmaz; dosyaya tıklamak Project penceresinde ping atar.
    /// </summary>
    public sealed class MirvenPenceresi : EditorWindow
    {
        const double YenilemeSn = 15;
        static readonly Dictionary<string, string> IsDurum = new Dictionary<string, string>
        {
            ["yapilacak"] = "Yapılacak", ["suruyor"] = "Sürüyor", ["inceleme"] = "İncelemede", ["bitti"] = "Bitti",
        };
        static readonly Dictionary<string, string> IstekDurum = new Dictionary<string, string>
        {
            ["inceleniyor"] = "İnceleniyor", ["hazir"] = "Plan hazır", ["hata"] = "Hata",
        };
        static readonly Dictionary<string, string> MirvenDurum = new Dictionary<string, string>
        {
            ["calisiyor"] = "Mirven çalışıyor", ["bitti"] = "Mirven bitirdi, inceleme bekliyor",
            ["bos"] = "Mirven değişiklik yapmadı", ["hata"] = "Mirven işi hata verdi",
        };

        string seciliIstek = "";
        string seciliIs = "";
        Vector2 kaydirma;
        bool ayarAcik, hatalarAcik = true, isteklerAcik = true;
        double sonYenileme;
        string elAdres, elAnahtar;

        [MenuItem("Window/Mirven", false, 2000)]
        public static void Ac()
        {
            var w = GetWindow<MirvenPenceresi>();
            w.titleContent = new GUIContent("Mirven");
            w.minSize = new Vector2(320, 240);
            w.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Mirven");
            wantsMouseMove = true;
            MirvenBaglanti.Degisti += Repaint;
            MirvenDerleme.Degisti += Repaint;
            Yenile();
        }

        void OnDisable()
        {
            MirvenBaglanti.Degisti -= Repaint;
            MirvenDerleme.Degisti -= Repaint;
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup - sonYenileme > YenilemeSn) Yenile();
        }

        void Yenile()
        {
            sonYenileme = EditorApplication.timeSinceStartup;
            MirvenBaglanti.Yenile();
        }

        // ------------------------------------------------------------ çizim
        void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            Ust();
            if (ayarAcik) Ayarlar();
            kaydirma = EditorGUILayout.BeginScrollView(kaydirma);
            switch (MirvenBaglanti.Durum)
            {
                case BaglantiDurumu.Bekliyor:
                case BaglantiDurumu.Baglaniyor:
                    EditorGUILayout.HelpBox("Mirven'e bağlanılıyor: " + MirvenAyar.Adres, MessageType.None);
                    break;
                case BaglantiDurumu.Hata:
                    EditorGUILayout.HelpBox(MirvenBaglanti.Hata, MessageType.Warning);
                    break;
                case BaglantiDurumu.ProjeYok:
                    EditorGUILayout.HelpBox("Bu Unity projesi Mirven Stüdyo'ya bağlı değil. Mirven'de Stüdyo ▸ Proje bağla ile şu klasörü seç:\n"
                                            + Proje.Kok, MessageType.Info);
                    break;
            }
            Derleme();
            if (MirvenBaglanti.Durum == BaglantiDurumu.Baglandi && MirvenBaglanti.Proje != null)
            {
                Istekler();
                Isler();
                SeciliIs();
            }
            EditorGUILayout.EndScrollView();
        }

        void Ust()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var p = MirvenBaglanti.Proje;
                string baslik;
                switch (MirvenBaglanti.Durum)
                {
                    case BaglantiDurumu.Baglandi:
                        var tarama = Json.Dize(Json.Nesne(p, "tarama"), "durum");
                        baslik = "● " + Json.Dize(p, "ad") + (tarama == "taraniyor" ? " · taranıyor" : "");
                        break;
                    case BaglantiDurumu.ProjeYok: baslik = "○ Bağlı proje yok"; break;
                    case BaglantiDurumu.Hata: baslik = "○ Bağlantı yok"; break;
                    default: baslik = "… Bağlanıyor"; break;
                }
                GUILayout.Label(baslik, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (MirvenBaglanti.Durum == BaglantiDurumu.Baglandi)
                    GUILayout.Label(MirvenBaglanti.SonOkuma.ToString("HH:mm:ss"), EditorStyles.miniLabel);
                using (new EditorGUI.DisabledScope(MirvenBaglanti.Okunuyor))
                    if (GUILayout.Button("Yenile", EditorStyles.toolbarButton, GUILayout.Width(52))) Yenile();
                ayarAcik = GUILayout.Toggle(ayarAcik, "Ayarlar", EditorStyles.toolbarButton, GUILayout.Width(58));
            }
        }

        void Ayarlar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                elAdres = elAdres ?? MirvenAyar.Adres;
                elAnahtar = elAnahtar ?? MirvenAyar.AnahtarDosyasi;
                elAdres = EditorGUILayout.TextField("Mirven adresi", elAdres);
                using (new EditorGUILayout.HorizontalScope())
                {
                    elAnahtar = EditorGUILayout.TextField("Anahtar dosyası", elAnahtar);
                    if (GUILayout.Button("…", GUILayout.Width(24)))
                    {
                        var s = EditorUtility.OpenFilePanel("Mirven data/.anahtar", "", "");
                        if (!string.IsNullOrEmpty(s)) elAnahtar = s;
                    }
                }
                EditorGUILayout.LabelField("Kaynak", MirvenAyar.ElleAyarli ? "Elle ayar (bu bilgisayar)"
                    : MirvenAyar.KurulumDosyasiVar ? "UserSettings/Mirven.json (Mirven kurulumu)" : "Varsayılan", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (MirvenAyar.ElleAyarli && GUILayout.Button("Elle ayarı sil", GUILayout.Width(100)))
                    {
                        MirvenAyar.ElleAyariSil();
                        elAdres = elAnahtar = null;
                        Yenile();
                    }
                    if (GUILayout.Button("Kaydet", GUILayout.Width(70)))
                    {
                        if (elAdres.Trim().TrimEnd('/') != MirvenAyar.Adres) MirvenAyar.Adres = elAdres;
                        if (elAnahtar.Trim() != MirvenAyar.AnahtarDosyasi) MirvenAyar.AnahtarDosyasi = elAnahtar;
                        elAdres = elAnahtar = null;
                        Yenile();
                    }
                }
            }
        }

        void Derleme()
        {
            var son = MirvenDerleme.Son;
            var hatalar = son == null ? new List<object>() : Json.Liste(son, "hatalar");
            using (new EditorGUILayout.HorizontalScope())
            {
                string ozet;
                if (son != null) ozet = hatalar.Count == 0 ? "Derleme temiz" : "Derleme: " + hatalar.Count + " hata";
                else if (MirvenDerleme.AyrintisizHata) ozet = "Derleme hatası var (ayrıntı bu oturumda okunmadı)";
                else ozet = EditorApplication.isCompiling ? "Derleniyor…" : "Derleme: bu oturumda derleme olmadı";
                if (hatalar.Count > 0) hatalarAcik = EditorGUILayout.Foldout(hatalarAcik, ozet, true);
                else EditorGUILayout.LabelField(ozet);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
                    if (GUILayout.Button(new GUIContent("Yeniden derle", "Kodu yeniden derler; dosyalara dokunmaz."), EditorStyles.miniButton, GUILayout.Width(92)))
                        MirvenDerleme.YenidenDerle();
            }
            if (MirvenDerleme.GonderimDurumu != "")
                EditorGUILayout.LabelField(MirvenDerleme.GonderimDurumu, EditorStyles.miniLabel);
            if (!hatalarAcik || hatalar.Count == 0) return;
            foreach (var h in hatalar)
            {
                var dosya = Json.Dize(h, "dosya");
                var etiket = dosya + ":" + Json.Dize(h, "satir") + "  " + Json.Dize(h, "mesaj");
                if (GUILayout.Button(new GUIContent(etiket, etiket), Satir())) Ping(dosya);
            }
            EditorGUILayout.Space(4);
        }

        void Istekler()
        {
            var istekler = Json.Liste(MirvenBaglanti.Proje, "istekler");
            isteklerAcik = EditorGUILayout.Foldout(isteklerAcik, "İstekler (" + istekler.Count + ")", true, EditorStyles.foldoutHeader);
            if (!isteklerAcik) return;
            if (istekler.Count == 0) { EditorGUILayout.LabelField("Henüz istek yok.", EditorStyles.miniLabel); return; }
            if (SatirCiz(seciliIstek == "", "Tüm işler", "")) { seciliIstek = ""; }
            foreach (var i in istekler)
            {
                var id = Json.Dize(i, "id");
                var baslik = Json.Dize(Json.Nesne(i, "sonuc"), "baslik");
                if (baslik == "") baslik = Json.Dize(i, "cumle");
                var durum = Json.Sayi(i, "dagitildi") > 0 ? "İşlere bölündü" : Etiket(IstekDurum, Json.Dize(i, "durum"));
                if (SatirCiz(seciliIstek == id, baslik, durum)) seciliIstek = seciliIstek == id ? "" : id;
            }
            EditorGUILayout.Space(4);
        }

        void Isler()
        {
            var isler = Json.Liste(MirvenBaglanti.Proje, "isler");
            var gorunen = isler.FindAll(i => seciliIstek == "" || Json.Dize(i, "istek") == seciliIstek);
            EditorGUILayout.LabelField("İşler (" + gorunen.Count + ")", EditorStyles.boldLabel);
            if (gorunen.Count == 0)
            {
                EditorGUILayout.LabelField(isler.Count == 0 ? "Henüz iş yok. İstek işlere bölününce burada görünür." : "Bu istekte iş yok.", EditorStyles.miniLabel);
                return;
            }
            foreach (var i in gorunen)
            {
                var no = Json.Dize(i, "no");
                var kisi = Json.Dize(i, "kisi");
                var sag = Etiket(IsDurum, Json.Dize(i, "durum")) + (kisi != "" ? " · " + kisi : "");
                if (SatirCiz(seciliIs == no, no + "  " + Json.Dize(i, "ad"), sag)) seciliIs = seciliIs == no ? "" : no;
            }
            EditorGUILayout.Space(4);
        }

        void SeciliIs()
        {
            var i = Json.Liste(MirvenBaglanti.Proje, "isler").Find(x => Json.Dize(x, "no") == seciliIs);
            if (i == null) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(seciliIs + "  " + Json.Dize(i, "ad"), EditorStyles.wordWrappedLabel);
                var bilgi = new List<string>();
                foreach (var a in new[] { "sistem", "rol", "kisi" })
                    if (Json.Dize(i, a) != "") bilgi.Add(Json.Dize(i, a));
                if (Json.Sayi(i, "gun") > 0) bilgi.Add(Json.Sayi(i, "gun") + " gün");
                EditorGUILayout.LabelField(string.Join(" · ", bilgi), EditorStyles.miniLabel);
                var kabul = Json.Liste(i, "kabul");
                if (kabul.Count > 0)
                {
                    EditorGUILayout.LabelField("Kabul ölçütleri", EditorStyles.miniBoldLabel);
                    foreach (var k in kabul) EditorGUILayout.LabelField("• " + k, EditorStyles.wordWrappedMiniLabel);
                }

                var mirven = Json.Nesne(i, "mirven_is");
                var md = Json.Dize(mirven, "durum");
                if (md != "") EditorGUILayout.LabelField(Etiket(MirvenDurum, md), EditorStyles.miniBoldLabel);
                if (md == "hata" && Json.Dize(mirven, "hata") != "")
                    EditorGUILayout.LabelField(Json.Dize(mirven, "hata"), EditorStyles.wordWrappedMiniLabel);
                var degisen = Json.Liste(mirven, "dosyalar");   // studyo/<pid>/is/<no>/fark ile aynı liste
                if (degisen.Count > 0)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Mirven'in değiştirdiği dosyalar (dal " + Json.Dize(mirven, "dal") + ")", EditorStyles.miniBoldLabel);
                    DosyaListesi(degisen, true);
                }

                var plan = Json.Liste(i, "dosyalar");
                if (plan.Count > 0)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Plandaki dosyalar", EditorStyles.miniBoldLabel);
                    DosyaListesi(plan, false);
                }
            }
        }

        void DosyaListesi(List<object> dosyalar, bool fark)
        {
            var bulunan = new List<UnityEngine.Object>();
            foreach (var d in dosyalar)
            {
                var yol = fark ? Json.Dize(d, "yol") : d as string ?? "";
                var varlik = Varlik(yol);
                if (varlik != null) bulunan.Add(varlik);
                var sag = fark ? "+" + Json.Dize(d, "ek") + " −" + Json.Dize(d, "sil") : "";
                if (varlik == null) sag = (sag + " projede yok").Trim();
                using (new EditorGUI.DisabledScope(varlik == null))
                {
                    var ikon = varlik != null ? AssetDatabase.GetCachedIcon(VarlikYolu(yol)) : null;
                    if (SatirCiz(false, VarlikYolu(yol), sag, ikon)) Ping(yol);
                }
            }
            if (bulunan.Count > 1)
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Hepsini Project'te seç", EditorStyles.miniButton, GUILayout.Width(140)))
                        Selection.objects = bulunan.ToArray();
                }
        }

        // ------------------------------------------------------------ yardımcılar
        /// <summary>Mirven yolları Unity köküne ya da git köküne göredir; Assets/ ya da Packages/ ile başlayan parçayı alır.</summary>
        public static string VarlikYolu(string yol)
        {
            var y = (yol ?? "").Replace('\\', '/').TrimStart('/');
            if (y.StartsWith("Assets/") || y.StartsWith("Packages/") || y == "Assets") return y;
            foreach (var kok in new[] { "/Assets/", "/Packages/" })
            {
                var i = y.IndexOf(kok, StringComparison.Ordinal);
                if (i >= 0) return y.Substring(i + 1);
            }
            return y;
        }

        static UnityEngine.Object Varlik(string yol) =>
            string.IsNullOrEmpty(yol) ? null : AssetDatabase.LoadMainAssetAtPath(VarlikYolu(yol));

        static void Ping(string yol)
        {
            var v = Varlik(yol);
            if (v != null) EditorGUIUtility.PingObject(v);
        }

        static string Etiket(Dictionary<string, string> sozluk, string anahtar) =>
            sozluk.TryGetValue(anahtar ?? "", out var e) ? e : anahtar;

        static GUIStyle satir;
        static GUIStyle Satir() => satir ?? (satir = new GUIStyle(EditorStyles.label) { wordWrap = false, clipping = TextClipping.Clip, richText = false });

        static bool SatirCiz(bool secili, string sol, string sag, Texture ikon = null)
        {
            var r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2);
            if (secili) EditorGUI.DrawRect(r, EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.69f, 0.35f));
            else if (r.Contains(Event.current.mousePosition)) EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.12f));
            var sagGen = string.IsNullOrEmpty(sag) ? 0 : Mathf.Min(r.width * 0.45f, EditorStyles.miniLabel.CalcSize(new GUIContent(sag)).x + 6);
            var ikonGen = ikon != null ? 18 : 0;
            if (ikon != null) GUI.DrawTexture(new Rect(r.x + 2, r.y + 1, 16, 16), ikon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + 4 + ikonGen, r.y, r.width - sagGen - 8 - ikonGen, r.height), new GUIContent(sol, sol), Satir());
            if (sagGen > 0) GUI.Label(new Rect(r.xMax - sagGen, r.y + 1, sagGen, r.height), sag, EditorStyles.miniLabel);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        static class Proje
        {
            public static string Kok => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
        }
    }
}
