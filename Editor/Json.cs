using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mirven.Unity.Editor
{
    /// <summary>
    /// Mirven yanıtları serbest biçimli (sözlük, liste); JsonUtility bunları okuyamaz.
    /// Nesne → Dictionary&lt;string, object&gt;, dizi → List&lt;object&gt;, sayı → double.
    /// </summary>
    public static class Json
    {
        public static object Oku(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return null;
            var o = new Okuyucu(metin);
            var deger = o.Deger();
            o.Bosluk();
            if (o.i != metin.Length) throw new FormatException("JSON sonunda fazladan karakter var.");
            return deger;
        }

        public static string Yaz(object deger)
        {
            var sb = new StringBuilder();
            Yaz(sb, deger);
            return sb.ToString();
        }

        static void Yaz(StringBuilder sb, object d)
        {
            switch (d)
            {
                case null: sb.Append("null"); break;
                case string s: Metin(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case long n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case float n: sb.Append(n.ToString("R", CultureInfo.InvariantCulture)); break;
                case double n: sb.Append(n.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> nesne:
                    sb.Append('{');
                    var ilk = true;
                    foreach (var kv in nesne)
                    {
                        if (!ilk) sb.Append(',');
                        ilk = false;
                        Metin(sb, kv.Key);
                        sb.Append(':');
                        Yaz(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable liste:
                    sb.Append('[');
                    var bas = true;
                    foreach (var x in liste)
                    {
                        if (!bas) sb.Append(',');
                        bas = false;
                        Yaz(sb, x);
                    }
                    sb.Append(']');
                    break;
                default: Metin(sb, d.ToString()); break;
            }
        }

        static void Metin(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Okuyucu
        {
            readonly string m;
            public int i;

            public Okuyucu(string metin) { m = metin; }

            public void Bosluk()
            {
                while (i < m.Length && char.IsWhiteSpace(m[i])) i++;
            }

            public object Deger()
            {
                Bosluk();
                if (i >= m.Length) throw new FormatException("JSON erken bitti.");
                var c = m[i];
                if (c == '{') return Nesne();
                if (c == '[') return Dizi();
                if (c == '"') return Dize();
                if (Bekle("true")) return true;
                if (Bekle("false")) return false;
                if (Bekle("null")) return null;
                return Sayi();
            }

            bool Bekle(string s)
            {
                if (string.CompareOrdinal(m, i, s, 0, s.Length) != 0) return false;
                i += s.Length;
                return true;
            }

            Dictionary<string, object> Nesne()
            {
                var d = new Dictionary<string, object>();
                i++;
                Bosluk();
                if (i < m.Length && m[i] == '}') { i++; return d; }
                while (true)
                {
                    Bosluk();
                    var ad = Dize();
                    Bosluk();
                    if (i >= m.Length || m[i] != ':') throw new FormatException("JSON: ':' bekleniyordu.");
                    i++;
                    d[ad] = Deger();
                    Bosluk();
                    if (i < m.Length && m[i] == ',') { i++; continue; }
                    if (i < m.Length && m[i] == '}') { i++; return d; }
                    throw new FormatException("JSON: ',' ya da '}' bekleniyordu.");
                }
            }

            List<object> Dizi()
            {
                var l = new List<object>();
                i++;
                Bosluk();
                if (i < m.Length && m[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Deger());
                    Bosluk();
                    if (i < m.Length && m[i] == ',') { i++; continue; }
                    if (i < m.Length && m[i] == ']') { i++; return l; }
                    throw new FormatException("JSON: ',' ya da ']' bekleniyordu.");
                }
            }

            string Dize()
            {
                if (m[i] != '"') throw new FormatException("JSON: dize bekleniyordu.");
                i++;
                var sb = new StringBuilder();
                while (i < m.Length)
                {
                    var c = m[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    var e = m[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)int.Parse(m.Substring(i, 4), NumberStyles.HexNumber));
                            i += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                throw new FormatException("JSON: dize kapanmadı.");
            }

            double Sayi()
            {
                var bas = i;
                while (i < m.Length && "+-0123456789.eE".IndexOf(m[i]) >= 0) i++;
                if (bas == i) throw new FormatException("JSON: beklenmeyen karakter '" + m[i] + "'.");
                return double.Parse(m.Substring(bas, i - bas), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        // ------------------------------------------------ okuma yardımcıları
        public static Dictionary<string, object> Nesne(object o, string ad) =>
            o is Dictionary<string, object> d && d.TryGetValue(ad, out var v) ? v as Dictionary<string, object> : null;

        public static List<object> Liste(object o, string ad) =>
            o is Dictionary<string, object> d && d.TryGetValue(ad, out var v) ? v as List<object> ?? new List<object>() : new List<object>();

        public static string Dize(object o, string ad) =>
            o is Dictionary<string, object> d && d.TryGetValue(ad, out var v) && v != null
                ? (v is double n ? n.ToString(CultureInfo.InvariantCulture) : v.ToString()) : "";

        public static double Sayi(object o, string ad) =>
            o is Dictionary<string, object> d && d.TryGetValue(ad, out var v) && v is double n ? n : 0;

        public static bool Dogru(object o, string ad) =>
            o is Dictionary<string, object> d && d.TryGetValue(ad, out var v) && v is bool b && b;
    }
}
