using System;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu logic độc lập với GameObject và Transform của Unity.
/// Quy ước màu: W = trắng, Y = vàng, G = xanh lá,
/// B = xanh dương, R = đỏ, O = cam.
/// </summary>
[Serializable]
public class RubikState
{
    public const int FaceCount = 6;
    public const int FaceSize = 9;

    // Thứ tự mặt: U, D, F, B, R, L.
    private readonly char[][] faces = new char[FaceCount][];

    private static readonly char[] SolvedColors =
        { 'W', 'Y', 'G', 'B', 'R', 'O' };

    public RubikState()
    {
        ResetSolved();
    }

    public void ResetSolved()
    {
        for (int f = 0; f < FaceCount; f++)
        {
            faces[f] = new char[FaceSize];

            for (int i = 0; i < FaceSize; i++)
                faces[f][i] = SolvedColors[f];
        }
    }

    public char GetColor(char face, int row, int col)
    {
        int faceIndex = FaceIndex(face);
        ValidateCell(row, col);
        return faces[faceIndex][row * 3 + col];
    }

    public void SetColor(char face, int row, int col, char color)
    {
        int faceIndex = FaceIndex(face);
        ValidateCell(row, col);

        if ("WYGBRO".IndexOf(color) < 0)
            throw new ArgumentException("Mã màu không hợp lệ.");

        faces[faceIndex][row * 3 + col] = color;
    }

    public char[] GetFace(char face)
    {
        return (char[])faces[FaceIndex(face)].Clone();
    }

    public bool IsSolved()
    {
        for (int f = 0; f < FaceCount; f++)
        {
            for (int i = 0; i < FaceSize; i++)
            {
                if (faces[f][i] != SolvedColors[f])
                    return false;
            }
        }

        return true;
    }

    public bool HasValidColorCounts()
    {
        Dictionary<char, int> counts = new Dictionary<char, int>();

        foreach (char color in SolvedColors)
            counts[color] = 0;

        for (int f = 0; f < FaceCount; f++)
        {
            for (int i = 0; i < FaceSize; i++)
            {
                char color = faces[f][i];

                if (!counts.ContainsKey(color))
                    return false;

                counts[color]++;
            }
        }

        foreach (char color in SolvedColors)
        {
            if (counts[color] != 9)
                return false;
        }

        return true;
    }

    public string ToFaceletString()
    {
        // Thứ tự xuất dữ liệu: U, R, F, D, L, B.
        char[] order = { 'U', 'R', 'F', 'D', 'L', 'B' };
        System.Text.StringBuilder result =
            new System.Text.StringBuilder(54);

        foreach (char face in order)
        {
            foreach (char color in GetFace(face))
                result.Append(color);
        }

        return result.ToString();
    }

    public void ApplyMove(string move)
    {
        if (string.IsNullOrWhiteSpace(move))
            throw new System.ArgumentException("Nước đi không hợp lệ.");

        move = move.Trim().ToUpperInvariant();

        char face = move[0];

        if ("UDRLFB MES".IndexOf(face) < 0)
            throw new System.ArgumentException($"Mặt xoay '{face}' không hợp lệ.");

        int turns = 1;

        if (move.Length == 2 && move[1] == '\'')
            turns = 3;
        else if (move.Length == 2 && move[1] == '2')
            turns = 2;
        else if (move.Length != 1)
            throw new System.ArgumentException("Định dạng nước đi không hợp lệ.");

        for (int i = 0; i < turns; i++)
        {
            if (face == 'M')
                RotateMiddleSliceM();
            else if (face == 'E')
                RotateMiddleSliceE();
            else if (face == 'S')
                RotateMiddleSliceS();
            else
                RotateFaceClockwise(face);
        }
    }

