using System.Globalization;
using System.Text.RegularExpressions;

using _3DPrint.NET.Data.Values;

namespace _3DPrint.NET.Data.Processors;
public static class ResponseParser
{
    public static Cube ReadSizeResponse(SerialMessage? serialMessage)
    {
        if (serialMessage == null)
            return default;

        string[] parts = serialMessage.Full.Split("   ");

        string[] min = parts[0].Split("  ");
        string[] max = parts[1].Split("  ");

        if (min.Length < 2 || max.Length < 2)
            return default;

        min = min[1].Split(' ');
        max = max[1].Split(' ');

        if (min.Length < 3 || max.Length < 3)
            return default;

        (double? minX, double? minY, double? minZ, _) = ParseCoordinates(min);
        (double? maxX, double? maxY, double? maxZ, _) = ParseCoordinates(max);

        if (minX == null || minY == null || minZ == null || maxX == null || maxY == null || maxZ == null)
            return default;

        return new Cube(new Coordinate(minX.Value, minY.Value, minZ.Value), new Coordinate(maxX.Value, maxY.Value, maxZ.Value));
    }

    public static (double? X, double? Y, double? Z, double? E) ParseCoordinates(string[] parts)
    {
        double? x = null;
        double? y = null;
        double? z = null;
        double? e = null;

        foreach (string part in parts)
        {
            if (part.Length < 2)
                continue;
            char axis = part[0];
            if (!double.TryParse(part[1..], CultureInfo.InvariantCulture, out double value))
                continue;

            switch (axis)
            {
                case 'X':
                    x = value;
                    break;

                case 'Y':
                    y = value;
                    break;

                case 'Z':
                    z = value;
                    break;

                case 'E':
                    e = value;
                    break;
            }
        }

        return (x, y, z, e);
    }

    public static double ParseTemperatureRequest(GCodeMessage message)
    {

        string? tempStr = message.Arguments.FirstOrDefault(x => x.StartsWith('S'));
        if (tempStr == null)
            return default;

        return double.TryParse(tempStr[1..], CultureInfo.InvariantCulture, out double temp)
            ? temp
            : default;
    }

    public static Dictionary<char, double> ParseGcode(string[] args)
    {
        var values = new Dictionary<char, double>();

        foreach (string part in args)
        {
            if (part.Length < 2)
                continue;
            char axis = part[0];
            if (!double.TryParse(part[1..], CultureInfo.InvariantCulture, out double value))
                continue;

            values.Add(axis, value);
        }

        return values;
    }

    private static readonly Regex s_meshLineRegex = new(@"^\d ([+-]\d+\.\d+ ?)+$", RegexOptions.Compiled);
    public static double[,] ParseMesh(IEnumerable<string> lines)
    {
        string[] linesWithData = lines.Where(x => s_meshLineRegex.IsMatch(x)).ToArray();
        IEnumerable<string[]> onlyDataString = linesWithData.Select(x => x.Split(' ')[1..]);
        IEnumerable<IEnumerable<double>> data = onlyDataString.Select(x => x.Select(y => double.Parse(y, CultureInfo.InvariantCulture)));

        int height = linesWithData.Length;
        int width = height;

        double[,] matrix = new double[width, height];
        foreach ((IEnumerable<double> row, int y) in data.Select((x, i) => (x, i)))
        {
            foreach ((double value, int x) in row.Select((x, i) => (x, i)))
            {
                matrix[x, y] = value;
            }
        }

        return matrix;
    }
}
