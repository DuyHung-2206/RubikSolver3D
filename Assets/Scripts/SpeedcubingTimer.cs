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
    /// Tự động bắt đầu nếu người chơi trực tiếp xoay khối khi đang Idle
    /// </summary>
    private void HandlePlayerMove(string move)
    {
        if (currentState == TimerState.Idle)
        {
            StartTimer();
        }
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
            case TimerState.Holding: return new Color(1f, 0.5f, 0f); // Màu cam
            case TimerState.Ready: return Color.green;               // Màu xanh lá
            case TimerState.Running: return Color.white;             // Màu trắng
            case TimerState.Finished: return isNewRecord ? Color.yellow : Color.cyan;
            default: return Color.white;
        }
    }

    // Tự động vẽ giao diện HUD lên màn hình nếu chưa có UI Canvas
    private void OnGUI()
    {
        if (timerTextTMP != null) return; // Nếu đã có TMP UI thì không vẽ OnGUI

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = GetStateColor();

        float screenWidth = Screen.width;
        // Hiển thị đồng hồ to ở chính giữa phía trên
        GUI.Label(new Rect(0, 20, screenWidth, 60), FormatTime(elapsedTime), style);

        // Hiển thị hướng dẫn & PB ở dưới đồng hồ
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 18,
            fontStyle = FontStyle.Normal
        };
        subStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        string statusPrompt = "";
        switch (currentState)
        {
            case TimerState.Idle: statusPrompt = "Giữ [SPACE] để chuẩn bị | Vuốt xoay để bắt đầu"; break;
            case TimerState.Holding: statusPrompt = "Giữ nguyên..."; break;
            case TimerState.Ready: statusPrompt = "SẴN SÀNG! Nhả [SPACE] để tính giờ"; break;
            case TimerState.Running: statusPrompt = "Đang giải... Bấm [SPACE] để dừng khẩn cấp"; break;
            case TimerState.Finished: statusPrompt = isNewRecord ? "🏆 KỶ LỤC MỚI ĐƯỢC THIẾT LẬP!" : "Hoàn thành! Giữ [SPACE] để chơi ván mới"; break;
        }

        string pbText = personalBest < float.MaxValue ? $"Kỷ lục cá nhân (PB): {FormatTime(personalBest)}" : "Chưa có kỷ lục";
        GUI.Label(new Rect(0, 80, screenWidth, 30), $"{statusPrompt}  |  {pbText}", subStyle);
    }
}