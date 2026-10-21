# 📊 TIẾN TRÌNH DỰ ÁN RUBIK SOLVER 3D (PROJECT TRACKER)

> **Cập nhật lần cuối:** 21/10/2026  
> **Trạng thái hiện tại:** `Giai đoạn 2 - Cơ chế xoay & Quản lý trạng thái logic (Core Mechanics & State)`  
> **Phiên bản Unity:** `Unity 6 (6000.6.5f1)`  
> **Render Pipeline:** Universal Render Pipeline (URP)  
> **Input System:** Unity New Input System (`UnityEngine.InputSystem`)  

---

## 📌 1. TỔNG QUAN DỰ ÁN
**RubikSolver3D** là ứng dụng/game mô phỏng và giải khối Rubik 3x3 tương tác 3D chân thực, hỗ trợ:
- Dựng hình khối Rubik 3x3x3 3D chuẩn xác.
- Điều khiển camera quỹ đạo (Orbit) tự do theo chuột/cảm ứng.
- Xoay các lớp mặt (Face Rotation) mượt mà bằng phím tắt / tương tác kéo thả.
- Hệ thống quản lý trạng thái Rubik phẳng (6 mặt $\times$ 9 ô) hỗ trợ Undo/Redo.
- Thuật toán tự động giải khối Rubik (Kociemba / CFOP / LBL) và minh họa bước giải trực quan.
- Tùy chọn trộn ngẫu nhiên (Scramble) và bộ đếm giờ (Timer).

---

## 🗺️ 2. BẢNG TIẾN ĐỘ THEO GIAI ĐOẠN (ROADMAP)

| Giai đoạn | Nội dung công việc | Trạng thái | Tỉ lệ hoàn thành |
| :--- | :--- | :---: | :---: |
| **Phase 1** | **Khởi tạo & Dựng hình khối Rubik 3D** | ✅ Hoàn thành | 100% |
| **Phase 2** | **Cơ chế xoay mặt & Quản lý trạng thái** | 🟡 Đang hoàn thiện | 85% |
| **Phase 3** | **Tương tác chuột/cảm ứng kéo mặt (Face Dragging)** | ⚪ Chưa bắt đầu | 0% |
| **Phase 4** | **Thuật toán giải Rubik tự động (Solver Algorithm)** | ⚪ Chưa bắt đầu | 0% |
| **Phase 5** | **Giao diện người dùng (UI/UX) & Chế độ chơi** | ⚪ Chưa bắt đầu | 0% |
| **Phase 6** | **Âm thanh, Hiệu ứng & Visual Polish** | ⚪ Chưa bắt đầu | 0% |
| **Phase 7** | **Tối ưu hóa & Đóng gói đa nền tảng (Build)** | ⚪ Chưa bắt đầu | 0% |

---

## 📝 3. CHI TIẾT CÁC HẠNG MỤC (CHECKLIST)

### ✅ Phase 1: Dựng hình khối Rubik 3D (Đã hoàn tất)
- [x] Tạo `RubikGenerator.cs` sinh tự động 26/27 mảnh cubie (tâm, cạnh, góc).
- [x] Thiết lập hệ màu WCA thi đấu chuẩn: Trắng (+Y), Vàng (-Y), Xanh lá (+Z), Xanh dương (-Z), Đỏ (+X), Cam (-X), Đen cho lõi.
- [x] Tạo sticker offset để tránh hiện tượng Z-fighting.
- [x] Tự động khởi tạo Material chuẩn URP Lit tương thích Unity 6.
- [x] Tạo `RubikCameraOrbit.cs` xoay quanh tâm Rubik bằng chuột (xoay ngang, dọc và zoom in/out).

