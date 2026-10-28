using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RubikSolver : MonoBehaviour
{
    private RubikStateManager stateManager;
    private static readonly object kociembaLock = new object();
    private static bool isWarmingUpTables = false;
    private static bool tablesWarmedUp = false;

    public string LastErrorMessage { get; private set; } = string.Empty;

    private void Awake()
    {
        stateManager = GetComponent<RubikStateManager>();
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();
    }

    private void Start()
    {
        WarmupTablesInBackground();
    }

    /// <summary>
    /// Khởi tạo trước bảng tra cứu Kociemba (Move Tables & Pruning Tables) trên luồng phụ
    /// để khi người dùng nhấn Giải tự động sẽ không phải chờ khởi tạo bảng lần đầu.
    /// </summary>
    public static void WarmupTablesInBackground()
    {
        if (tablesWarmedUp || isWarmingUpTables)
            return;

        isWarmingUpTables = true;
        Task.Run(() =>
        {
            try
            {
                lock (kociembaLock)
                {
                    if (!tablesWarmedUp)
                    {
                        string info;
                        Kociemba.SearchRunTime.solution(
                            "UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBB",
                            out info, maxDepth: 1, timeOut: 1000);
                        tablesWarmedUp = true;
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu có ngoại lệ trong lúc khởi tạo ngầm
            }
            finally
            {
                isWarmingUpTables = false;
            }
        });
    }

    /// <summary>
    /// Chuyển đổi RubikState hiện tại thành chuỗi 54 ký tự URFDLB chuẩn quy ước quốc tế.
    /// </summary>
    public string GetCubeDefinitionString()
    {
        if (stateManager == null || stateManager.CurrentState == null)
            return string.Empty;

        string cubeDef = GetCubeDefinitionStringFromState(stateManager.CurrentState, out string err);
        if (!string.IsNullOrEmpty(err))
            LastErrorMessage = err;
        return cubeDef;
    }

    /// <summary>
    /// Chuyển đổi một bản sao RubikState bất kỳ thành chuỗi 54 ký tự URFDLB (An toàn đa luồng - Thread-safe).
    /// </summary>
    public static string GetCubeDefinitionStringFromState(RubikState state, out string errorMsg)
    {
        errorMsg = string.Empty;
        if (state == null)
            return string.Empty;

        // Xác định màu của 6 tâm để ánh xạ: Tâm nào thì mang tên mặt đó
        char colorU = state.GetColor('U', 1, 1);
        char colorR = state.GetColor('R', 1, 1);
        char colorF = state.GetColor('F', 1, 1);
        char colorD = state.GetColor('D', 1, 1);
        char colorL = state.GetColor('L', 1, 1);
        char colorB = state.GetColor('B', 1, 1);

        char[] centers = { colorU, colorR, colorF, colorD, colorL, colorB };
        HashSet<char> centerSet = new HashSet<char>(centers);
        if (centerSet.Count != 6)
        {
            errorMsg = "6 tâm mặt không đủ 6 màu khác nhau. Vui lòng kiểm tra lại màu các mặt.";
            return string.Empty;
        }

        Dictionary<char, char> colorToFace = new Dictionary<char, char>()
        {
            { colorU, 'U' },
            { colorR, 'R' },
            { colorF, 'F' },
            { colorD, 'D' },
            { colorL, 'L' },
            { colorB, 'B' }
        };

        // Chuẩn Kociemba duyệt theo thứ tự: U (9 ô) -> R (9 ô) -> F (9 ô) -> D (9 ô) -> L (9 ô) -> B (9 ô)
        char[] faceOrder = { 'U', 'R', 'F', 'D', 'L', 'B' };
        System.Text.StringBuilder sb = new System.Text.StringBuilder(54);

        foreach (char f in faceOrder)
        {
            char[] faceletColors = state.GetFace(f);
            foreach (char c in faceletColors)
            {
                if (colorToFace.ContainsKey(c))
                    sb.Append(colorToFace[c]);
                else
                    sb.Append('?');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tìm lời giải cho khối Rubik hiện tại (Đồng bộ trên Main Thread).
    /// Trả về danh sách các nước đi chuẩn (ví dụ: ["R", "U", "R'", "F2", ...])
    /// </summary>
    public List<string> SolveCurrentState()
    {
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();

        if (stateManager == null || stateManager.CurrentState == null)
        {
            LastErrorMessage = "Không tìm thấy trạng thái khối Rubik hiện tại.";
            return new List<string>();
        }

        RubikState snapshot = stateManager.CurrentState.Clone();
        List<string> historySnapshot = stateManager.GetUndoMoveHistory();

        List<string> result = SolveStateSnapshot(snapshot, historySnapshot, out string err);
        LastErrorMessage = err;
        return result;
    }

    /// <summary>
    /// Tìm lời giải trên bản sao trạng thái RubikState (Hoàn toàn Thread-Safe, chạy được trong Task.Run để không gây đơ màn hình).
    /// </summary>
    public List<string> SolveStateSnapshot(RubikState stateSnapshot, List<string> historySnapshot, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (stateSnapshot == null)
        {
            errorMessage = "Trạng thái khối Rubik rỗng.";
            LastErrorMessage = errorMessage;
            return new List<string>();
        }

        if (stateSnapshot.IsSolved())
        {
            LastErrorMessage = string.Empty;
            return new List<string>();
        }

        // 1. Lấy lịch sử nước đi từ StateManager (nếu người dùng tự xoay trên 3D)
        if (historySnapshot != null && historySnapshot.Count > 0)
        {
            List<string> rawInverses = new List<string>();
            for (int i = 0; i < historySnapshot.Count; i++)
            {
                rawInverses.Add(RubikStateManager.GetInverseMove(historySnapshot[i]));
            }

            List<string> optimized = SimplifyMoves(rawInverses);

            if (VerifySolutionOnState(stateSnapshot, optimized))
            {
                LastErrorMessage = string.Empty;
                return optimized;
            }

            if (VerifySolutionOnState(stateSnapshot, rawInverses))
            {
                LastErrorMessage = string.Empty;
                return rawInverses;
            }
        }

        // 2. Thử giải nhanh bằng BFS với độ sâu <= 3
        var bfsSolution = SolveByBFS(stateSnapshot, 3);
        if (bfsSolution != null && bfsSolution.Count > 0 && VerifySolutionOnState(stateSnapshot, bfsSolution))
        {
            LastErrorMessage = string.Empty;
            return bfsSolution;
        }

        // 3. Giải trạng thái bất kỳ (từ Camera Scanner hoặc Net Color Picker) bằng thuật toán Kociemba Two-Phase
        string cubeDef = GetCubeDefinitionStringFromState(stateSnapshot, out string defErr);
        if (string.IsNullOrEmpty(cubeDef) || cubeDef.Length != 54 || cubeDef.Contains("?"))
        {
            errorMessage = !string.IsNullOrEmpty(defErr)
                ? defErr
                : "Không thể tạo chuỗi định nghĩa Rubik: các mặt bị thiếu ô hoặc trùng màu tâm.";
            LastErrorMessage = errorMessage;
            return new List<string>();
        }

        // Kiểm tra tính hợp lệ vật lý trước khi giải
        int verifyResult = Kociemba.Tools.verify(cubeDef);
        if (verifyResult != 0)
        {
            errorMessage = GetVerifyErrorMessage(verifyResult);
            LastErrorMessage = errorMessage;
            return new List<string>();
        }

        try
        {
            string info;
            string solutionResult;
            lock (kociembaLock)
            {
                solutionResult = Kociemba.SearchRunTime.solution(cubeDef, out info, maxDepth: 22, timeOut: 6000);
                tablesWarmedUp = true;
            }

            if (string.IsNullOrEmpty(solutionResult))
            {
                LastErrorMessage = string.Empty;
                return new List<string>();
            }

            if (solutionResult.StartsWith("Error"))
            {
                errorMessage = GetSearchErrorMessage(solutionResult);
                LastErrorMessage = errorMessage;
                return new List<string>();
            }

            string[] tokens = solutionResult.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> kociembaMoves = new List<string>();
            foreach (var token in tokens)
            {
                string t = token.Trim();
                if (t == "." || string.IsNullOrEmpty(t)) continue;
                kociembaMoves.Add(t);
            }

            List<string> optimizedKociemba = SimplifyMoves(kociembaMoves);
            if (VerifySolutionOnState(stateSnapshot, optimizedKociemba))
            {
                LastErrorMessage = string.Empty;
                return optimizedKociemba;
            }

            if (VerifySolutionOnState(stateSnapshot, kociembaMoves))
            {
                LastErrorMessage = string.Empty;
                return kociembaMoves;
            }

            errorMessage = "Chuỗi nước đi tìm được không đưa được khối Rubik về trạng thái hoàn chỉnh.";
            LastErrorMessage = errorMessage;
            return new List<string>();
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi khi thực thi thuật toán giải: {ex.Message}";
            LastErrorMessage = errorMessage;
            return new List<string>();
        }
    }

    private string GetVerifyErrorMessage(int code)
    {
        switch (code)
        {
            case -1:
                return "Số lượng màu không hợp lệ: Mỗi màu phải có đúng 9 ô.";
            case -2:
                return "Lỗi cạnh: Không đủ 12 viên cạnh độc nhất (có thể do quét sai màu ô cạnh).";
            case -3:
                return "Lỗi lật cạnh (Flip error): Có viên cạnh bị lật ngược bất thường.";
            case -4:
                return "Lỗi góc: Không đủ 8 viên góc độc nhất (có thể do quét sai màu ô góc).";
            case -5:
                return "Lỗi xoay góc (Twist error): Có viên góc bị xoay lệch hướng.";
            case -6:
                return "Lỗi hoán vị (Parity error): Hai viên bị tráo đổi vị trí không thể giải bằng các phép quay thông thường.";
            default:
                return $"Trạng thái khối Rubik không hợp lệ (Mã kiểm tra: {code}).";
        }
    }

    private string GetSearchErrorMessage(string errorStr)
    {
        switch (errorStr.Trim())
        {
            case "Error 1":
                return "Số lượng màu không hợp lệ: Mỗi màu phải có đúng 9 ô.";
            case "Error 2":
                return "Lỗi cạnh: Không đủ 12 viên cạnh hợp lệ.";
            case "Error 3":
                return "Lỗi lật cạnh (Flip error): Một cạnh bị lật ngược.";
            case "Error 4":
                return "Lỗi góc: Không đủ 8 viên góc hợp lệ.";
            case "Error 5":
                return "Lỗi xoay góc (Twist error): Một góc bị xoay lệch.";
            case "Error 6":
                return "Lỗi hoán vị (Parity error): Cần tráo đổi 2 góc hoặc 2 cạnh.";
            case "Error 7":
                return "Không tìm thấy lời giải trong giới hạn bước đi cho phép.";
            case "Error 8":
                return "Hết thời gian tìm kiếm lời giải (Timeout).";
            default:
                return $"Lỗi thuật toán giải: {errorStr}";
        }
    }

    /// <summary>
    /// Kiểm tra mô phỏng xem chuỗi nước đi có đưa bản sao RubikState về 6 mặt đồng màu hay không (Thread-safe).
    /// </summary>
    public static bool VerifySolutionOnState(RubikState baseState, List<string> solution)
    {
        if (baseState == null || solution == null)
            return false;

        try
        {
            RubikState testState = baseState.Clone();
            foreach (string move in solution)
            {
                if (!string.IsNullOrEmpty(move))
                    testState.ApplyMove(move);
            }
            return testState.IsSolved();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra mô phỏng xem chuỗi nước đi có đưa Rubik về 6 mặt đồng màu hay không.
    /// </summary>
    public bool VerifySolution(List<string> solution)
    {
        if (stateManager == null || stateManager.CurrentState == null || solution == null)
            return false;

        return VerifySolutionOnState(stateManager.CurrentState, solution);
    }

    /// <summary>
    /// Rút gọn chuỗi nước đi Rubik: gộp cùng mặt, triệt tiêu đối nghịch, gộp qua mặt đối diện giao hoán.
    /// </summary>
    public static List<string> SimplifyMoves(List<string> rawMoves)
    {
        if (rawMoves == null || rawMoves.Count == 0)
            return new List<string>();

        List<string> moves = new List<string>(rawMoves);
        bool changed = true;
        int maxPasses = 50;

        while (changed && maxPasses-- > 0)
        {
            changed = false;
            List<string> next = new List<string>();

            for (int i = 0; i < moves.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(moves[i]))
                    continue;

                char f1 = moves[i][0];

                // 1. Hai nước cùng mặt liền kề (ví dụ: R + R -> R2, R + R' -> 0)
                if (i + 1 < moves.Count && moves[i + 1].Length > 0 && moves[i + 1][0] == f1)
                {
                    int t1 = GetQuarterTurns(moves[i]);
                    int t2 = GetQuarterTurns(moves[i + 1]);
                    int total = (t1 + t2) % 4;

                    if (total != 0)
                    {
                        next.Add(TurnsToMove(f1, total));
                    }
                    i++;
                    changed = true;
                    continue;
                }

                // 2. Hai nước cùng mặt bị ngăn cách bởi 1 nước ở mặt đối diện (giao hoán, ví dụ: R L R' -> L)
                if (i + 2 < moves.Count &&
                    moves[i + 1].Length > 0 && moves[i + 2].Length > 0 &&
                    AreOppositeFaces(f1, moves[i + 1][0]) &&
                    moves[i + 2][0] == f1)
                {
                    int t1 = GetQuarterTurns(moves[i]);
                    int t3 = GetQuarterTurns(moves[i + 2]);
                    int total = (t1 + t3) % 4;

                    if (total != 0)
                    {
                        next.Add(TurnsToMove(f1, total));
                    }
                    next.Add(moves[i + 1]);
                    i += 2;
                    changed = true;
                    continue;
                }

                next.Add(moves[i]);
            }

            moves = next;
        }

        return moves;
    }

    private static int GetQuarterTurns(string move)
    {
        if (string.IsNullOrEmpty(move)) return 0;
        if (move.Length == 1) return 1;
        if (move[1] == '\'') return 3;
        if (move[1] == '2') return 2;
        return 0;
    }

    private static string TurnsToMove(char face, int quarterTurns)
    {
        quarterTurns = ((quarterTurns % 4) + 4) % 4;
        switch (quarterTurns)
        {
            case 1: return face.ToString();
            case 2: return face.ToString() + "2";
            case 3: return face.ToString() + "'";
            default: return string.Empty;
        }
    }

    private static bool AreOppositeFaces(char a, char b)
    {
        if ((a == 'U' && b == 'D') || (a == 'D' && b == 'U')) return true;
        if ((a == 'R' && b == 'L') || (a == 'L' && b == 'R')) return true;
        if ((a == 'F' && b == 'B') || (a == 'B' && b == 'F')) return true;
        return false;
    }

    /// <summary>
    /// Thuật toán BFS tìm chuỗi nước đi ngắn nhất đưa khối về Solved nếu độ sâu <= maxDepth.
    /// </summary>
    private List<string> SolveByBFS(RubikState startState, int maxDepth)
    {
        if (startState.IsSolved()) return new List<string>();

        string[] possibleMoves = {
            "U", "U'", "U2", "D", "D'", "D2",
            "R", "R'", "R2", "L", "L'", "L2",
            "F", "F'", "F2", "B", "B'", "B2"
        };

        Queue<BFSNode> queue = new Queue<BFSNode>();
        queue.Enqueue(new BFSNode(startState.Clone(), new List<string>()));

        int statesExplored = 0;
        int maxStates = 40000;

        while (queue.Count > 0 && statesExplored < maxStates)
        {
            BFSNode current = queue.Dequeue();
            statesExplored++;

            if (current.moves.Count >= maxDepth)
                continue;

            char lastFace = current.moves.Count > 0 ? current.moves[current.moves.Count - 1][0] : ' ';

            foreach (string m in possibleMoves)
            {
                // Không xoay lại cùng mặt liên tiếp trong BFS
                if (m[0] == lastFace) continue;

                RubikState nextState = current.state.Clone();
                nextState.ApplyMove(m);

                List<string> nextMoves = new List<string>(current.moves) { m };

                if (nextState.IsSolved())
                {
                    return nextMoves;
                }

                if (nextMoves.Count < maxDepth)
                {
                    queue.Enqueue(new BFSNode(nextState, nextMoves));
                }
            }
        }

        return null;
    }

    private class BFSNode
    {
        public RubikState state;
        public List<string> moves;

        public BFSNode(RubikState state, List<string> moves)
        {
            this.state = state;
            this.moves = moves;
        }
    }
}
