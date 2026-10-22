using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RubikStateManager : MonoBehaviour
{
    public RubikState CurrentState { get; private set; }
    // Sự kiện phát ra khi khối Rubik đã được giải hoàn tất
    public static event System.Action OnRubikSolved;
    // Sự kiện phát ra mỗi khi người chơi thực hiện 1 nước đi bất kỳ
    public static event System.Action<string> OnMoveExecuted;
    [Header("Lịch sử nước đi")]
    [Tooltip("Danh sách các nước đi có thể Undo")]
    private readonly Stack<string> undoMoveStack = new Stack<string>();
    [Tooltip("Danh sách các nước đi có thể Redo")]
    private readonly Stack<string> redoMoveStack = new Stack<string>();

    // Cờ đánh dấu đang thực thi quay lùi 3D để không ghi đè lại lịch sử
    [HideInInspector]
    public bool isExecutingUndoRedo = false;

    private RubikRotator rotator;
    private RubikGenerator generator;

    private void Awake()
    {
        CurrentState = new RubikState();
        rotator = GetComponent<RubikRotator>();
        generator = GetComponent<RubikGenerator>();
    }

    private void Update()
    {
        HandleShortcuts();
    }

    /// <summary>
    /// Phím tắt nhanh: Ctrl + Z để Undo, Ctrl + Y để Redo, Backspace để Reset
    /// </summary>
    private void HandleShortcuts()
    {
        if (Keyboard.current == null) return;

        bool ctrlPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;

        // Undo: Ctrl + Z
        if (ctrlPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            Undo3D();
        }

        // Redo: Ctrl + Y
        if (ctrlPressed && Keyboard.current.yKey.wasPressedThisFrame)
        {
            Redo3D();
        }

        // Reset: Phím Backspace hoặc Delete
        if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            ResetAll();
        }
    }

    /// <summary>
    /// Trả về danh sách các nước đi trong lịch sử (từ mới nhất đến cũ nhất).
    /// </summary>
    public List<string> GetUndoMoveHistory()
    {
        return new List<string>(undoMoveStack);
    }

    /// <summary>
    /// Được gọi từ RubikRotator mỗi khi một nước đi 3D hoàn thành.
    /// </summary>
    public void OnMoveCompleted(string move)
    {
        // 1. Luôn cập nhật ma trận logic trạng thái màu tương ứng với mô hình 3D
        CurrentState.ApplyMove(move);

        // 2. Chỉ ghi nhận vào stack Undo nếu không phải là đang chạy Undo/Redo/AutoSolve
        if (!isExecutingUndoRedo)
        {
            undoMoveStack.Push(move);
            redoMoveStack.Clear();
            Debug.Log($"[History] Đã ghi nhận nước: {move}. Tổng số nước trong Undo stack: {undoMoveStack.Count}");
            OnMoveExecuted?.Invoke(move);
        }

        // 3. Kiểm tra nếu khối Rubik đã giải xong hoàn toàn (cả 6 mặt đều đồng màu)
        if (CurrentState.IsSolved())
        {
            Debug.Log("<color=green><b>🎉 CHÚC MỪNG: KHỐI RUBIK ĐÃ ĐƯỢC GIẢI THÀNH CÔNG!</b></color>");
            undoMoveStack.Clear();
            redoMoveStack.Clear();
            OnRubikSolved?.Invoke();
        }
    }

    /// <summary>
    /// Hoàn tác nước đi trước đó cả trên mô hình 3D lẫn ma trận logic.
    /// </summary>
    [ContextMenu("Undo (Ctrl + Z)")]
    public void Undo3D()
    {
        if (rotator == null) rotator = GetComponent<RubikRotator>();

        if (undoMoveStack.Count == 0)
        {
            Debug.LogWarning("[Undo] Không có nước đi nào để hoàn tác!");
            return;
        }

        if (rotator.IsBusy || isExecutingUndoRedo)
        {
            Debug.LogWarning("[Undo] Khối Rubik đang bận xoay, vui lòng đợi!");
            return;
        }

        string lastMove = undoMoveStack.Pop();
        redoMoveStack.Push(lastMove);

        // Tìm nước đi nghịch đảo để xoay ngược lại
        string inverseMove = GetInverseMove(lastMove);
        Debug.Log($"<color=cyan>[Undo]</color> Hoàn tác nước '{lastMove}' bằng cách quay ngược '{inverseMove}'");

        StartCoroutine(ExecuteUndoRedoCoroutine(inverseMove));
    }

    /// <summary>
    /// Thực hiện lại nước đi vừa hoàn tác cả trên 3D lẫn ma trận logic.
    /// </summary>
    [ContextMenu("Redo (Ctrl + Y)")]
    public void Redo3D()
    {
        if (rotator == null) rotator = GetComponent<RubikRotator>();

        if (redoMoveStack.Count == 0)
        {
            Debug.LogWarning("[Redo] Không có nước đi nào để làm lại!");
            return;
        }

        if (rotator.IsBusy || isExecutingUndoRedo)
        {
            Debug.LogWarning("[Redo] Khối Rubik đang bận xoay, vui lòng đợi!");
            return;
        }

        string move = redoMoveStack.Pop();
        undoMoveStack.Push(move);

        Debug.Log($"<color=cyan>[Redo]</color> Thực hiện lại nước '{move}'");
        StartCoroutine(ExecuteUndoRedoCoroutine(move));
    }

    private IEnumerator ExecuteUndoRedoCoroutine(string move)
    {
        isExecutingUndoRedo = true;

        // Đẩy lệnh xoay 3D vào hàng đợi
        rotator.EnqueueMove(move);

        // Chờ đến khi hoạt ảnh 3D xoay xong hoàn toàn
        while (rotator.IsBusy)
        {
            yield return null;
        }

        isExecutingUndoRedo = false;
    }

    /// <summary>
    /// Đưa toàn bộ khối 3D và logic về trạng thái đã giải ban đầu (Solved).
    /// </summary>
    [ContextMenu("Reset All (Khôi phục ban đầu)")]
    public void ResetAll()
    {
        if (rotator == null) rotator = GetComponent<RubikRotator>();
        if (generator == null) generator = GetComponent<RubikGenerator>();

        StopAllCoroutines();
        isExecutingUndoRedo = false;

        // 1. Xóa sạch hàng đợi xoay
        if (rotator != null)
            rotator.ClearQueue();

        // 2. Khởi tạo lại toàn bộ 27 khối 3D ngay lập tức
        if (generator != null)
            generator.InitializeRubik();

        // 3. Đặt lại ma trận màu và xóa sạch lịch sử
        CurrentState.ResetSolved();
        undoMoveStack.Clear();
        redoMoveStack.Clear();

        Debug.Log("<color=green>[Reset] Đã khôi phục toàn bộ khối Rubik 3D và logic về trạng thái Solved ban đầu!</color>");
    }

    /// <summary>
    /// Tìm nước đi đảo chiều: R -> R', R' -> R, R2 -> R2
    /// </summary>
    public static string GetInverseMove(string move)
    {
        if (string.IsNullOrEmpty(move)) return string.Empty;

        // Nếu là nước 180 độ (ví dụ R2) thì nghịch đảo của nó vẫn là chính nó
        if (move.EndsWith("2"))
            return move;

        // Nếu có dấu nghịch đảo (') thì bỏ dấu ' để thành nước thuận
        if (move.EndsWith("'"))
            return move.Substring(0, move.Length - 1);

        // Nếu là nước thuận thì thêm dấu '
        return move + "'";
    }
    public bool IsSolved()
    {
        return CurrentState != null && CurrentState.IsSolved();
    }
}