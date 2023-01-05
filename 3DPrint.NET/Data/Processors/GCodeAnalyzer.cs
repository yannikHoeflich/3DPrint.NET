using System.Globalization;

namespace _3DPrint.NET.Data.Processors;
public class GCodeAnalyzer {
    private readonly string[] _gCode;
    private readonly int _startIndex;


    private double _hotEndTemp = 0;
    private double _bedTemp = 0;
    private double _x = 0;
    private double _y = 0;
    private double _z = 0;
    private double _speed = 30;

    public GCodeAnalyzer(string[] gCode, int fromIndex) {
        _gCode = gCode;
        _startIndex = fromIndex;
    }

    public TimeSpan Analyze() {
        TimeSpan totalTime = TimeSpan.Zero;

        foreach(string line in _gCode.Skip(_startIndex)) {
            var gCode = new GCodeMessage(line);

            if(gCode.GCode == GCode.LinearMove || gCode.GCode == GCode.LinearMoveNotPrinting) {
                totalTime += AnalyzeMovement(gCode);
            }

            if (gCode.GCode == GCode.WaitForBedTemperature) {
                totalTime += AnalyzeHeating(gCode, 1.8, ref _bedTemp);
            }

            if (gCode.GCode == GCode.WaitForHotendTemperature) {
                totalTime += AnalyzeHeating(gCode, 0.7, ref _hotEndTemp);
            }
        }

        return totalTime;
    }

    private TimeSpan AnalyzeMovement( GCodeMessage gCode) {
        string? newSpeedStr = gCode.Arguments.FirstOrDefault(x => x.StartsWith('F'));
        if (newSpeedStr != null && double.TryParse(newSpeedStr, CultureInfo.InvariantCulture, out double newSpeed))
            _speed = newSpeed / 60;

        (double? newX, double? newY, double? newZ, _) = ResponseParser.ParseCoordinates(gCode.Arguments);
        double sum = 0;
        if (newX != null) {
            double diff = _x - newX.Value;
            sum += diff * diff;
            _x = newX.Value;
        }

        if (newY != null) {
            double diff = _y - newY.Value;
            sum += diff * diff;
            _y = newY.Value;
        }

        if (newZ != null) {
            double diff = _z - newZ.Value;
            sum += diff * diff;
            _z = newZ.Value;
        }

        double wayLength = Math.Sqrt(sum);

        double seconds = wayLength / _speed;
        return TimeSpan.FromSeconds(seconds);
    }

    private TimeSpan AnalyzeHeating(GCodeMessage gCode, double secondsPerDegree, ref double currentDegree) {
        Dictionary<char, double> values = ResponseParser.ParseGcode(gCode.Arguments);
        if (!values.TryGetValue('S', out double newTemp))
            return TimeSpan.Zero;

        double tempDiff = newTemp - currentDegree;
        currentDegree = newTemp;
        if (tempDiff < 0)
            return TimeSpan.Zero;

        double seconds = tempDiff * secondsPerDegree;
        return TimeSpan.FromSeconds(seconds);
    }
}