### 🟡 Phase 2: Cơ chế xoay & Trạng thái logic (Đang xử lý)
- [x] Tạo `RubikRotator.cs` hỗ trợ xoay các mặt chuẩn WCA: `U, D, R, L, F, B` và các mặt nghịch đảo (`'`) thông qua Shift.
- [x] Hoạt ảnh xoay mặt mượt mà với đường cong chuyển động `AnimationCurve.EaseInOut`.
- [x] Xử lý hàng đợi lệnh xoay (`moveQueue`) tránh xung đột khi bấm liên tục.
- [x] Tạo `RubikState.cs` lưu trữ ma trận màu 6 mặt $\times$ 9 ô, độc lập với Unity GameObject.
- [x] Tạo `RubikStateManager.cs` hỗ trợ ngăn xếp lịch sử Undo/Redo và kiểm tra tính hợp lệ khối Rubik.
- [ ] Đồng bộ hai chiều giữa chuyển động xoay 3D vật lý (`RubikRotator`) và trạng thái phẳng (`RubikState`).
- [ ] Xử lý xoay các tầng giữa (M, E, S) và xoay toàn khối (x, y, z).

### ⚪ Phase 3: Tương tác chạm / Kéo chuột trực tiếp (Face Dragging)
- [ ] Bắn Raycast nhận diện đúng mảnh cubie và mặt sticker mà chuột nhấp trúng.
- [ ] Phân tích vector kéo chuột (Drag Vector) để suy ra mặt xoay và chiều xoay mong muốn.
- [ ] Cơ chế bám tay tạm thời (Drag preview rotation) trước khi nhả chuột để chốt góc xoay $90^\circ$.
- [ ] Hỗ trợ đa cảm ứng (Multi-touch) cho thiết bị di động.

### ⚪ Phase 4: Thuật toán giải Rubik tự động (Rubik Solver)
- [ ] Tích hợp thuật toán giải (Kociemba Two-Phase hoặc phương pháp CFOP / Beginner Layer-by-layer).
- [ ] Chuyển đổi trạng thái hiện tại từ `RubikState` thành chuỗi ký hiệu đầu vào cho Solver.
- [ ] Nhận về chuỗi lời giải (ví dụ: `R U R' U' ...`) và tự động nạp vào `moveQueue`.
- [ ] Chế độ xem từng bước giải (Step-by-step viewer) có nút Next / Previous cho người học.

### ⚪ Phase 5: Giao diện UI/UX & Tính năng bổ trợ
- [ ] Nút Trộn ngẫu nhiên (Scramble) với 20-25 bước chuẩn WCA.
- [ ] Nút Giải tự động (Solve) & Đặt lại về ban đầu (Reset).
- [ ] Thanh trượt điều chỉnh tốc độ xoay (Rotation Speed Slider).
- [ ] Đồng hồ bấm giờ (Timer / Speedcubing StopWatch).
- [ ] Màn hình nhập màu thủ công hoặc quét Camera cho khối Rubik thật bên ngoài.

### ⚪ Phase 6: Âm thanh & Đồ họa
- [ ] SFX tiếng xoay cơ học chân thực của khối Rubik nhựa.
- [ ] SFX hoàn thành (Victory sound) & hạt bụi sáng (Particle effect).
- [ ] Tinh chỉnh ánh sáng, Shadow, Bloom và Post-Processing trong URP.

### ⚪ Phase 7: Đóng gói & Phát hành
- [ ] Build Windows Desktop (x64).
- [ ] Build WebGL (Chơi trực tiếp trên trình duyệt).
- [ ] Build Android / iOS (nếu có nhu cầu).

---

## 📂 4. SƠ ĐỒ CẤU TRÚC MÃ NGUỒN (CODEBASE MAP)

