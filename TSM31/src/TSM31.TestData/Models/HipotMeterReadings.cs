namespace TSM31.TestData.Models;

/// <summary>
/// Manages hipot meter readings, applies calibration corrections, and tracks voltage stability.
/// </summary>
public class HipotMeterReadings
{
    private readonly Queue<HipotMeterReading> _readings = new();
    private HipotMeterReading _currentReading = new();
    private HipotCoefficients _coefficients = new();
    private bool _hvVoltageStable;
    private bool _lvVoltageStable;
    private DateTime _timeStamp;

    public string? CoefficientsFile { get; set; }
    public TestTypes TestType { get; set; }
    public UnitTypes UnitType { get; set; }

    public enum TestTypes
    {
        Simultaneous = 0,
        Primary = 1,
        Secondary = 2,
        FourLvb = 3
    }

    public enum UnitTypes
    {
        NonStepDown = 0,
        StepDown = 1
    }

    public void ClearReadings()
    {
        _readings.Clear();
    }

    internal void AddReading(HipotMeterReading currentReading)
    {
        _timeStamp = DateTime.Now.AddMilliseconds(800);
        _currentReading = currentReading;

        if (CorrectHipotMeterReadings())
        {
            _readings.Enqueue(_currentReading);

            // Keep only the last 4 readings
            while (_readings.Count > 4)
            {
                _readings.Dequeue();
            }

            if (_readings.Count == 4)
            {
                var readingArray = _readings.ToArray();

                // Check HV voltage stability
                _hvVoltageStable = CheckVoltageStability(
                    readingArray.Select(r => r.HighVoltageKilovolts).ToArray());

                // Check LV voltage stability
                _lvVoltageStable = CheckVoltageStability(
                    readingArray.Select(r => r.LowVoltageKilovolts).ToArray());
            }

            // Notify variacs of new meter data (would need event handlers in actual implementation)
            // HV.Meter_NewMeterData();
            // LV.Meter_NewMeterData();
        }
        else
        {
            _hvVoltageStable = false;
            _lvVoltageStable = false;
        }
    }

    private bool CheckVoltageStability(float[] voltages)
    {
        if (voltages.Any(v => v <= 0))
            return false;

        var avg = voltages.Average();
        return voltages.All(v => Math.Abs(v - avg) / avg <= 0.01);
    }

    private bool CorrectHipotMeterReadings()
    {
        // Apply calibration correction to HV measurements
        _currentReading.HighVoltageKilovolts = _currentReading.HighVoltageKilovoltsRaw;
        var hvTare = _currentReading.HighVoltageKilovoltsRaw * _currentReading.HighVoltageKilovoltsRaw *
            _coefficients.HvTare.C2 +
            _currentReading.HighVoltageKilovoltsRaw * _coefficients.HvTare.C1 +
            _coefficients.HvTare.C0;

        _currentReading.HighVoltageMilliamps = hvTare > _currentReading.HighVoltageMilliampsRaw
            ? _currentReading.HighVoltageMilliampsRaw + _coefficients.HvTare.Bias
            : _currentReading.HighVoltageMilliampsRaw - hvTare + _coefficients.HvTare.Bias;

        // Apply calibration correction to LV measurements
        _currentReading.LowVoltageKilovolts = _currentReading.LowVoltageKilovoltsRaw;
        var lvTare = _currentReading.LowVoltageKilovoltsRaw * _currentReading.LowVoltageKilovoltsRaw * _coefficients.LvTare.C2 +
            _currentReading.LowVoltageKilovoltsRaw * _coefficients.LvTare.C1 +
            _coefficients.LvTare.C0;

        _currentReading.LowVoltageMilliamps = lvTare > _currentReading.LowVoltageMilliampsRaw
            ? _currentReading.LowVoltageMilliampsRaw + _coefficients.LvTare.Bias
            : _currentReading.LowVoltageMilliampsRaw - lvTare + _coefficients.LvTare.Bias;

        return true;
    }

    public bool HvVoltageStable
    {
        get
        {
            if (_hvVoltageStable && DateTime.Now > _timeStamp)
            {
                _hvVoltageStable = false;
            }

            return _hvVoltageStable;
        }
    }

    public bool LvVoltageStable
    {
        get
        {
            if (_lvVoltageStable && DateTime.Now > _timeStamp)
            {
                _lvVoltageStable = false;
            }

            return _lvVoltageStable;
        }
    }

    public float HighVoltageKilovolts => _currentReading.HighVoltageKilovolts;
    public float HighVoltageKilovoltsRaw => _currentReading.HighVoltageKilovoltsRaw;
    public float LowVoltageKilovolts => _currentReading.LowVoltageKilovolts;
    public float LowVoltageKilovoltsRaw => _currentReading.LowVoltageKilovoltsRaw;
    public float HighVoltageMilliamps => _currentReading.HighVoltageMilliamps;
    public float HighVoltageMilliampsRaw => _currentReading.HighVoltageMilliampsRaw;
    public float LowVoltageMilliamps => _currentReading.LowVoltageMilliamps;
    public float LowVoltageMilliampsRaw => _currentReading.LowVoltageMilliampsRaw;
}
