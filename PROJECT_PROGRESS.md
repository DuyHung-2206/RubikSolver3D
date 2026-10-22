# 📊 TIẾN TRÌNH DỰ ÁN RUBIK SOLVER 3D (PROJECT TRACKER)

> **Cập nhật lần cuối:** 22/10/2026 - 00:05 (Review Phase 5 UI/UX & Phát hiện Bug State)  
> **Trạng thái hiện tại:** `Phase 1 (100%) | Phase 2 (100%) | Phase 3 (75%) | Phase 5 (Đạt 90% - UI/UX, Timer, Scramble)`  
> **Phiên bản Unity:** `Unity 6 (6000.6.5f1)`  
> **Render Pipeline:** Universal Render Pipeline (URP)  
> **Input System:** Unity New Input System (`UnityEngine.InputSystem`)  

---

## 🚨 NGUYÊN TẮC BẮT BUỘC KHI LÀM VIỆC VỚI DỰ ÁN (MANDATORY RULES)

> [!CAUTION]
> **TẤT CẢ các AI Assistant và người đóng góp phải tuân thủ nghiêm ngặt 2 nguyên tắc sau:**
> 1. **Chỉ sửa đổi khi được yêu cầu rõ ràng:** Tuyệt đối không tự ý chỉnh sửa code, tài nguyên hay cấu hình trừ khi có yêu cầu cụ thể từ User. Khi User chat có chữ **`review`** (hoặc yêu cầu xem xét/đánh giá), AI **chỉ được phép phân tích, đánh giá, đưa ra nhận xét**; **TUYỆT ĐỐI KHÔNG ĐƯỢC CHỈNH SỬA BẤT KỲ FILE NÀO**. Sau khi đánh giá xong, thêm tiến trình mới kế tiếp tiến trình cũ vào file `PROJECT_PROGRESS.md`.
> 2. **Không tự ý đẩy code lên GitHub:** Tuyệt đối **KHÔNG** chạy `git push` tự động trong bất kỳ tình huống nào. Chỉ khi nào User chat chính xác cụm từ **`"đẩy lên github"`** thì mới được phép thực hiện lệnh đẩy code.

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
| **Phase 2** | **Cơ chế xoay mặt & Quản lý trạng thái** | ✅ Hoàn thành | **100%** |
| **Phase 3** | **Tương tác chuột/cảm ứng kéo mặt (Face Dragging)** | 🟢 Đạt chuẩn cơ bản | **75%** |
| **Phase 4** | **Thuật toán giải Rubik tự động (Solver Algorithm)** | 🟢 Đang hoàn thiện | **85%** |
| **Phase 5** | **Giao diện người dùng (UI/UX) & Chế độ chơi** | 🟢 Đạt chuẩn cao | **95%** |
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

### 🟢 Phase 2: Cơ chế xoay & Trạng thái logic (Đã đạt 90%)
- [x] Tạo `RubikRotator.cs` hỗ trợ xoay các mặt chuẩn WCA: `U, D, R, L, F, B` và các mặt nghịch đảo (`'`) thông qua Shift.
- [x] Hoạt ảnh xoay mặt mượt mà với đường cong chuyển động `AnimationCurve.EaseInOut`.
- [x] Xử lý hàng đợi lệnh xoay (`moveQueue`) tránh xung đột khi bấm liên tục.
- [x] Tạo `RubikState.cs` cấu trúc mảng 6 mặt $\times$ 9 ô.
- [x] Tạo `RubikStateManager.cs` hỗ trợ Undo/Redo stack.
- [x] **[HOÀN THÀNH]** Hoàn thiện logic luân chuyển 4 dải biên (adjacent edges & corners) cho 6 mặt trong `RubikState.RotateFaceClockwise()`.
- [x] **[HOÀN THÀNH]** Kết nối đồng bộ hai chiều giữa `RubikRotator` (3D Transform) và `RubikStateManager` (Logic Matrix) sau mỗi hoạt ảnh xoay.
- [x] **[HOÀN THÀNH]** Xử lý xoay các tầng giữa (M, E, S) cả về mặt 3D Transform lẫn ma trận trạng thái logic `RubikState`.

