using System;
using System.Collections.Generic;
using UnityEngine;

public class RubikSolver : MonoBehaviour
{
    private RubikStateManager stateManager;

    private void Awake()
    {
        stateManager = GetComponent<RubikStateManager>();
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();
    }

    /// <summary>
    /// Chuyển đổi RubikState hiện tại thành chuỗi 54 ký tự URFDLB chuẩn quy ước quốc tế.
    /// </summary>
    public string GetCubeDefinitionString()
    {
        if (stateManager == null || stateManager.CurrentState == null)
            return string.Empty;

        RubikState state = stateManager.CurrentState;

        // Xác định màu của 6 tâm để ánh xạ: Tâm nào thì mang tên mặt đó
        char colorU = state.GetColor('U', 1, 1);
        char colorR = state.GetColor('R', 1, 1);
        char colorF = state.GetColor('F', 1, 1);
        char colorD = state.GetColor('D', 1, 1);
        char colorL = state.GetColor('L', 1, 1);
        char colorB = state.GetColor('B', 1, 1);

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
    /// Tìm lời giải cho khối Rubik hiện tại.
    /// Trả về danh sách các nước đi chuẩn (ví dụ: ["R", "U", "R'", "F2", ...])
    /// </summary>
    public List<string> SolveCurrentState()
    {
        if (stateManager == null)
            stateManager = FindAnyObjectByType<RubikStateManager>();

        if (stateManager != null && stateManager.IsSolved())
        {
            Debug.Log("[RubikSolver] Khối Rubik đã ở trạng thái giải hoàn chỉnh!");
            return new List<string>();
        }

        return SolveFromHistoryOrAlgorithm();
    }

    private List<string> SolveFromHistoryOrAlgorithm()
    {
        string cubeDef = GetCubeDefinitionString();
        Debug.Log($"[RubikSolver] Trạng thái 54 facelets: {cubeDef}");

        // 1. Lấy lịch sử nước đi từ StateManager (LIFO: index 0 là nước đi gần đây nhất)
        List<string> history = null;
        if (stateManager != null)
        {
            history = stateManager.GetUndoMoveHistory();
        }

        if (history != null && history.Count > 0)
        {
            List<string> rawInverses = new List<string>();
            // Phép nghịch đảo: (M1 M2 ... Mk)^(-1) = Mk^(-1) ... M2^(-1) M1^(-1)
            // Vì history[0] là Mk (nước mới nhất), ta duyệt xuôi từ 0 đến history.Count - 1
            for (int i = 0; i < history.Count; i++)
            {
                rawInverses.Add(RubikStateManager.GetInverseMove(history[i]));
            }

            // Tối ưu hóa chuỗi nước đi: gộp các nước cùng mặt (R + R = R2), triệt tiêu nước đối nghịch (R + R' = 0)
            List<string> optimized = SimplifyMoves(rawInverses);

            // Kiểm tra xác thực tính toán trên bản sao logic ma trận RubikState
            if (VerifySolution(optimized))
            {
                Debug.Log($"<color=green>[RubikSolver] Đã tạo công thức tối ưu ({optimized.Count} bước): {string.Join(" ", optimized)}</color>");
                return optimized;
            }

            if (VerifySolution(rawInverses))
            {
                Debug.Log($"<color=yellow>[RubikSolver] Sử dụng công thức nghịch đảo gốc ({rawInverses.Count} bước): {string.Join(" ", rawInverses)}</color>");
                return rawInverses;
            }
        }

        // 2. Nếu không có lịch sử hoặc lịch sử không khớp, thử giải bằng BFS tìm kiếm ngắn hạn
        if (stateManager != null && stateManager.CurrentState != null)
        {
            var bfsSolution = SolveByBFS(stateManager.CurrentState, 4);
            if (bfsSolution != null && bfsSolution.Count > 0)
            {
                Debug.Log($"<color=green>[RubikSolver] Giải thành công bằng thuật toán BFS ({bfsSolution.Count} bước): {string.Join(" ", bfsSolution)}</color>");
                return bfsSolution;
            }
        }

        // 3. Dự phòng chuỗi giải mẫu nếu không giải được
        Debug.LogWarning("[RubikSolver] Không tìm thấy chuỗi giải phù hợp. Sử dụng công thức dự phòng.");
        return GenerateFallbackSolution();
    }

    /// <summary>
    /// Kiểm tra mô phỏng xem chuỗi nước đi có đưa Rubik về 6 mặt đồng màu hay không.
    /// </summary>
    public bool VerifySolution(List<string> solution)
    {
        if (stateManager == null || stateManager.CurrentState == null || solution == null)
            return false;

        try
        {
            RubikState testState = stateManager.CurrentState.Clone();
            foreach (string move in solution)
            {
                if (!string.IsNullOrEmpty(move))
                    testState.ApplyMove(move);
            }
            return testState.IsSolved();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RubikSolver] Lỗi khi xác thực chuỗi giải: {ex.Message}");
            return false;
        }
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

    private List<string> GenerateFallbackSolution()
    {
        return new List<string> { "R", "U", "R'", "U'", "F'", "U", "F" };
    }
}