    private void RotateFaceClockwise(char face)
    {
        // 1. Xoay 9 ô của chính mặt đó theo chiều kim đồng hồ
        char[] oldFace = GetFace(face);
        char[] newFace = new char[9];

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int oldIndex = row * 3 + col;
                int newRow = col;
                int newCol = 2 - row;
                newFace[newRow * 3 + newCol] = oldFace[oldIndex];
            }
        }

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
                SetColor(face, row, col, newFace[row * 3 + col]);
        }

        // 2. Hoán vị 4 dải biên của 4 mặt kề nhau theo quy chuẩn hình học chuẩn WCA
        RotateAdjacentStrips(face);
    }

    private void RotateAdjacentStrips(char face)
    {
        char[] temp = new char[3];

        switch (face)
        {
            case 'U': // Mặt Trên: luân chuyển hàng 0 của B -> R -> F -> L -> B
                temp[0] = GetColor('B', 0, 0); temp[1] = GetColor('B', 0, 1); temp[2] = GetColor('B', 0, 2);

                SetColor('B', 0, 0, GetColor('L', 0, 0));
                SetColor('B', 0, 1, GetColor('L', 0, 1));
                SetColor('B', 0, 2, GetColor('L', 0, 2));

                SetColor('L', 0, 0, GetColor('F', 0, 0));
                SetColor('L', 0, 1, GetColor('F', 0, 1));
                SetColor('L', 0, 2, GetColor('F', 0, 2));

                SetColor('F', 0, 0, GetColor('R', 0, 0));
                SetColor('F', 0, 1, GetColor('R', 0, 1));
                SetColor('F', 0, 2, GetColor('R', 0, 2));

                SetColor('R', 0, 0, temp[0]);
                SetColor('R', 0, 1, temp[1]);
                SetColor('R', 0, 2, temp[2]);
                break;

            case 'D': // Mặt Dưới: luân chuyển hàng 2 của F -> R -> B -> L -> F
                temp[0] = GetColor('F', 2, 0); temp[1] = GetColor('F', 2, 1); temp[2] = GetColor('F', 2, 2);

                SetColor('F', 2, 0, GetColor('L', 2, 0));
                SetColor('F', 2, 1, GetColor('L', 2, 1));
                SetColor('F', 2, 2, GetColor('L', 2, 2));

                SetColor('L', 2, 0, GetColor('B', 2, 0));
                SetColor('L', 2, 1, GetColor('B', 2, 1));
                SetColor('L', 2, 2, GetColor('B', 2, 2));

                SetColor('B', 2, 0, GetColor('R', 2, 0));
                SetColor('B', 2, 1, GetColor('R', 2, 1));
                SetColor('B', 2, 2, GetColor('R', 2, 2));

                SetColor('R', 2, 0, temp[0]);
                SetColor('R', 2, 1, temp[1]);
                SetColor('R', 2, 2, temp[2]);
                break;

            case 'R': // Mặt Phải: luân chuyển cột 2 của U, F, D và cột 0 của B (đảo chiều)
                temp[0] = GetColor('U', 0, 2); temp[1] = GetColor('U', 1, 2); temp[2] = GetColor('U', 2, 2);

                SetColor('U', 0, 2, GetColor('F', 0, 2));
                SetColor('U', 1, 2, GetColor('F', 1, 2));
                SetColor('U', 2, 2, GetColor('F', 2, 2));

                SetColor('F', 0, 2, GetColor('D', 0, 2));
                SetColor('F', 1, 2, GetColor('D', 1, 2));
                SetColor('F', 2, 2, GetColor('D', 2, 2));

                SetColor('D', 0, 2, GetColor('B', 2, 0));
                SetColor('D', 1, 2, GetColor('B', 1, 0));
                SetColor('D', 2, 2, GetColor('B', 0, 0));

                SetColor('B', 2, 0, temp[0]);
                SetColor('B', 1, 0, temp[1]);
                SetColor('B', 0, 0, temp[2]);
                break;

            case 'L': // Mặt Trái: luân chuyển cột 0 của U, B (đảo), D, F
                temp[0] = GetColor('U', 0, 0); temp[1] = GetColor('U', 1, 0); temp[2] = GetColor('U', 2, 0);

                SetColor('U', 0, 0, GetColor('B', 2, 2));
                SetColor('U', 1, 0, GetColor('B', 1, 2));
                SetColor('U', 2, 0, GetColor('B', 0, 2));

                SetColor('B', 2, 2, GetColor('D', 0, 0));
                SetColor('B', 1, 2, GetColor('D', 1, 0));
                SetColor('B', 0, 2, GetColor('D', 2, 0));

                SetColor('D', 0, 0, GetColor('F', 0, 0));
                SetColor('D', 1, 0, GetColor('F', 1, 0));
                SetColor('D', 2, 0, GetColor('F', 2, 0));

                SetColor('F', 0, 0, temp[0]);
                SetColor('F', 1, 0, temp[1]);
                SetColor('F', 2, 0, temp[2]);
                break;

            case 'F': // Mặt Trước: luân chuyển hàng 2 của U, cột 0 của R, hàng 0 của D, cột 2 của L
                temp[0] = GetColor('U', 2, 0); temp[1] = GetColor('U', 2, 1); temp[2] = GetColor('U', 2, 2);

                SetColor('U', 2, 0, GetColor('L', 2, 2));
                SetColor('U', 2, 1, GetColor('L', 1, 2));
                SetColor('U', 2, 2, GetColor('L', 0, 2));

                SetColor('L', 0, 2, GetColor('D', 0, 0));
                SetColor('L', 1, 2, GetColor('D', 0, 1));
                SetColor('L', 2, 2, GetColor('D', 0, 2));

                SetColor('D', 0, 0, GetColor('R', 2, 0));
                SetColor('D', 0, 1, GetColor('R', 1, 0));
                SetColor('D', 0, 2, GetColor('R', 0, 0));

                SetColor('R', 0, 0, temp[0]);
                SetColor('R', 1, 0, temp[1]);
                SetColor('R', 2, 0, temp[2]);
                break;

            case 'B': // Mặt Sau: luân chuyển hàng 0 của U, cột 2 của R, hàng 2 của D, cột 0 của L
                temp[0] = GetColor('U', 0, 0); temp[1] = GetColor('U', 0, 1); temp[2] = GetColor('U', 0, 2);

                SetColor('U', 0, 0, GetColor('R', 0, 2));
                SetColor('U', 0, 1, GetColor('R', 1, 2));
                SetColor('U', 0, 2, GetColor('R', 2, 2));

                SetColor('R', 0, 2, GetColor('D', 2, 2));
                SetColor('R', 1, 2, GetColor('D', 2, 1));
                SetColor('R', 2, 2, GetColor('D', 2, 0));

                SetColor('D', 2, 0, GetColor('L', 0, 0));
                SetColor('D', 2, 1, GetColor('L', 1, 0));
                SetColor('D', 2, 2, GetColor('L', 2, 0));

                SetColor('L', 0, 0, temp[2]);
                SetColor('L', 1, 0, temp[1]);
                SetColor('L', 2, 0, temp[0]);
                break;
        }
    }

    /// <summary>
    /// Tầng giữa M (Middle): Nằm giữa L và R, quay theo chiều của L.
    /// Luân chuyển cột 1 của U, B (đảo), D, F.
    /// </summary>
    private void RotateMiddleSliceM()
    {
        char[] temp = new char[3];
        temp[0] = GetColor('U', 0, 1);
        temp[1] = GetColor('U', 1, 1);
        temp[2] = GetColor('U', 2, 1);

        SetColor('U', 0, 1, GetColor('B', 2, 1));
        SetColor('U', 1, 1, GetColor('B', 1, 1));
        SetColor('U', 2, 1, GetColor('B', 0, 1));

        SetColor('B', 2, 1, GetColor('D', 0, 1));
        SetColor('B', 1, 1, GetColor('D', 1, 1));
        SetColor('B', 0, 1, GetColor('D', 2, 1));

        SetColor('D', 0, 1, GetColor('F', 0, 1));
        SetColor('D', 1, 1, GetColor('F', 1, 1));
        SetColor('D', 2, 1, GetColor('F', 2, 1));

        SetColor('F', 0, 1, temp[0]);
        SetColor('F', 1, 1, temp[1]);
        SetColor('F', 2, 1, temp[2]);
    }

    /// <summary>
    /// Tầng giữa E (Equator): Nằm giữa U và D, quay theo chiều của D.
    /// Luân chuyển hàng 1 của F -> R -> B -> L -> F.
    /// </summary>
    private void RotateMiddleSliceE()
    {
        char[] temp = new char[3];
        temp[0] = GetColor('F', 1, 0);
        temp[1] = GetColor('F', 1, 1);
        temp[2] = GetColor('F', 1, 2);

        SetColor('F', 1, 0, GetColor('L', 1, 0));
        SetColor('F', 1, 1, GetColor('L', 1, 1));
        SetColor('F', 1, 2, GetColor('L', 1, 2));

        SetColor('L', 1, 0, GetColor('B', 1, 0));
        SetColor('L', 1, 1, GetColor('B', 1, 1));
        SetColor('L', 1, 2, GetColor('B', 1, 2));

        SetColor('B', 1, 0, GetColor('R', 1, 0));
        SetColor('B', 1, 1, GetColor('R', 1, 1));
        SetColor('B', 1, 2, GetColor('R', 1, 2));

        SetColor('R', 1, 0, temp[0]);
        SetColor('R', 1, 1, temp[1]);
        SetColor('R', 1, 2, temp[2]);
    }

    /// <summary>
    /// Tầng giữa S (Standing): Nằm giữa F và B, quay theo chiều của F.
    /// Luân chuyển hàng 1 của U, cột 1 của R, hàng 1 của D, cột 1 của L.
    /// </summary>
    private void RotateMiddleSliceS()
    {
        char[] temp = new char[3];
        temp[0] = GetColor('U', 1, 0);
        temp[1] = GetColor('U', 1, 1);
        temp[2] = GetColor('U', 1, 2);

        SetColor('U', 1, 0, GetColor('L', 2, 1));
        SetColor('U', 1, 1, GetColor('L', 1, 1));
        SetColor('U', 1, 2, GetColor('L', 0, 1));

        SetColor('L', 0, 1, GetColor('D', 1, 0));
        SetColor('L', 1, 1, GetColor('D', 1, 1));
        SetColor('L', 2, 1, GetColor('D', 1, 2));

        SetColor('D', 1, 0, GetColor('R', 2, 1));
        SetColor('D', 1, 1, GetColor('R', 1, 1));
        SetColor('D', 1, 2, GetColor('R', 0, 1));

        SetColor('R', 0, 1, temp[0]);
        SetColor('R', 1, 1, temp[1]);
        SetColor('R', 2, 1, temp[2]);
    }

    public RubikState Clone()
    {
        RubikState copy = new RubikState();

        for (int f = 0; f < FaceCount; f++)
            Array.Copy(faces[f], copy.faces[f], FaceSize);

        return copy;
    }

    public bool HasSameState(RubikState other)
    {
        if (other == null)
            return false;

        for (int f = 0; f < FaceCount; f++)
        {
            for (int i = 0; i < FaceSize; i++)
            {
                if (faces[f][i] != other.faces[f][i])
                    return false;
            }
        }

        return true;
    }

    private static int FaceIndex(char face)
    {
        switch (face)
        {
            case 'U': return 0;
            case 'D': return 1;
            case 'F': return 2;
            case 'B': return 3;
            case 'R': return 4;
            case 'L': return 5;
            default:
                throw new ArgumentException(
                    "Mặt phải là U, D, F, B, R hoặc L."
                );
        }
    }

    private static void ValidateCell(int row, int col)
    {
        if (row < 0 || row > 2 || col < 0 || col > 2)
            throw new ArgumentOutOfRangeException(
                "row/col phải nằm trong khoảng 0 đến 2."
            );
        }
}
