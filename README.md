# Mirven for Unity

[Türkçe](#türkçe) · [English](#english)

---

## Türkçe

Unity Editor'ün içinde Mirven Stüdyo paneli. Mirven'in bu proje için çıkardığı istekleri ve işleri gösterir, bir işin değiştirdiği dosyaları Project penceresinde bulur, derleme hatalarını Mirven'e gönderir.

**Salt okur.** Paket Unity projenize hiçbir şey yazmaz: sahne, prefab, kod ya da ayar değiştirmez. Mirven'e giden tek şey derleme raporudur (dosya, satır, hata metni).

### Ne gösterir

- **Bağlı proje.** Unity projesinin klasörü Mirven Stüdyo'daki bir projeyle eşleşirse adı ve durumu üstte görünür.
- **İstekler.** "Kart satışına vergi ekleyelim" gibi istekler ve durumları (inceleniyor, hazır, hata). Bir isteği seçince yalnız onun işleri listelenir.
- **İşler.** No, ad, durum (yapılacak, sürüyor, incelemede, bitti) ve kişi.
- **İş ayrıntısı.** Kabul ölçütleri, Mirven'in iş üstündeki durumu ve değiştirdiği dosyalar (`+eklenen −silinen` satır). Bir dosyaya tıklayınca Project penceresinde gösterilir. "Hepsini Project'te seç" bütün dosyaları seçer. Projede bulunmayan dosya soluk görünür, çünkü Mirven işi kendi kopyasında, ayrı bir dalda yapar.
- **Derleme.** Son derlemenin hataları. Tıklayınca dosya bulunur. Hatalar Mirven'e gider ve Stüdyo › Ayarlar'da görünür.

### Kurulum

Mirven bu bilgisayarda kuruluysa tek komut yeter:

```
mirven unity-panel "C:\Oyunlar\Projem"
```

Komut iki şey yapar:

1. `Packages/manifest.json` dosyasına `com.mirven.unity` satırını ekler. Dosyanın önceki hali Mirven'in `yedek/` klasörüne kopyalanır.
2. `UserSettings/Mirven.json` dosyasına Mirven'in adresini ve yerel anahtar dosyasının **yolunu** yazar. Anahtarın kendisi kopyalanmaz. `UserSettings/` Unity'nin varsayılan `.gitignore` listesindedir.

Elle kurmak da mümkün: Unity'de **Window › Package Manager › + › Add package from git URL…** menüsüne şu adresi girin:

```
https://github.com/siiplasplas/mirven-unity.git#v0.1.0
```

Panel **Window › Mirven** menüsünden açılır. Projenin Mirven Stüdyo'ya bağlı olması gerekir. Mirven'de Stüdyo'yu açıp bu klasörü bağlayın.

### Ayarlar

Paneldeki **Ayarlar** düğmesi şunları açar:

- **Adres.** Varsayılan `http://127.0.0.1:8791`.
- **Anahtar dosyası.** Mirven'in `data/.anahtar` dosyası. Mirven'de hesap açıksa bu dosya olmadan bağlanılamaz.

Elle girilen ayar yalnız bu bilgisayarda ve bu projede geçerlidir (EditorPrefs). Ayar şu sırayla okunur: önce elle girilen ayar, sonra `UserSettings/Mirven.json`, sonra varsayılan.

### Gereksinimler

- Unity 2021.3 ya da üstü.
- Aynı bilgisayarda açık bir Mirven.

---

## English

A Mirven Studio panel inside the Unity Editor. It shows the requests and tasks Mirven planned for this project, finds the files a task changed in the Project window, and sends compile errors back to Mirven.

**Read-only.** The package never writes to your Unity project. It doesn't change scenes, prefabs, code or settings. The only thing sent to Mirven is the compile report: file, line and error text.

### What it shows

- **Linked project.** If the Unity project's folder matches a project in Mirven Studio, its name and status appear at the top.
- **Requests.** Requests such as "add a tax to card sales", with their status: under review, ready, or failed. Selecting a request filters the task list to that request.
- **Tasks.** Number, name, status (to do, in progress, in review, done) and assignee.
- **Task details.** Acceptance criteria, Mirven's status on the task, and the files it changed, with `+added −removed` lines.
  - Click a file to ping it in the Project window.
  - "Select all in Project" selects every file.
  - Files that aren't in your project yet show greyed out. Mirven works in its own copy, on a separate branch.
- **Compile.** Errors from the last compile; click one to ping the file. The errors go to Mirven and show up under Studio › Settings.

### Install

If Mirven is installed on this computer, one command does it:

```
mirven unity-panel "C:\Games\MyGame"
```

The command does two things:

1. It adds `com.mirven.unity` to `Packages/manifest.json`. The previous file is copied to Mirven's `yedek/` folder first.
2. It writes Mirven's address and the **path** of its local key file to `UserSettings/Mirven.json`. The key itself is never copied. `UserSettings/` is in Unity's default `.gitignore`.

You can also install it by hand. In Unity, go to **Window › Package Manager › + › Add package from git URL…** and enter:

```
https://github.com/siiplasplas/mirven-unity.git#v0.1.0
```

Open the panel from **Window › Mirven**. The project has to be linked in Mirven Studio first: open Studio in Mirven and link this folder.

### Settings

The **Ayarlar** (Settings) button in the panel opens these fields:

- **Address.** Default: `http://127.0.0.1:8791`.
- **Key file.** Mirven's `data/.anahtar`. If your Mirven has an account, the panel can't connect without it.

Manual settings apply to this machine and this project only (EditorPrefs). Settings are read in this order: manual settings, then `UserSettings/Mirven.json`, then the default.

### Requirements

- Unity 2021.3 or newer.
- Mirven running on the same machine.

## License

MIT, see [LICENSE.md](LICENSE.md).
