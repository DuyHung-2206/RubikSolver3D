using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Sử dụng nếu bạn có TextMeshPro, nếu không script vẫn tự vẽ OnGUI tuyệt đẹp

public class SpeedcubingTimer : MonoBehaviour
{
    public enum TimerState
    {
        Idle,       // Chờ xuất phát
        Holding,    // Đang đè giữ phím Space (< 0.5s)
        Ready,      // Đã giữ đủ thời gian, sẵn sàng nhấc tay (> 0.5s)
        Running,    // Đang tính giờ chạy
        Finished    // Đã dừng khi giải xong
    }

    [Header("Trạng thái hiện tại")]
    public TimerState currentState = TimerState.Idle;

    [Header("Cài đặt chuẩn WCA")]
    [Tooltip("Thời gian cần giữ phím Space để kích hoạt Ready (giây)")]
    public float holdThreshold = 0.55f;

    [Header("Dữ liệu thời gian")]
    public float elapsedTime = 0f;
    public float personalBest = float.MaxValue;
    public bool isNewRecord = false;

    [Header("UI Tùy chọn (Để trống sẽ tự vẽ bằng OnGUI)")]
    public TextMeshProUGUI timerTextTMP;
    public TextMeshProUGUI pbTextTMP;

    private float holdTimer = 0f;
    private const string PB_KEY = "Rubik_Speedcubing_PB";

    private void Start()
    {
        // Tải kỷ lục cá nhân đã lưu từ trước
        personalBest = PlayerPrefs.GetFloat(PB_KEY, float.MaxValue);
    }

    private void OnEnable()
    {
        RubikStateManager.OnRubikSolved += HandleRubikSolved;
    }

    private void OnDisable()
    {
        RubikStateManager.OnRubikSolved -= HandleRubikSolved;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        currentState = TimerState.Idle;
        isNewRecord = false;
    }

    public void ClearPersonalBest()
    {
        personalBest = float.MaxValue;
        isNewRecord = false;
        PlayerPrefs.DeleteKey(PB_KEY);
        PlayerPrefs.Save();
        UpdateTMPDisplay();
    }

    private void Update()
    {
        HandleTimerInput();

        // Cập nhật đếm thời gian khi đang Running
        if (currentState == TimerState.Running)
        {
            elapsedTime += Time.deltaTime;
        }

        UpdateTMPDisplay();
    }

    private void HandleTimerInput()
    {
        if (Keyboard.current == null) return;
        if (RubikRotator.IsUserTypingInInputField()) return;

        // Không nhận Space của Timer nếu đang mở bảng Quét Camera (vì Space dùng để chụp mặt)
        var scanner = FindAnyObjectByType<RubikCameraScanner>();
        if (scanner != null && scanner.scannerPanel != null && scanner.scannerPanel.activeInHierarchy)
            return;

        bool spacePressed = Keyboard.current.spaceKey.isPressed;
        bool spaceReleased = Keyboard.current.spaceKey.wasReleasedThisFrame;

        // 1. Nếu đang ở trạng thái Idle hoặc Finished: Nhấn Space để chuẩn bị
        if (currentState == TimerState.Idle || currentState == TimerState.Finished)
        {
            if (spacePressed)
            {
                currentState = TimerState.Holding;
                holdTimer = 0f;
            }
        }
        // 2. Đang giữ Space: Tích lũy thời gian
        else if (currentState == TimerState.Holding)
        {
            if (spacePressed)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdThreshold)
                {
                    currentState = TimerState.Ready; // SẴN SÀNG!
                }
            }
            else
            {
                // Nhả ra quá sớm trước 0.5s -> quay về Idle
                currentState = TimerState.Idle;
            }
        }
        // 3. Đã Ready: Khi nhả phím Space ra -> XUẤT PHÁT!
        else if (currentState == TimerState.Ready)
        {
            if (spaceReleased)
            {
                StartTimer();
            }
        }
        // 4. Đang chạy mà người chơi bấm Space -> Dừng khẩn cấp thủ công
        else if (currentState == TimerState.Running)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StopTimer();
            }
        }
    }

    /// <summary>
    /// Bắt đầu bấm giờ
    /// </summary>
    public void StartTimer()
    {
        elapsedTime = 0f;
        isNewRecord = false;
        currentState = TimerState.Running;
        Debug.Log("<color=yellow>[Timer] BẮT ĐẦU TÍNH GIỜ!</color>");
    }

    /// <summary>
    /// Tự động dừng chính xác khi Rubik được giải hoàn tất
    /// </summary>
    private void HandleRubikSolved()
    {
        if (currentState == TimerState.Running)
        {
            StopTimer();
        }
    }

    public void StopTimer()
    {
        currentState = TimerState.Finished;

        // Kiểm tra và lưu kỷ lục mới (PB)
        if (elapsedTime < personalBest)
        {
            personalBest = elapsedTime;
            isNewRecord = true;
            PlayerPrefs.SetFloat(PB_KEY, personalBest);
            PlayerPrefs.Save();
            Debug.Log($"<color=green><b>🏆 KỶ LỤC CÁ NHÂN MỚI (PB): {FormatTime(personalBest)}!</b></color>");
        }
        else
        {
            Debug.Log($"[Timer] Hoàn thành với thời gian: {FormatTime(elapsedTime)}");
        }
    }

    /// <summary>
    /// Định dạng thời gian chuẩn: mm:ss.fff (Phút:Giây.Mili-giây)
    /// </summary>
    public static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0f || float.IsInfinity(timeInSeconds) || float.IsNaN(timeInSeconds))
            return "00:00.000";

        int minutes = (int)(timeInSeconds / 60);
        float seconds = timeInSeconds % 60;
        return string.Format("{0:00}:{1:00.000}", minutes, seconds);
    }

    private void UpdateTMPDisplay()
    {
        if (timerTextTMP != null)
        {
            timerTextTMP.text = FormatTime(elapsedTime);
            timerTextTMP.color = GetStateColor();
        }

        if (pbTextTMP != null)
        {
            pbTextTMP.text = personalBest < float.MaxValue ? $"PB: {FormatTime(personalBest)}" : "PB: --:--.---";
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case TimerState.Holding: return new Color32(0xEA, 0x58, 0x0C, 255);  // Cam đậm
            case TimerState.Ready: return new Color32(0x16, 0xA3, 0x4A, 255);    // Xanh lá
            case TimerState.Running: return new Color32(0x25, 0x63, 0xEB, 255);  // Xanh dương
            case TimerState.Finished: return isNewRecord ? new Color32(0xD9, 0x77, 0x06, 255) : new Color32(0x0D, 0x94, 0x88, 255);
            default: return new Color32(0x47, 0x55, 0x69, 255);                  // Xám Slate
        }
    }

    // Tự động vẽ giao diện HUD lên màn hình nếu chưa có UI Canvas
    private void OnGUI()
    {
        if (timerTextTMP != null) return; // Nếu đã có TMP UI thì không vẽ OnGUI
    }
}