```text
Assets/
├── Scripts/
│   ├── RubikGenerator.cs     # Khởi tạo mô hình 3D, chia lưới 3x3x3, dán nhãn màu URP
│   ├── RubikRotator.cs       # Logic xoay các tầng, Coroutine hoạt ảnh, nhận diện phím
│   ├── RubikState.cs         # Mô hình dữ liệu toán học (màu 6 mặt x 9 ô), xoay mảng
│   ├── RubikStateManager.cs  # Quản lý lịch sử Undo / Redo, Validate tính hợp lệ
│   └── RubikCameraOrbit.cs   # Điều khiển Camera tự do (Orbit, Clamp angle, Zoom)
├── Scenes/
│   ├── MainScene.unity       # Scene chính chứa RubikManager, Camera, Ánh sáng
│   └── SampleScene.unity     # Scene mặc định ban đầu
├── Settings/                 # Cấu hình URP (PC / Mobile Asset, Renderer, Volume Profile)
└── InputSystem_Actions.inputactions # Cấu hình New Input System của Unity 6
```

---

## 💾 5. HƯỚNG DẪN SAO LƯU (BACKUP GUIDE)

### ⚠️ Quy tắc sống còn khi backup Unity:
Dự án Unity sinh ra rất nhiều thư mục tạm / cache / biên dịch (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) có dung lượng từ **vài GB đến chục GB**. Bạn **tuyệt đối không cần backup các thư mục này**, vì Unity sẽ tự động sinh lại khi mở dự án.

#### ✅ 3 Thư mục BẮT BUỘC cần backup (Dung lượng chỉ vài chục MB):
1. `Assets/` (Chứa toàn bộ code, scene, material, âm thanh...)
2. `Packages/` (Chứa danh sách package cần cài trong `manifest.json`)
3. `ProjectSettings/` (Chứa cấu hình đồ họa, input, layer, tag của dự án)

#### ❌ Các thư mục KHÔNG ĐƯỢC nén / backup:
- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `*.sln`, `*.slnx`, `*.csproj`

---

### 🚀 Phương án 1: Backup qua Git / GitHub (Khuyên dùng)
Nếu muốn sao lưu lên GitHub, chạy các lệnh sau trong PowerShell tại thư mục dự án:

```powershell
# 1. Khởi tạo kho git (nếu chưa có)
git init

# 2. Thêm file .gitignore chuẩn Unity (xem file .gitignore đã tạo)
git add .gitignore PROJECT_PROGRESS.md Assets Packages ProjectSettings

# 3. Tạo commit đánh dấu
git commit -m "feat: Milestone Phase 1 & 2 - Rubik Generator, Rotator and State Management"

# 4. Đẩy lên GitHub (thay URL của bạn vào)
# git remote add origin https://github.com/username/RubikSolver3D.git
# git branch -M main
# git push -u origin main
```

---

### 📦 Phương án 2: Backup nén ZIP nhanh gọn bằng PowerShell
Để nén nhanh chỉ các thư mục cốt lõi vào 1 file ZIP (được đặt tên theo ngày giờ):

```powershell
# Chạy lệnh này tại thư mục gốc dự án:
$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
Compress-Archive -Path "Assets", "Packages", "ProjectSettings", "PROJECT_PROGRESS.md" -DestinationPath "..\RubikSolver3D_Backup_$timestamp.zip"
Write-Host "Đã tạo bản sao lưu thành công tại: ..\RubikSolver3D_Backup_$timestamp.zip"
```

---

## 📜 6. NHẬT KÝ CẬP NHẬT (WORK LOG)

| Ngày | Người thực hiện | Nội dung cập nhật |
| :---: | :---: | :--- |
| **21/10/2026** | Antigravity AI | - Khởi tạo tài liệu theo dõi tiến trình `PROJECT_PROGRESS.md`.<br>- Tổng hợp trạng thái Phase 1 (Generator, Materials, Camera) và Phase 2 (Rotator, Logic State, Undo/Redo).<br>- Thiết lập chuẩn quy trình backup tối ưu cho Unity 6. |
| **Giai đoạn trước** | Developer | - Xây dựng bộ khung Rubik 3D bằng C#.<br>- Tạo cơ chế Input U, D, R, L, F, B.<br>- Xây dựng cấu trúc dữ liệu `RubikState`. |
