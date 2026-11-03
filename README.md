# 🚗 Autonomous Driving & ADAS Simulation Suite (Unity + ML-Agents)

[![Unity](https://img.shields.io/badge/Unity-2022.3%20%7C%20Unity%206-black?logo=unity)](https://unity.com/)
[![ML-Agents](https://img.shields.io/badge/Unity%20ML--Agents-Release%2020%2B-blue?logo=robot)](https://github.com/Unity-Technologies/ml-agents)
[![Inference](https://img.shields.io/badge/Unity%20Sentis-ONNX%20Runtime-orange)](https://unity.com/products/sentis)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A modular, end-to-end Autonomous Driving and Advanced Driver Assistance Systems (ADAS) simulation platform built in **Unity**. It combines **Deep Reinforcement Learning (PPO via Unity ML-Agents)** and **Deep Computer Vision (Unity Sentis ONNX Inference)** for full-stack autonomous vehicle research and prototyping.

---

## 📑 Proje Mimarisi & Modüller (System Architecture)

```
                            ┌──────────────────────────────────────────────┐
                            │      Autonomous Driving Suite (Unity)        │
                            └──────────────────────┬───────────────────────┘
                                                   │
        ┌───────────────────┬──────────────────────┼───────────────────────┬───────────────────┐
        ▼                   ▼                      ▼                       ▼                   ▼
┌───────────────┐   ┌───────────────┐      ┌───────────────┐       ┌───────────────┐   ┌───────────────┐
│  ACC System   │   │Parallel Park  │      │ Lane Centering│       │Pedestrian AEB │   │  Sign Vision  │
│  (ACCAgent)   │   │ (ParkAgent)   │      │(CenteringAgent│       │(SafetyManager)│   │ (Sentis 43cls)│
└───────┬───────┘   └───────┬───────┘      └───────┬───────┘       └───────┬───────┘   └───────┬───────┘
        │                   │                      │                       │                   │
        ▼                   ▼                      ▼                       ▼                   ▼
┌───────────────┐   ┌───────────────┐      ┌───────────────┐       ┌───────────────┐   ┌───────────────┐
│  Dynamic Lead │   │ 4-Wheel Phys  │      │Lateral Raycast│       │ Procedural Ped│   │Screen ROI Crop│
│  Cut-in Cars  │   │WheelColliders │      │Road Boundary  │       │ Yield Reward  │   │GPU Compute NN │
└───────────────┘   └───────────────┘      └───────────────┘       └───────────────┘   └───────────────┘
```

Proje **5 ana otonom sürüş alt sisteminden** meydana gelir:

### 1. 🅿️ Autonomous Parallel Parking (Otonom Paralel Park)
- **Ajan**: `ParallelParkingAgent.cs`
- **Gözlem Uzayı (Observations)**: Araç hızı, yerel konum ve rotasyon, park cebinin hedef konumu, hedefe olan Öklid mesafesi, hedefle açısal fark ve hedef içi varlık durumu.
- **Eylem Uzayı (Actions)**: Sürekli eylem uzayı (Direksiyon, İleri Gaz, Geri Vites).
- **Ödül Tasarımı (Reward Shaping)**:
  - Süre optimizasyonu için küçük adım cezası (`-0.01`).
  - Hedefe yaklaşma mesafesi gradyanı ve açısal hizalanma ödülü.
  - Park alanı içinde hız sıfırlama teşviki ve kusursuz parkta `+100` bonus ödül.
  - Kaldırım (`-2`), duvar (`-10`) ve park halindeki diğer araçlarla çarpışma cezaları.

### 2. 🛡️ Adaptive Cruise Control (ACC - Adaptif Hız Sabitleme)
- **Ajan**: `ACCAgent.cs`
- **Çevre Kontrolcüleri**: `LeadCarController.cs` (dinamik hız profilli ön araç), `RedCarController.cs` (aniden yola atlayan/şerit değiştiren engel araç).
- **Gözlem Uzayı**: Kendi hızı, ön araç mesafesi, ön aracın anlık hızı.
- **Eylem Uzayı**: Gaz ve fren kuvveti (Sürekli).
- **Ödül Tasarımı**:
  - Güvenli takip mesafesi penceresinde (`safeDistance ± 3m`) kalma ödülü (`+0.2`).
  - Tehlikeli yakınlaşmada (arkadan çarpma riski) ceza (`-0.2`).
  - Hız eşleme / senkronizasyon ödülleri.
  - Bölüm boyunca güvenli mesafeyi ihlal etmeden bitişe ulaşmada `+10` büyük bonus.

### 3. 🛣️ Lane Centering & Lane Keeping Assist (Şerit Takip ve Ortalama)
- **Ajanlar**:
  - `LaneCenteringAgent.cs`: Sürekli direksiyon ve motor torku ile sürekli şeritte kalma.
  - `DiscreteLaneAgent.cs`: Ayrık eylem uzayı (Sol/Sağ/Düz) ve 4 adet `CollidingWheel` Raycast sensörü ile WheelCollider alt adımı optimizasyonu.
- **Ödül Tasarımı**:
  - Şerit merkezinden sapma farkına göre Gauss (`exp(-10 * diff^2)`) ödülü.
  - Hedef hıza uyum (`exp(-0.1 * error^2)`) ödülü.
  - Şerit dışı sınır ihlali ve sarı/beyaz çizgi taşma cezaları.

### 4. 🚸 Pedestrian Detection & Crosswalk Yielding (Yaya Algılama ve Yol Verme - AEB)
- **Sistem Bileşenleri**: `PedestrianController.cs`, `CrossingDetector.cs`, `CollisionDetector.cs`, `GoalTrigger.cs`, `EpisodeController.cs`, `RewardManager.cs`.
- **Özellikler**:
  - Kaldırımda bekleyen, araç yaklaşınca yolun karşısına geçen veya geçmekten vazgeçen olasılıksal yaya davranışı.
  - Yaya geçitteyken yavaşlayan/duran araca yol verme ödülü (`+5`).
  - Yayaya çarpma durumunda terminal ceza (`-20`), yayasız veya yol verilerek hedefe varışta başarı ödülü (`+10`).

### 5. 🛑 Traffic Sign Recognition (TSR - Derin Öğrenme ile Tabela Tanıma)
- **Modül**: `TrafficSignReader.cs`
- **Yapay Zeka Mimarisi**: **Unity Sentis / InferenceEngine (GPU Compute Worker)**
- **Model Standardı**: 43 sınıflı **German Traffic Sign Recognition Benchmark (GTSRB)** (Hız sınırları, Dur, Yol Ver, Tehlike, Mecburi Yönler vb.).
- **Çalışma Prensibi**:
  - Araç kamerasından tabelanın ekran izdüşümünü (`WorldToScreenPoint`) yakalar.
  - Mesafeye bağlı dinamik ROI odak kutusu kırpar (`ReadPixels`).
  - `30x30x3` tensörüne dönüştürür (`NHWC` formatı).
  - Sentis GPU Compute arka planında sınıflandırır ve `%70+` güven eşiğinde HUD arayüzünde canlı yansıtır.

---

## 📂 Klasör Hiyerarşisi (Repository Structure)

```
Autonomous-Driving-Unity/
├── Assets/
│   └── AutonomousDriving/
│       ├── Core/
│       │   ├── Vehicle/
│       │   │   ├── PhysicsCarController.cs     # 4 tekerlekli WheelCollider fizik kontrolcüsü
│       │   │   ├── SimpleCarController.cs      # Boylamsal basit araç kontrolcüsü
│       │   │   └── CollidingWheel.cs           # Raycast tabanlı mesafe algılayıcı tekerlek
│       │   └── Camera/
│       │       ├── CameraFollow.cs             # Yumuşak takipçi araç kamerası
│       │       └── CameraManager.cs            # Çok açılı kamera yöneticisi (Follow/Kuşbakışı/Yan)
│       ├── Parking/
│       │   └── ParallelParkingAgent.cs         # Paralel park ML-Agents ajanı
│       ├── CruiseControl/
│       │   ├── ACCAgent.cs                     # Adaptif hız sabitleme RL ajanı
│       │   ├── LeadCarController.cs            # Dinamik hız profilli ön araç kontrolcüsü
│       │   ├── RedCarController.cs             # Yola dalan engel araç
│       │   └── SpeedInterface.cs               # Hız göstergesi telemetri arayüzü
│       ├── LaneKeeping/
│       │   ├── LaneCenteringAgent.cs           # Sürekli şerit merkezleme ve hız takip ajanı
│       │   └── DiscreteLaneAgent.cs            # Ayrık eylemli şerit takip ajanı
│       ├── PedestrianSafety/
│       │   ├── PedestrianController.cs         # Prosedürel yaya hareketi ve karar mantığı
│       │   ├── CrossingDetector.cs             # Yaya geçidi alan doluluk tetikleyicisi
│       │   ├── CollisionDetector.cs            # Araç-yaya çarpışma sezicisi
│       │   ├── GoalTrigger.cs                  # Bitiş çizgisi varış tetikleyicisi
│       │   ├── EpisodeController.cs            # Yaya senaryosu bölüm yöneticisi
│       │   └── RewardManager.cs                # Yol verme ve güvenlik ödül hesaplayıcısı
│       ├── TrafficSignRecognition/
│       │   └── TrafficSignReader.cs            # Sentis 43 sınıflı GTSRB tabela okuyucu
│       ├── Environment/
│       │   ├── EnvironmentBuilder.cs           # Prosedürel yol, ağaç, tabela ve aydınlatma
│       │   └── RoadMarkingGenerator.cs         # Prosedürel yaya geçidi ve şerit çizgileri
│       └── UI/
│           └── TrainingHUD.cs                  # Dinamik telemetri ve eğitim HUD arayüzü
├── Config/                                     # ML-Agents PPO Eğitim Konfigürasyonları
│   ├── acc_config.yaml
│   ├── parking_config.yaml
│   ├── lane_centering_config.yaml
│   └── pedestrian_crossing_config.yaml
├── .gitignore                                  # Unity & ML-Agents için temiz gitignore
├── .gitattributes                              # Git LFS (ONNX modelleri & dokular için)
├── LICENSE                                     # MIT Lisansı
└── README.md                                   # Detaylı dökümantasyon
```

---

## 🚀 Kurulum & Başlangıç (Getting Started)

### 1. Gereksinimler
- **Unity**: `2022.3 LTS` veya `Unity 6 (6000.x)`.
- **Unity Paketleri (Package Manager)**:
  - `com.unity.ml-agents` (Release 20 / v2.0+)
  - `com.unity.sentis` (veya Unity InferenceEngine)
  - `com.unity.inputsystem` (New Input System)
  - `TextMeshPro`
- **Python (Model Eğitimi İçin)**:
  - Python 3.9 veya 3.10
  - `pip install mlagents torch`

### 2. Unity Projesine Entegrasyon
1. Bu depoyu klonlayın:
   ```bash
   git clone https://github.com/kullanici_adiniz/Autonomous-Driving-Unity.git
   ```
2. Unity Hub üzerinden projeyi açın veya `Assets/AutonomousDriving` klasörünü mevcut projenize sürükleyin.
3. Sahnenizde araç modelinize `PhysicsCarController` veya `SimpleCarController` bileşenlerini atayın.

---

## 🧠 ML-Agents Model Eğitimi (Training Guide)

Terminalinizi açın, proje kök dizinine gelin ve eğitmek istediğiniz modülün komutunu çalıştırın:

#### Paralel Park Eğitimi:
```bash
mlagents-learn Config/parking_config.yaml --run-id=Park_PPO_01
```

#### Adaptif Hız Sabitleme (ACC) Eğitimi:
```bash
mlagents-learn Config/acc_config.yaml --run-id=ACC_PPO_01
```

#### Şerit Takip & Merkezleme Eğitimi:
```bash
mlagents-learn Config/lane_centering_config.yaml --run-id=Lane_PPO_01
```

Komutu verdikten sonra Unity Editor içerisinde **Play (▶️)** butonuna basarak eğitimi başlatın. Canlı telemetriyi incelemek için:
```bash
tensorboard --logdir results
```

---

## 🎮 Klavye Kontrolleri (Manuel Test / Heuristic)

| Tuş | Fonksiyon |
|---|---|
| **W / S** | Gaz / Fren & Geri Vites |
| **A / D** | Direksiyon Sol / Sağ |
| **Space** | El Freni (Brake) |
| **1 / 2 / 3** | Kamera Modu Değiştir (1: Takipçi, 2: Kuşbakışı, 3: Sabit Yan) |

---

## 📜 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına göz atabilirsiniz.
