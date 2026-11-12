# Unity Otonom Sürüş ve ADAS Simülasyonu

Unity ortamında pekiştirmeli öğrenme (Unity ML-Agents) ve derin öğrenme tabanlı bilgisayarlı görü (Unity Sentis) kullanılarak geliştirilmiş modüler otonom sürüş alt sistemleri simülasyonudur.

## Önizleme

| Paralel Park |
|---|
| <img width="1710" height="1154" alt="Park" src="https://github.com/user-attachments/assets/f0304b43-bf91-4bcd-96ce-ebce361f5c93" /> |

| Adaptif Hız Sabitleme |
|---|
| <img width="1930" height="1444" alt="Takip" src="https://github.com/user-attachments/assets/e1c4167c-8aaa-4460-a7c8-8eb1a004ee12" /> |

| Yaya Güvenliği |
|---|
| <img width="2764" height="1584" alt="Yaya" src="https://github.com/user-attachments/assets/d171a800-b192-43b8-8a4a-9ef726d93b04" /> | 

| Tabela Tanıma |
|---|
| <img width="1616" height="724" alt="Tabela" src="https://github.com/user-attachments/assets/2ca64143-46e7-401d-8fd3-b3985a1ee40f" /> |

## Modüller

### 1. Paralel Park
- Dosya: `ParallelParkingAgent.cs`
- Algoritma: PPO (Proximal Policy Optimization)
- Girdiler: Araç hızı, yerel konum/rotasyon, park cebine olan mesafe ve açı farkı.
- Eylemler: Sürekli direksiyon açısı, gaz ve geri vites torku.
- Amaç: Çevre araçlara, kaldırıma veya duvara çarpmadan aracı park cebine hizalayıp tamamen durdurmak.

### 2. Adaptif Hız Sabitleme 
- Dosyalar: `ACCAgent.cs`, `LeadCarController.cs`, `RedCarController.cs`
- Senaryo: Değişken hız profiline sahip ön araç ve aniden şeride katılan engel araç.
- Girdiler: Araç hızı, ön araç mesafesi ve ön aracın hızı.
- Amaç: Ani hız değişimlerinde güvenli takip mesafesini (30m) korumak ve arkadan çarpmayı önlemek.

### 3. Şerit Takip ve Ortalama
- Dosyalar: `LaneCenteringAgent.cs`, `DiscreteLaneAgent.cs`, `CollidingWheel.cs`
- Algılama: Yol sınırlarına ve şerit çizgilerine fırlatılan Raycast sensörleri.
- Amaç: Düz ve virajlı yollarda seyir hızını korurken aracı şerit merkezinde tutmak.

### 4. Yaya Güvenliği ve Yol Verme
- Dosyalar: `PedestrianController.cs`, `CrossingDetector.cs`, `EpisodeController.cs`, `RewardManager.cs`
- Dinamikler: Kaldırımda bekleyen veya yaklaşan araca göre yola adım atan stokastik yaya davranışı.
- Amaç: Yaya geçidine giren yayayı algılayıp güvenli mesafede durmak, geçiş tamamlandıktan sonra hedefe ilerlemek.

### 5. Trafik Tabelası Tanıma
- Dosya: `TrafficSignReader.cs`
- Model: 43 sınıflı GTSRB (German Traffic Sign Recognition Benchmark) ONNX modeli.
- Çıkarım Motoru: Unity Sentis (GPU Compute).
- Çalışma Mantığı: Araç kamerasından alınan görüntüden tabela alanını dinamik olarak kırpar (30x30 piksel, NHWC formatı), GPU üzerinde sınıflandırır ve güven eşiğini (%70) aşan tabelaları ekrana yansıtır.

## Klasör Yapısı

```
Assets/
└── AutonomousDriving/
    ├── Core/                      # Araç fizikleri ve kamera kontrolcüleri
    ├── Parking/                   # Paralel park ajanı
    ├── CruiseControl/             # ACC ajanı ve trafik kontrolcüleri
    ├── LaneKeeping/               # Şerit takip ve ortalama ajanları
    ├── PedestrianSafety/          # Yaya davranışı, geçit algılama ve ödül sistemi
    ├── TrafficSignRecognition/    # Sentis tabela tanıma modülü
    ├── Environment/               # Prosedürel yol ve çevre üreticisi
    └── UI/                        # Eğitim telemetri arayüzü
Config/                            # ML-Agents eğitim konfigürasyonları (.yaml)
media/                             # Ekran görüntüleri ve demo GIF'leri
```

## Kurulum ve Gereksinimler

### Gereksinimler
- Unity 2022.3 LTS veya Unity 6
- Unity Paketleri: `ML-Agents (2.0+)`, `Unity Sentis (1.3+)`, `Input System`, `TextMeshPro`
- Python 3.9 / 3.10 (`pip install mlagents torch`)

### Kurulum Adımları
1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/emuratgullu/Autonomous-Driving-Unity.git
   ```
2. Projeyi Unity Hub üzerinden açın.
3. Tabela tanıma için eğittiğiniz GTSRB `.onnx` modelini Unity'ye aktarıp `TrafficSignReader` bileşenine atayın.

## Model Eğitimi

İlgili ajanın eğitimini başlatmak için terminalden komutu çalıştırın ve Unity Editör'de Play butonuna basın:

```bash
# Paralel Park
mlagents-learn Config/parking_config.yaml --run-id=park_01

# Adaptif Hız Sabitleme
mlagents-learn Config/acc_config.yaml --run-id=acc_01

# Şerit Takip
mlagents-learn Config/lane_centering_config.yaml --run-id=lane_01
```

## Manuel Test Kontrolleri

| Tuş | İşlev |
|---|---|
| W / S | Gaz / Fren & Geri Vites |
| A / D | Direksiyon Sol / Sağ |
| Space | El Freni |
| 1 / 2 / 3 | Kamera Açısı (Takipçi / Kuşbakışı / Yan Profil) |

## Lisans

Bu proje MIT Lisansı altında sunulmaktadır. Ayrıntılar için LICENSE dosyasına bakabilirsiniz.
