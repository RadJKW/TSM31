namespace TSM31.Core.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestData.Context;
using TestData.Entities;
using TestData.Models;

public class UnitDataService
{
    private readonly TestDataDbContext _dbContext;
    private readonly ILogger<UnitDataService> _logger;
    private UnitData? _currentUnit;

    public UnitData? CurrentUnit => _currentUnit;

    public event Action? OnUnitDataChanged;

    public UnitDataService(TestDataDbContext dbContext, ILogger<UnitDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> DownloadUnitAsync(string serialNumber,
                                                                        string? catalogNumber = null)
    {
        try
        {
            // Create new unit
            _currentUnit = new UnitData {
                SerialNumber = serialNumber,
                CustomerName = "Valued Customer",// Placeholder - would be from web service
                WorkOrder = "00000"
            };

            // Look up in Xref table
            var xref = await _dbContext.Xrefs
                .FirstOrDefaultAsync(x => x.Serno == serialNumber);

            if (xref == null && string.IsNullOrEmpty(catalogNumber))
            {
                return (false, $"Serial Number ({serialNumber}) not found in Cross Reference table.");
            }

            var workOrder = xref?.Workorder ?? "00000";
            var catalog = xref?.Catno ?? catalogNumber ?? "";

            _currentUnit.WorkOrder = workOrder;
            _currentUnit.CatalogNumber = catalog;

            // Check if it's a regulator (catalog starts with "28")
            if (catalog.Length >= 2 && catalog.Substring(0, 2) == "28")
            {
                _currentUnit.UnitType = TransformerType.Regulator;
                workOrder = "00000";
            }

            // Get test parameters from Params table
            // Note: TestNumber is a string in the database, so we parse it as int for proper numerical ordering
            var paramsFromDb = await _dbContext.Params
                .Where(p => p.WorkOrder == workOrder && p.CatalogNumber == catalog)
                .ToListAsync();

            var paramsList = paramsFromDb
                .OrderBy(p => int.Parse(p.TestNumber))
                .ToList();

            if (paramsList.Count == 0)
            {
                return (false, $"No test data found for Serial Number ({serialNumber}) in Params table.");
            }

            // Parse each test parameter row
            foreach (var param in paramsList)
            {
                ParseTransformerParams(param);
            }

            // Initialize CurrentTest to 1 after parsing all tests
            _currentUnit.CurrentTest = 1;

            _currentUnit.IsDownloaded = true;
            OnUnitDataChanged?.Invoke();

            return (true, $"Successfully downloaded {_currentUnit.TotalTests} test(s) for unit {serialNumber}");
        }
        catch (Exception ex)
        {
            return (false, $"Error downloading unit: {ex.Message}");
        }
    }

    private void ParseTransformerParams(Param dr)
    {
        if (_currentUnit == null) return;

        var testNumber = int.Parse(dr.TestNumber);
        _currentUnit.TotalTests = testNumber;

        // First row initializes global transformer metadata
        if (testNumber == 1)
        {
            _currentUnit.HasArrestor = dr.Arrestor == "Y";
            _currentUnit.HasDisconnect = dr.DisconnectPresent == "Y";
            _currentUnit.Kva = float.Parse(dr.Kva);
            _currentUnit.PrimaryBushings = int.Parse(dr.PriBushings);
            _currentUnit.PrimaryMaterial = dr.PriMaterial;
            _currentUnit.PrimaryRatings = int.Parse(dr.PriRatings);
            _currentUnit.SecondaryBushings = int.Parse(dr.SecBushings);
            _currentUnit.SecondaryMaterial = dr.SecMaterial;
            _currentUnit.SecondaryRatings = int.Parse(dr.SecRatings);
            _currentUnit.PrimaryCoilConfiguration = dr.PriCoilCfg;
            _currentUnit.SecondaryCoilConfiguration = dr.SecCoilCfg;
            _currentUnit.PolarityDesign = dr.Polarity;
            _currentUnit.IsSideBySide = dr.SideBySideFlag == "Y";

            // Determine unit type based on UnitType field
            if (dr.UnitType == "2" || dr.UnitType == "6")
            {
                // 3-phase logic would go here
                _currentUnit.UnitType = TransformerType.ThreePhaseYd;
            }
            else if (dr.UnitType == "1")
            {
                if (_currentUnit.CatalogNumber.StartsWith("14") || _currentUnit.CatalogNumber.StartsWith("24"))
                {
                    _currentUnit.UnitType = TransformerType.StepDown;
                }
                else
                {
                    _currentUnit.UnitType = TransformerType.SinglePhase;
                }
            }
        }

        // Add ratings for this test
        var ratings = new Ratings {
            PrimaryVoltage = long.Parse(dr.Pv),
            SecondaryVoltage = long.Parse(dr.Sv),
            PrimaryBIL = int.Parse(dr.PriBil),
            SecondaryBIL = int.Parse(dr.SecBil)
        };
        ratings.PrimaryCurrent = _currentUnit.Kva * 1000 / ratings.PrimaryVoltage;
        ratings.SecondaryCurrent = _currentUnit.Kva * 1000 / ratings.SecondaryVoltage;
        _currentUnit.Ratings.Add(ratings);

        // Add Hipot data
        var hipot = new HipotData {
            PrimaryStatus =
                new TestStatus(dr.PriHipotrequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            PrimaryLimit = int.Parse(dr.Hvhipotlimit),
            SecondaryStatus =
                new TestStatus(dr.SecHipotrequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            SecondaryLimit = int.Parse(dr.Lvhipotlimit)
        };

        // 4LVB setup
        if (dr.FourLvbhipotRequired == "R" && _currentUnit.UnitType != TransformerType.ThreePhaseYd)
        {
            hipot.FourLvbStatus = new TestStatus(TestStatusType.Required);
            hipot.FourLvbSetCondition = int.Parse(dr.FourLvbhipotTestVoltage) / 1000f;
            hipot.FourLvbTimeRequired = int.Parse(dr.FourLvbhipotTestTime);
            hipot.FourLvbLimit = hipot.SecondaryLimit;
        }
        else
        {
            hipot.FourLvbStatus = new TestStatus(TestStatusType.NotRequired);
        }

        _currentUnit.Hipot.Add(hipot);

        // Add Induced data
        var induced = new InducedData {
            FirstStatus =
                new TestStatus(dr.FirstInducedRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            SecondStatus = new TestStatus(dr.SecondInducedRequired == "R"
                ? TestStatusType.Required
                : TestStatusType.NotRequired),
            FirstTimeRequired = 4,// Default from TestStation
            SecondTimeRequired = int.Parse(dr.SecondInducedTestTime),
            WattLimit = int.Parse(dr.InducedWattsLimit),
            SetCondition = int.Parse(dr.InducedVolts)
        };
        _currentUnit.Induced.Add(induced);

        // Add Impulse data
        var impulse = new ImpulseData {
            H1Status = new TestStatus(
            dr.H1ImpulseRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            H2Status = new TestStatus(
            dr.H2ImpulseRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            H3Status = new TestStatus(
            dr.H3ImpulseRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            X1Status = new TestStatus(
            dr.X1ImpulseRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            X2Status = new TestStatus(
            dr.X2ImpulseRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            SetCondition = ratings.PrimaryBIL
        };

        if (_currentUnit.UnitType == TransformerType.StepDown)
        {
            impulse.SecondarySetCondition = ratings.SecondaryBIL;
        }

        _currentUnit.Impulse.Add(impulse);
    }

    public void ClearUnit()
    {
        _currentUnit = null;
        OnUnitDataChanged?.Invoke();
    }

    public async Task<IEnumerable<string>> GetDistinctPrimaryBILs()
    {
        _logger.LogDebug("Querying distinct primary BILs...");
        var result = await _dbContext.Params
            .Select(p => p.PriBil)
            .Distinct()
            .OrderBy(bil => bil)
            .ToListAsync();
        _logger.LogDebug("Retrieved {PrimaryBilCount} primary BIL records", result.Count);
        return result;
    }

    public async Task<IEnumerable<string>> GetDistinctSecondaryBILs()
    {
        _logger.LogDebug("Querying distinct secondary BILs...");
        var result = await _dbContext.Params
            .Select(p => p.SecBil)
            .Distinct()
            .OrderBy(bil => bil)
            .ToListAsync();
        _logger.LogDebug("Retrieved {SecondaryBilCount} secondary BIL records", result.Count);
        return result;
    }
}
