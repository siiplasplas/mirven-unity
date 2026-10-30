using System;
using System.Collections.Generic;
using UnityEditor;

namespace Mirven.Unity.Editor
{
    public enum BaglantiDurumu { Bekliyor, Baglaniyor, Baglandi, ProjeYok, Hata }

    /// <summary>
    /// Bu Unity projesinin Mirven Stüdyo'daki karşılığı: Stüdyo proje listesinde `yol` alanı proje köküyle
    /// (Assets'in üstü) eşleşen kayıt. Yalnız okur.
    /// </summary>
    public static class MirvenBaglanti
    {
        const string PidAnahtari = "Mirven.Pid";

        public static BaglantiDurumu Durum { get; private set; } = BaglantiDurumu.Bekliyor;
        public static string Hata { get; private set; } = "";
        public static Dictionary<string, object> Proje { get; private set; }
        public static DateTime SonOkuma { get; private set; }
        public static event Action Degisti;

        public static string Pid
        {
            get => SessionState.GetString(PidAnahtari, "");
            private set => SessionState.SetString(PidAnahtari, value ?? "");
        }

        static bool okunuyor;
        public static bool Okunuyor => okunuyor;

        public static void Yenile(Action bitince = null)
        {
            if (okunuyor) return;
            okunuyor = true;
            if (Proje == null) Durum = BaglantiDurumu.Baglaniyor;
            Degisti?.Invoke();
            MirvenIstemci.Iste("GET", "/api/m1/studyo", null, y =>
            {
                if (!y.Tamam) { Bitir(BaglantiDurumu.Hata, y.Hata, bitince); return; }
                var pid = Eslesen(Json.Liste(y.Veri, "projeler"));
                if (pid == "")
                {
                    Pid = "";
                    Proje = null;
                    Bitir(BaglantiDurumu.ProjeYok, "", bitince);
                    return;
                }
                Pid = pid;
                MirvenIstemci.Iste("GET", "/api/m1/studyo/" + Uri.EscapeDataString(pid), null, p =>
                {
                    if (!p.Tamam) { Bitir(BaglantiDurumu.Hata, p.Hata, bitince); return; }
                    Proje = Json.Nesne(p.Veri, "proje");
                    SonOkuma = DateTime.Now;
                    Bitir(BaglantiDurumu.Baglandi, "", bitince);
                });
            });
        }

        /// <summary>Bekleyerek bağlanır (batchmode doğrulaması). Döner: hata metni ya da "".</summary>
        public static string YenileBekle()
        {
            var y = MirvenIstemci.IsteBekle("GET", "/api/m1/studyo");
            if (!y.Tamam) { Durum = BaglantiDurumu.Hata; return Hata = y.Hata; }
            Pid = Eslesen(Json.Liste(y.Veri, "projeler"));
            if (Pid == "") { Durum = BaglantiDurumu.ProjeYok; return Hata = "Bu proje Mirven Stüdyo'ya bağlı değil: " + MirvenAyar.ProjeKoku; }
            var p = MirvenIstemci.IsteBekle("GET", "/api/m1/studyo/" + Uri.EscapeDataString(Pid));
            if (!p.Tamam) { Durum = BaglantiDurumu.Hata; return Hata = p.Hata; }
            Proje = Json.Nesne(p.Veri, "proje");
            Durum = BaglantiDurumu.Baglandi;
            return Hata = "";
        }

        static string Eslesen(List<object> projeler)
        {
            var kok = MirvenAyar.ProjeKoku;
            foreach (var p in projeler)
                if (MirvenAyar.Normal(Json.Dize(p, "yol")) == kok) return Json.Dize(p, "id");
            return "";
        }

        static void Bitir(BaglantiDurumu d, string hata, Action bitince)
        {
            okunuyor = false;
            Durum = d;
            Hata = hata;
            if (d != BaglantiDurumu.Baglandi && d != BaglantiDurumu.ProjeYok) Proje = null;
            if (d == BaglantiDurumu.Baglandi) MirvenDerleme.BekleyeniGonder();
            Degisti?.Invoke();
            bitince?.Invoke();
        }
    }
}