### 🟢 Phase 3: Tương tác chạm / Kéo chuột trực tiếp (Face Dragging - Đạt 75%)
- [x] Bắn Raycast nhận diện đúng mảnh cubie và mặt sticker mà chuột nhấp trúng (`RubikDragController.cs`).
- [x] Phân tích vector kéo chuột (Drag Vector) chiếu lên không gian 2D màn hình để suy ra hướng kéo 3D (`RubikDragController.cs`).
- [x] Ánh xạ hướng kéo và vị trí viên Rubik thành nước đi chuẩn: 6 mặt ngoài (`U, D, R, L, F, B`) và 3 tầng giữa (`M, E, S`).
- [x] Tách biệt điều khiển chuột: Chuột phải xoay Camera, Chuột trái dành riêng cho việc kéo mặt Rubik (`RubikCameraOrbit.cs`).
- [x] Cơ chế bảo vệ `rotator.IsRotating` chống xung đột nhận thao tác kéo mới khi hoạt ảnh đang xoay.
- [ ] (Tùy chọn nâng cao) Cơ chế bám tay tạm thời (Drag preview rotation) trước khi nhả chuột để chốt góc xoay $90^\circ$.
- [ ] Hỗ trợ đa cảm ứng (Multi-touch) cho thiết bị di động.

### 🟢 Phase 4: Thuật toán giải Rubik tự động (Rubik Solver - Đạt 85%)
- [x] Chuyển đổi trạng thái hiện tại từ `RubikState` thành chuỗi 54 ký tự URFDLB quy ước quốc tế (`GetCubeDefinitionString()`).
- [x] Tạo thuật toán nghịch đảo chuỗi nước đi với thứ tự toán học chuẩn $(M_1 \dots M_k)^{-1} = M_k^{-1} \dots M_1^{-1}$ từ `undoMoveStack`.
- [x] Thuật toán rút gọn chuỗi nước đi `SimplifyMoves`: tự động gộp các nước cùng mặt ($R + R = R2$), triệt tiêu các nước nghịch đảo ($R + R' = \emptyset$), rút gọn qua các mặt đối diện giao hoán ($R L R' = L$).
- [x] Tích hợp thuật toán BFS tìm kiếm nghiệm tối ưu cho các trạng thái scramble ngắn hạn.
- [x] Tự động xác thực công thức giải bằng mô phỏng trên bản sao `RubikState.Clone()` trước khi trả về.
- [x] Chế độ xem từng bước giải (Step-by-step viewer: `RubikSolutionViewer.cs`) hỗ trợ Next, Previous, Tự động giải (Auto Solve), hiển thị toàn bộ chuỗi công thức và tô màu nước đang đi.
- [ ] Tích hợp trọn vẹn Kociemba Two-Phase cho các khối Rubik nhập từ camera quét ngoài không có lịch sử.

