using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Database.Entities;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.Database;

/// <summary>
/// Repository for downloading and parsing unit test data from SQL Server TestData database.
/// Implements the ITestDataRepository interface for the dielectric test station.
/// </summary>
public class TestDataRepository : ITestDataRepository<UnitData>
{
    private readonly TestDataDbContext _dbContext;
    private readonly ILogger<TestDataRepository> _logger;

    public TestDataRepository(TestDataDbContext dbContext, ILogger<TestDataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Downloads unit data by serial number, looking up catalog and work order in Xref table,
    /// then loading test parameters from Params table.
    /// </summary>
    /// <param name="identifier">Serial number (10 digits)</param>
    /// <returns>Complete UnitData with all test parameters, or null if not found</returns>
    public async Task<UnitData?> DownloadUnitAsync(string identifier)
    {
        try
        {
            var serialNumber = identifier;

            // Create new unit
            var unit = new UnitData
            {
                SerialNumber = serialNumber,
                CustomerName = "Valued Customer", // Placeholder - would be from web service
                WorkOrder = "00000",
                DownloadedAt = DateTime.Now
            };

            // Look up in Xref table
            var xref = await _dbContext.Xrefs
                .FirstOrDefaultAsync(x => x.Serno == serialNumber);

            if (xref == null)
            {
                _logger.LogWarning("Serial Number {SerialNumber} not found in Cross Reference table",
                    serialNumber);
                return null;
            }

            var workOrder = xref.Workorder ?? "00000";
            var catalog = xref.Catno ?? "";

            unit.WorkOrder = workOrder;
            unit.CatalogNumber = catalog;

            // Check if it's a regulator (catalog starts with "28")
            if (catalog.Length >= 2 && catalog.Substring(0, 2) == "28")
            {
                unit.TransformerUnitType = TransformerType.Regulator;
                workOrder = "00000"; // Regulators use default work order
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
                _logger.LogWarning(
                    "No test data found for Serial Number {SerialNumber} (WorkOrder: {WorkOrder}, Catalog: {Catalog})",
                    serialNumber, workOrder, catalog);
                return null;
            }

            // Parse each test parameter row
            foreach (var param in paramsList)
            {
                ParseTransformerParams(unit, param);
            }

            // Initialize CurrentTest to 1 after parsing all tests
            unit.CurrentTest = 1;
            unit.IsDownloaded = true;

            _logger.LogInformation("Successfully downloaded {TestCount} test(s) for unit {SerialNumber}",
                unit.TotalTests, serialNumber);

            return unit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading unit {Identifier}", identifier);
            return null;
        }
    }

    /// <summary>
    /// Validates that a unit exists in the Xref table.
    /// </summary>
    /// <param name="identifier">Serial number to validate</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ValidateUnitExistsAsync(string identifier)
    {
        try
        {
            var exists = await _dbContext.Xrefs.AnyAsync(x => x.Serno == identifier);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unit existence for {Identifier}", identifier);
            return false;
        }
    }

    /// <summary>
    /// Parses a single test parameter row from the Params table into the unit's test collections.
    /// Each row represents one test number with its specific voltage ratings and test requirements.
    /// </summary>
    private void ParseTransformerParams(UnitData unit, Param dr)
    {
        var testNumber = int.Parse(dr.TestNumber);
        unit.TotalTests = testNumber;

        // First row initializes global transformer metadata
        if (testNumber == 1)
        {
            unit.HasArrestor = dr.Arrestor == "Y";
            unit.HasDisconnect = dr.DisconnectPresent == "Y";
            unit.Kva = float.Parse(dr.Kva);
            unit.PrimaryBushings = int.Parse(dr.PriBushings);
            unit.PrimaryMaterial = dr.PriMaterial;
            unit.PrimaryRatings = int.Parse(dr.PriRatings);
            unit.SecondaryBushings = int.Parse(dr.SecBushings);
            unit.SecondaryMaterial = dr.SecMaterial;
            unit.SecondaryRatings = int.Parse(dr.SecRatings);
            unit.PrimaryCoilConfiguration = dr.PriCoilCfg;
            unit.SecondaryCoilConfiguration = dr.SecCoilCfg;
            unit.PolarityDesign = dr.Polarity;
            unit.IsSideBySide = dr.SideBySideFlag == "Y";

            // Determine unit type based on UnitType field
            if (dr.UnitType == "2" || dr.UnitType == "6")
            {
                // 3-phase logic
                unit.TransformerUnitType = TransformerType.ThreePhaseYd;
            }
            else if (dr.UnitType == "1")
            {
                if (unit.CatalogNumber.StartsWith("14") || unit.CatalogNumber.StartsWith("24"))
                {
                    unit.TransformerUnitType = TransformerType.StepDown;
                }
                else
                {
                    unit.TransformerUnitType = TransformerType.SinglePhase;
                }
            }
        }

        // Add ratings for this test
        var ratings = new DielectricRatings
        {
            PrimaryVoltage = long.Parse(dr.Pv),
            SecondaryVoltage = long.Parse(dr.Sv),
            PrimaryBIL = int.Parse(dr.PriBil),
            SecondaryBIL = int.Parse(dr.SecBil)
        };
        ratings.PrimaryCurrent = unit.Kva * 1000 / ratings.PrimaryVoltage;
        ratings.SecondaryCurrent = unit.Kva * 1000 / ratings.SecondaryVoltage;
        unit.DielectricRatings.Add(ratings);

        // Add Hipot data
        var hipot = new HipotData
        {
            PrimaryStatus =
                new TestStatus(dr.PriHipotrequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            PrimaryLimit = int.Parse(dr.Hvhipotlimit),
            SecondaryStatus =
                new TestStatus(dr.SecHipotrequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            SecondaryLimit = int.Parse(dr.Lvhipotlimit)
        };

        // 4LVB setup (not required for 3-phase Yd units)
        if (dr.FourLvbhipotRequired == "R" && unit.TransformerUnitType != TransformerType.ThreePhaseYd)
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

        unit.Hipot.Add(hipot);

        // Add Induced data
        var induced = new InducedData
        {
            FirstStatus =
                new TestStatus(dr.FirstInducedRequired == "R" ? TestStatusType.Required : TestStatusType.NotRequired),
            SecondStatus = new TestStatus(dr.SecondInducedRequired == "R"
                ? TestStatusType.Required
                : TestStatusType.NotRequired),
            FirstTimeRequired = 4, // Default from TestStation
            SecondTimeRequired = int.Parse(dr.SecondInducedTestTime),
            WattLimit = int.Parse(dr.InducedWattsLimit),
            SetCondition = int.Parse(dr.InducedVolts)
        };
        unit.Induced.Add(induced);

        // Add Impulse data
        var impulse = new ImpulseData
        {
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

        // Step-down units have secondary impulse requirements
        if (unit.TransformerUnitType == TransformerType.StepDown)
        {
            impulse.SecondarySetCondition = ratings.SecondaryBIL;
        }

        unit.Impulse.Add(impulse);
    }

    /// <summary>
    /// Gets distinct primary BIL values from the database for UI dropdowns.
    /// </summary>
    public async Task<IEnumerable<string>> GetDistinctPrimaryBILsAsync()
    {
        _logger.LogDebug("Querying distinct primary BILs...");
        var result = await _dbContext.Params
            .Select(p => p.PriBil)
            .Distinct()
            .OrderBy(bil => bil)
            .ToListAsync();
        _logger.LogDebug("Retrieved {Count} primary BIL records", result.Count);
        return result;
    }

    /// <summary>
    /// Gets distinct secondary BIL values from the database for UI dropdowns.
    /// </summary>
    public async Task<IEnumerable<string>> GetDistinctSecondaryBILsAsync()
    {
        _logger.LogDebug("Querying distinct secondary BILs...");
        var result = await _dbContext.Params
            .Select(p => p.SecBil)
            .Distinct()
            .OrderBy(bil => bil)
            .ToListAsync();
        _logger.LogDebug("Retrieved {Count} secondary BIL records", result.Count);
        return result;
    }
}
