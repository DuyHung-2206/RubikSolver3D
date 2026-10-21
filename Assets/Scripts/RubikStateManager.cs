
using System.Collections.Generic;
using UnityEngine;

public class RubikStateManager : MonoBehaviour
{
    public RubikState CurrentState { get; private set; }

    private readonly Stack<RubikState> undoStack =
        new Stack<RubikState>();

    private readonly Stack<RubikState> redoStack =
        new Stack<RubikState>();

    private void Awake()
    {
        CurrentState = new RubikState();
    }

    public void SaveBeforeChange()
    {
        undoStack.Push(CurrentState.Clone());
        redoStack.Clear();
    }

    public bool Undo()
    {
        if (undoStack.Count == 0)
            return false;

        redoStack.Push(CurrentState.Clone());
        CurrentState = undoStack.Pop();

        Debug.Log("[Rubik State] Đã khôi phục trạng thái trước.");
        return true;
    }

    public bool Redo()
    {
        if (redoStack.Count == 0)
            return false;

        undoStack.Push(CurrentState.Clone());
        CurrentState = redoStack.Pop();

        Debug.Log("[Rubik State] Đã khôi phục trạng thái tiếp theo.");
        return true;
    }

    public void ResetState()
    {
        CurrentState.ResetSolved();
        undoStack.Clear();
        redoStack.Clear();

        Debug.Log("[Rubik State] Đã đặt dữ liệu về trạng thái đã giải.");
    }

    public bool ValidateState()
    {
        if (CurrentState == null)
        {
            Debug.LogError("[Rubik State] Chưa khởi tạo dữ liệu.");
            return false;
        }

        bool valid = CurrentState.HasValidColorCounts();

        Debug.Log(
            valid
                ? "[Rubik State] Sáu màu đều có đúng 9 ô."
                : "[Rubik State] Số lượng màu không hợp lệ."
        );

        return valid;
    }

    public bool IsSolved()
    {
        return CurrentState != null && CurrentState.IsSolved();
    }

    public string GetFaceletString()
    {
        return CurrentState == null
            ? string.Empty
            : CurrentState.ToFaceletString();
    }

    [ContextMenu("Test: Rotate R Four Times")]
    private void TestRotateFourTimes()
    {
        RubikState testState = new RubikState();
        RubikState original = testState.Clone();

        testState.ApplyMove("R");
        testState.ApplyMove("R");
        testState.ApplyMove("R");
        testState.ApplyMove("R");

        Debug.Log(
            "[Test R4] " +
            (testState.HasSameState(original) ? "PASS" : "FAIL")
        );
    }

}
