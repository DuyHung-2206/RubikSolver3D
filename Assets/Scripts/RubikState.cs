
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

        if ("UDRLFB".IndexOf(face) < 0)
            throw new System.ArgumentException("Mặt xoay không hợp lệ.");

        int turns = 1;

        if (move.Length == 2 && move[1] == '\'')
            turns = 3;
        else if (move.Length == 2 && move[1] == '2')
            turns = 2;
        else if (move.Length != 1)
            throw new System.ArgumentException("Định dạng nước đi không hợp lệ.");

        for (int i = 0; i < turns; i++)
            RotateFaceClockwise(face);
    }

    private void RotateFaceClockwise(char face)
    {
        // Xoay 9 ô của mặt theo chiều kim đồng hồ.
        char[] oldFace = GetFace(face);
        char[] newFace = new char[9];

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int oldIndex = row * 3 + col;
                int newRow = col;
                int newCol = 2 - row;
                int newIndex = newRow * 3 + newCol;

                newFace[newIndex] = oldFace[oldIndex];
            }
        }

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
                SetColor(face, row, col, newFace[row * 3 + col]);
        }

        // Lưu ý: đây mới chỉ xoay mặt 3x3.
        // Chưa hoán đổi các ô ở bốn mặt kề nhau.
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
