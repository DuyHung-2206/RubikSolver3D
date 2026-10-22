using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubikScrambler : MonoBehaviour
{
    [Header("Cài đặt trộn chuẩn WCA")]
    [Range(20, 25)]
    [Tooltip("Số nước đi trong một chuỗi trộn (chuẩn WCA là 20-25 nước)")]
    public int scrambleMoveCount = 20;

    [Tooltip("Tốc độ xoay siêu nhanh khi trộn để tiết kiệm thời gian (giây/lần xoay)")]
    public float fastScrambleDuration = 0.04f;

    [Header("Dữ liệu hiển thị")]
    [TextArea(2, 4)]
    [Tooltip("Chuỗi trộn ngẫu nhiên gần nhất")]
    public string lastScrambleString = "";

    private RubikRotator rotator;

    // 6 mặt được xếp theo cặp trục đối xứng:
    // Index 0, 1: Trục Y (U, D)
    // Index 2, 3: Trục X (R, L)
    // Index 4, 5: Trục Z (F, B)
    private static readonly char[] Faces = { 'U', 'D', 'R', 'L', 'F', 'B' };
    private static readonly string[] Modifiers = { "", "'", "2" };

    private void Awake()
    {
        rotator = GetComponent<RubikRotator>();
    }

    /// <summary>
    /// Sinh chuỗi trộn ngẫu nhiên đạt chuẩn quy tắc WCA.
    /// </summary>
    public string GenerateWcaScramble(int count = 20)
    {
        List<string> moves = new List<string>();

        int prevFaceIndex = -1;
        int prevPrevFaceIndex = -1;

        for (int i = 0; i < count; i++)
        {
            int faceIndex;

            while (true)
            {
                faceIndex = Random.Range(0, 6);

                // 1. Không trùng mặt với nước liền trước (tránh R R hoặc R R')
                if (faceIndex == prevFaceIndex)
                    continue;

                // 2. Không lặp 3 nước trên cùng một trục đối xứng (tránh R L R hoặc U D U)
                int currentAxis = faceIndex / 2;
                int prevAxis = (prevFaceIndex >= 0) ? prevFaceIndex / 2 : -1;
                int prevPrevAxis = (prevPrevFaceIndex >= 0) ? prevPrevFaceIndex / 2 : -1;

                if (currentAxis == prevAxis && currentAxis == prevPrevAxis)
                    continue;

                break;
            }

            prevPrevFaceIndex = prevFaceIndex;
            prevFaceIndex = faceIndex;

            string modifier = Modifiers[Random.Range(0, Modifiers.Length)];
            moves.Add(Faces[faceIndex] + modifier);
        }

        lastScrambleString = string.Join(" ", moves);
        return lastScrambleString;
    }

    /// <summary>
    /// Kích hoạt chuỗi trộn tốc độ cao lên khối Rubik 3D.
    /// </summary>
    [ContextMenu("Trộn Rubik (Scramble Now)")]
    public void PerformScramble()
    {
        if (rotator == null)
            rotator = GetComponent<RubikRotator>();

        if (rotator == null)
        {
            Debug.LogError("[RubikScrambler] Không tìm thấy RubikRotator trên RubikManager!");
            return;
        }

        if (rotator.IsBusy)
        {
            Debug.LogWarning("[RubikScrambler] Khối Rubik đang bận xoay, vui lòng chờ!");
            return;
        }

        string scramble = GenerateWcaScramble(scrambleMoveCount);
        Debug.Log($"<color=yellow>[WCA Scramble]</color> Chuỗi nước đi: <b>{scramble}</b>");

        StartCoroutine(ExecuteScrambleCoroutine(scramble));
    }

    private IEnumerator ExecuteScrambleCoroutine(string scrambleString)
    {
        float originalDuration = rotator.rotationDuration;
        // Tăng tốc độ xoay siêu nhanh trong lúc trộn
        rotator.rotationDuration = fastScrambleDuration;

        string[] moves = scrambleString.Split(' ');

        foreach (string move in moves)
        {
            if (string.IsNullOrEmpty(move)) continue;

            // Xử lý nước đi 180 độ: "R2" -> thực hiện 2 lần "R"
            if (move.EndsWith("2"))
            {
                string singleMove = move.Substring(0, 1);
                rotator.EnqueueMove(singleMove);
                rotator.EnqueueMove(singleMove);
            }
            else
            {
                rotator.EnqueueMove(move);
            }
        }

        // Chờ cho đến khi tất cả các nước đi trong hàng đợi hoàn tất
        while (rotator.IsBusy)
        {
            yield return null;
        }

        // Khôi phục lại tốc độ xoay ban đầu của người chơi
        rotator.rotationDuration = originalDuration;
        Debug.Log("<color=green>[RubikScrambler] Đã hoàn tất trộn Rubik thành công!</color>");
    }
}