### 🟢 Phase 5: Giao diện UI/UX & Tính năng bổ trợ (Đạt 95%)
- [x] Tạo `RubikScrambler.cs` tự sinh chuỗi 20-25 nước đi chuẩn WCA, lọc trùng mặt & cùng trục đối xứng, tốc độ trộn cao (`fastScrambleDuration = 0.04s`).
- [x] Nút Undo / Redo trên 3D: `RubikStateManager.Undo3D()` và `Redo3D()` với `GetInverseMove()` đảo chiều xoay trực quan.
- [x] Nút Reset: `RubikStateManager.ResetAll()` khôi phục khối 3D và ma trận logic về Solved state ngay lập tức.
- [x] Bộ đếm thời gian chuyên nghiệp: `SpeedcubingTimer.cs` với 5 trạng thái (Idle, Holding, Ready, Running, Finished), tự vẽ OnGUI fallback hoặc liên kết TextMeshPro, tự động dừng khi `OnRubikSolved`, lưu PB qua `PlayerPrefs`.
- [x] Tạo `RubikUIController.cs`: Kết nối đầy đủ nút Scramble, Start, Undo, Redo, Reset, SpeedSlider, Victory Overlay với bảng màu Flat Design hiện đại. Tự động khóa tương tác khi `rotator.IsBusy`.
- [x] **[HOÀN THÀNH]** Nút Giải tự động (Solve): Kết nối `RubikSolutionViewer` và `RubikSolver`, tự động giải đưa về 6 mặt đồng màu và phát sinh chiến thắng.
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
│   ├── RubikGenerator.cs      # Khởi tạo mô hình 3D, chia lưới 3x3x3, dán nhãn màu URP
│   ├── RubikRotator.cs        # Logic xoay các tầng (hỗ trợ 180°), Coroutine hoạt ảnh, nhận diện phím
│   ├── RubikDragController.cs # Bắn Raycast kéo thả mặt Rubik bằng chuột trực tiếp
│   ├── RubikState.cs          # Mô hình dữ liệu toán học (màu 6 mặt x 9 ô), xoay mảng
│   ├── RubikStateManager.cs   # Quản lý lịch sử Undo / Redo, Validate tính hợp lệ, đồng bộ logic
│   ├── RubikSolver.cs         # Tìm kiếm lời giải tối ưu (LIFO Inverse + SimplifyMoves + BFS)
│   ├── RubikSolutionViewer.cs # Điều khiển hiển thị và tự động thực thi từng bước giải
│   ├── RubikScrambler.cs      # Trộn Rubik tự động theo quy tắc chuẩn WCA 20-25 nước
│   ├── RubikUIController.cs   # Quản lý UI Flat Design, thanh công cụ, các nút tương tác
│   ├── SpeedcubingTimer.cs    # Bộ đếm giờ thể thao Speedcubing WCA
│   └── RubikCameraOrbit.cs    # Điều khiển Camera tự do (Orbit, Clamp angle, Zoom)
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

## 📜 6. NHẬT KÝ CẬP NHẬT (WORK LOG)

| Ngày | Người thực hiện | Nội dung cập nhật |
| :---: | :---: | :--- |
| **22/10/2026 - 16:05** | Antigravity AI | - **Khắc phục triệt để lỗi nút Giải (Solve) & Tối ưu hóa chuỗi công thức giải đưa về 6 mặt đồng màu:**<br>  + **Sửa lỗi đảo ngược thứ tự lịch sử (LIFO history inversion):** Trong `RubikSolver.cs`, sửa vòng lặp duyệt `undoMoveStack` từ xuôi sang đúng thứ tự toán học $(M_1 \dots M_k)^{-1} = M_k^{-1} \dots M_1^{-1}$, giúp triệt tiêu hoàn toàn hiện tượng khối bị xáo trộn thêm sau khi giải.<br>  + **Hỗ trợ nước đi $180^\circ$ ("2"):** Trong `RubikRotator.cs`, bổ sung cho phép ký tự `'2'` qua hàm `EnqueueMove` và nhân đôi góc quay `angle *= 2f` trong `ExecuteMove`, ngăn chặn tình trạng các nước đi $180^\circ$ (như $R2, U2, F2$) bị bỏ qua âm thầm.<br>  + **Đồng bộ trạng thái logic `CurrentState` liên tục:** Trong `RubikStateManager.cs`, luôn gọi `CurrentState.ApplyMove(move)` khi hoạt ảnh hoàn thành (kể cả khi `isExecutingUndoRedo == true`), giúp ma trận màu luôn khớp $100\%$ với mô hình 3D và tự động kích hoạt `OnRubikSolved` khi khối đạt 6 mặt đồng màu.<br>  + **Thuật toán rút gọn chuỗi nước đi `SimplifyMoves`:** Gộp các nước cùng mặt ($R + R = R2$), triệt tiêu các nước nghịch đảo ($R + R' = \emptyset$), và rút gọn qua mặt đối diện giao hoán ($R L R' = L$), kèm bước mô phỏng kiểm tra `VerifySolution` trên `RubikState.Clone()`.<br>  + **Tích hợp thuật toán BFS ngắn hạn:** Hỗ trợ tìm kiếm đường đi ngắn nhất giải các khối Rubik ít nước scramble khi không có lịch sử.<br>  + **Cải tiến `RubikSolutionViewer.cs` & `RubikUIController.cs`:** Hoàn thiện Step-by-step viewer, tự động giải mượt mà và mở khóa nút an toàn.<br>  + Biên dịch dự án: **0 Warning, 0 Error**. Nâng tiến độ Phase 4 lên **85%**, Phase 5 lên **95%**. |
| **22/10/2026 - 00:05** | Antigravity AI | - **Review Phase 5 (Giao diện UI/UX & Tính năng bổ trợ):**<br>  + Đánh giá toàn diện 3 script mới: `RubikScrambler.cs` (WCA Scramble 20-25 nước, tránh lặp trục/mặt), `SpeedcubingTimer.cs` (Bộ đếm giờ 5 trạng thái WCA, lưu PB qua PlayerPrefs), `RubikUIController.cs` (Quản lý nút bấm, Flat Design, Victory Overlay).<br>  + Đánh giá tính năng Undo/Redo 3D và ResetAll trong `RubikStateManager.cs`.<br>  + **Phát hiện Bug quan trọng:** Tại `RubikStateManager.OnMoveCompleted`, lệnh `undoMoveStack.Push(move); CurrentState.ApplyMove(move);` bị dán lặp lại 2 lần khiến mỗi nước đi bị thực hiện 2 lần trong logic state.<br>  + Nâng tiến độ Phase 5 lên **90%**.<br>  + Tuân thủ quy tắc an toàn: Không push code có bug lên GitHub; chờ xác nhận sửa bug và lệnh chính xác `"đẩy lên github"`. |
| **21/10/2026 - 17:30** | Antigravity AI | - **Review Phase 3 & Đánh giá tương tác chuột (Face Dragging):**<br>  + Kiểm tra toàn diện thuật toán chiếu hình học 2D $\leftrightarrow$ 3D trong `RubikDragController.cs`. Cơ chế nhận diện qua ScreenPointToRay và tích có hướng giữa pháp tuyến và vector tiếp tuyến hoạt động chính xác.<br>  + Đã tích hợp hỗ trợ đầy đủ cả 6 mặt ngoài (`U, D, R, L, F, B`) và 3 tầng giữa (`M, E, S`).<br>  + Tách biệt hoàn hảo hệ thống input chuột giữa Camera Orbit (Chuột phải) và Kéo mặt (Chuột trái).<br>  + Chạy nghiệm thu kiểm thử logic toán học độc lập trên .NET 10: Xoay 4 lần & nghịch đảo của M, E, S đạt **100% PASS**.<br>  + Nâng tiến độ Phase 3 lên **75%** (Đã đạt chuẩn cơ bản để chơi và tương tác hoàn toàn bằng chuột). |
| **21/10/2026 - 17:22** | Antigravity AI | - **Bổ sung phím tắt bàn phím cho 3 tầng giữa M, E, S:**<br>  + Gắn phím `M`, `E`, `S` (kết hợp `Shift` để xoay `M'`, `E'`, `S'`) trong `HandleKeyboardInput()` của `RubikRotator.cs`.<br>  + Cho phép người chơi xoay cả bằng phím tắt lẫn kéo thả chuột trên màn hình, đồng bộ chuẩn xác với `RubikState`. |
| **21/10/2026 - 17:15** | Antigravity AI | - **Khắc phục triệt để lỗi vỡ khối hình chữ H và lỗi xoay tầng giữa (M, E, S):**<br>  + Sửa lỗi lọc trục toạ độ sai trong `RubikRotator.cs`: 'M' lọc theo trục X, 'E' theo trục Y, 'S' theo trục Z thay vì bị dồn toàn bộ về Z làm văng khối 3D ra ngoài khoảng không.<br>  + Đưa các `case 'M', 'E', 'S'` trong `switch (face)` về vị trí hợp lệ trước `default:`.<br>  + Bổ sung logic hoán vị mảng 3 tầng giữa `RotateMiddleSliceM()`, `RotateMiddleSliceE()`, `RotateMiddleSliceS()` vào `RubikState.cs` giúp triệt tiêu hoàn toàn ngoại lệ `ArgumentException: Mặt xoay không hợp lệ`.<br>  + Cập nhật `RubikDragController.cs`: Thêm cờ khóa `rotator.IsRotating` chống nhận tương tác kéo mới khi hoạt ảnh đang quay; chuyển sang dùng `InverseTransformPoint` để tính tọa độ viên rubik an toàn.<br>  + Bổ sung context menu test tầng giữa trong `RubikStateManager.cs` (xoay 4 lần & nghịch đảo: **100% PASS**).<br>  + Nâng tiến độ Phase 2 lên **100% (Hoàn thành)**, Phase 3 đạt **50%**. |
| **21/10/2026 - 16:44** | Antigravity AI | - **Review tổng thể Scene & Độ sẵn sàng Phase 3:**<br>  + Ghi nhận cập nhật tọa độ Main Camera trong `MainScene.unity` để đóng khung khối Rubik chuẩn xác.<br>  + Kiểm tra độ toàn vẹn của các scripts: `RubikGenerator.cs`, `RubikCameraOrbit.cs`, `RubikRotator.cs`, `RubikState.cs`, `RubikStateManager.cs`.<br>  + Toàn bộ hệ thống Phase 1 (Dựng hình khối) và Phase 2 (Xoay, Hoạt ảnh, Đồng bộ State) hoạt động ăn khớp, sẵn sàng chuyển tiếp sang **Phase 3: Tương tác chạm / Kéo chuột trực tiếp (Face Dragging)**. |
| **21/10/2026 - 16:28** | Antigravity AI | - **Review & Kiểm thử nghiệm thu Phase 2:**<br>  + Kiểm tra toàn bộ mã nguồn `RubikState.cs`, `RubikRotator.cs`, `RubikStateManager.cs`.<br>  + Đã tích hợp hàm hoán vị 4 dải biên `RotateAdjacentStrips(face)` và đồng bộ trạng thái khi kết thúc hoạt ảnh xoay 3D.<br>  + Chạy kiểm thử tự động 4 kịch bản toán học: (1) Xoay từng mặt 4 lần $X \times 4 =$ Solved, (2) Nghịch đảo $X + X' =$ Solved, (3) Sexy Move $(R\ U\ R'\ U') \times 6 =$ Solved, (4) T-Perm $\times 2 =$ Solved $\rightarrow$ **100% PASS**.<br>  + Nâng tiến độ Phase 2 từ **65% lên 90%** (Sẵn sàng để bước sang Phase 3: Tương tác kéo mặt bằng chuột/cảm ứng). |
| **21/10/2026** | Antigravity AI | - **Review Bước 2.7**: Phân tích sự sai lệch giữa Mô hình 3D (`RubikRotator`) và Logic (`RubikState`).<br>- Phát hiện `RotateFaceClockwise` chưa hoán vị 4 mặt biên và `RubikRotator` chưa gọi `RubikStateManager`.<br>- Cập nhật lại tỉ lệ thực tế Phase 2 về **65%** để phản ánh đúng hiện trạng. |
| **21/10/2026** | Antigravity AI | - Thiết lập 2 nguyên tắc bắt buộc: Chỉ sửa khi có yêu cầu (từ khóa "review" chỉ đánh giá), không tự ý đẩy lên GitHub trừ khi chat "đẩy lên github".<br>- Khởi tạo tài liệu `AGENTS.md` và cập nhật `PROJECT_PROGRESS.md`. |
| **21/10/2026** | Antigravity AI | - Khởi tạo tài liệu theo dõi tiến trình `PROJECT_PROGRESS.md`.<br>- Thiết lập git và đẩy dự án lên GitHub `DuyHung-2206/RubikSolver3D`. |
| **Giai đoạn trước** | Developer | - Xây dựng bộ khung Rubik 3D bằng C#.<br>- Tạo cơ chế Input U, D, R, L, F, B.<br>- Xây dựng cấu trúc dữ liệu `RubikState`. |
