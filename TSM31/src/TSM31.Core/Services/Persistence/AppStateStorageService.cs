namespace TSM31.Core.Services.Persistence;

using Core.Models;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestData.Models;

/// <summary>
/// Service for persisting and restoring application session state to SQLite.
/// Enables recovery from crashes, power loss, and intentional restarts.
/// Uses IServiceProvider to create new scopes for each operation to avoid DbContext disposal issues.
/// </summary>
public class AppStateStorageService(
    IServiceProvider serviceProvider,
    ILogger<AppStateStorageService> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Saves the current session state (operator and/or unit data) to the database.
    /// Marks all previous sessions as non-current.
    /// </summary>
    public async Task SaveSessionAsync(
        Employee? employee,
        UnitData? unitData,
        string? currentTestAction = null,
        string sessionEndReason = "Active")
    {
        await _lock.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            await dbContext.Database.EnsureCreatedAsync();

            await CreateNewSessionAsync(dbContext, employee, unitData, currentTestAction, sessionEndReason);
            await dbContext.SaveChangesAsync();

            logger.LogDebug("Session saved: Operator={OperatorName}, Serial={SerialNumber}",
                employee?.Name, unitData?.SerialNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save session");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads the most recent active session from the database.
    /// Returns null if no active session exists.
    /// </summary>
    public async Task<SessionState?> LoadLastSessionAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            // Log database path for debugging
            logger.LogDebug("Database path: {DatabasePath}", dbContext.GetDatabasePath());

            var lastSession = await dbContext.SessionStates
                .Include(s => s.Unit)
                .ThenInclude(u => u!.Ratings)
                .Include(s => s.Unit)
                .ThenInclude(u => u!.HipotTests)
                .Include(s => s.Unit)
                .ThenInclude(u => u!.InducedTests)
                .Include(s => s.Unit)
                .ThenInclude(u => u!.ImpulseTests)
                .Where(s => s.IsCurrent)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (lastSession == null)
            {
                logger.LogDebug("No current session found in database");
                return null;
            }

            logger.LogDebug("Loaded session from database: Operator={OperatorName}, Serial={SerialNumber}",
                lastSession.OperatorName, lastSession.SerialNumber);

            // Reconstruct unit data from Unit if present
            UnitData? unitData = null;
            if (lastSession.Unit != null)
            {
                try
                {
                    unitData = MapUnitToUnitData(lastSession.Unit);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to reconstruct UnitData");
                }
            }

            // Reconstruct operator object if present
            Employee? currentEmployee = null;
            if (!string.IsNullOrEmpty(lastSession.OperatorId))
            {
                currentEmployee = new Employee {
                    Id = lastSession.OperatorId,
                    Name = lastSession.OperatorName ?? string.Empty,
                    SuperVisorId = lastSession.SupervisorId ?? string.Empty,
                    IsValid = true
                };
            }

            return new SessionState {
                Operator = currentEmployee,
                UnitData = unitData,
                CurrentTestAction = lastSession.CurrentTestAction,
                CreatedAt = lastSession.CreatedAt,
                UpdatedAt = lastSession.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load last session");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Maps a Unit entity and its related test entities back to a UnitData object
    /// </summary>
    private static UnitData MapUnitToUnitData(Unit unit)
    {
        var unitData = new UnitData {
            SerialNumber = unit.SerialNumber,
            WorkOrder = unit.WorkOrder,
            CatalogNumber = unit.CatalogNumber,
            CustomerName = unit.CustomerName,
            CheckNumber = unit.CheckNumber,
            Kva = unit.Kva,
            UnitType = (TransformerType)unit.UnitType,
            IsDownloaded = unit.IsDownloaded,
            IsManualEntry = unit.IsManualEntry,
            IsSideBySide = unit.IsSideBySide,
            PolarityDesign = unit.PolarityDesign,
            CurrentTest = unit.CurrentTest,
            TotalTests = unit.TotalTests,
            PrimaryBushings = unit.PrimaryBushings,
            PrimaryCoilConfiguration = unit.PrimaryCoilConfiguration,
            PrimaryMaterial = unit.PrimaryMaterial,
            PrimaryRatings = unit.PrimaryRatings,
            SecondaryBushings = unit.SecondaryBushings,
            SecondaryCoilConfiguration = unit.SecondaryCoilConfiguration,
            SecondaryMaterial = unit.SecondaryMaterial,
            SecondaryRatings = unit.SecondaryRatings,
            HasArrestor = unit.HasArrestor,
            HasDisconnect = unit.HasDisconnect,
            RegulatorType = unit.RegulatorType,
            RegulatorVoltageRating = unit.RegulatorVoltageRating,
            RegulatorBil = unit.RegulatorBil,
            RegulatorHipotSetCondition = unit.RegulatorHipotSetCondition
        };

        // Reconstruct Ratings collection
        foreach (var rating in unit.Ratings.OrderBy(r => r.TestNumber))
        {
            unitData.Ratings.Add(new Ratings {
                PrimaryVoltage = rating.PrimaryVoltage,
                PrimaryCurrent = rating.PrimaryCurrent,
                PrimaryBIL = rating.PrimaryBIL,
                SecondaryVoltage = rating.SecondaryVoltage,
                SecondaryCurrent = rating.SecondaryCurrent,
                SecondaryBIL = rating.SecondaryBIL
            });
        }

        // Reconstruct Hipot collection
        foreach (var hipot in unit.HipotTests.OrderBy(h => h.TestNumber))
        {
            unitData.Hipot.Add(new HipotData {
                PrimaryStatus = new TestStatus((TestStatusType)hipot.PrimaryStatus),
                PrimaryLimitText = hipot.PrimaryLimitText,
                PrimaryVoltStatus = hipot.PrimaryVoltStatus,
                PrimaryAmpStatus = hipot.PrimaryAmpStatus,
                PrimaryTimeStatus = hipot.PrimaryTimeStatus,
                PrimaryLimit = hipot.PrimaryLimit,
                PrimarySetCondition = hipot.PrimarySetCondition,
                PrimaryKv = hipot.PrimaryKv,
                PrimaryCurrent = hipot.PrimaryCurrent,
                PrimaryTimeRequired = hipot.PrimaryTimeRequired,
                PrimaryTestTime = hipot.PrimaryTestTime,
                SecondaryStatus = new TestStatus((TestStatusType)hipot.SecondaryStatus),
                SecondaryLimitText = hipot.SecondaryLimitText,
                SecondaryVoltStatus = hipot.SecondaryVoltStatus,
                SecondaryAmpStatus = hipot.SecondaryAmpStatus,
                SecondaryTimeStatus = hipot.SecondaryTimeStatus,
                SecondaryLimit = hipot.SecondaryLimit,
                SecondarySetCondition = hipot.SecondarySetCondition,
                SecondaryKv = hipot.SecondaryKv,
                SecondaryCurrent = hipot.SecondaryCurrent,
                SecondaryTimeRequired = hipot.SecondaryTimeRequired,
                SecondaryTestTime = hipot.SecondaryTestTime,
                FourLvbStatus = new TestStatus((TestStatusType)hipot.FourLvbStatus),
                SetCondition = hipot.SetCondition,
                FourLvbSetCondition = hipot.FourLvbSetCondition,
                FourLvbTestTime = hipot.FourLvbTestTime,
                FourLvbLimit = hipot.FourLvbLimit,
                FourLvbKv = hipot.FourLvbKv,
                FourLvbCurrent = hipot.FourLvbCurrent,
                FourLvbTimeRequired = hipot.FourLvbTimeRequired
            });
        }

        // Reconstruct Induced collection
        foreach (var induced in unit.InducedTests.OrderBy(i => i.TestNumber))
        {
            unitData.Induced.Add(new InducedData {
                FirstStatus = new TestStatus((TestStatusType)induced.FirstStatus),
                FirstLimitText = induced.FirstLimitText,
                FirstVoltStatus = induced.FirstVoltStatus,
                FirstWattStatus = induced.FirstWattStatus,
                FirstTimeRequired = induced.FirstTimeRequired,
                FirstVoltage = induced.FirstVoltage,
                FirstPower = induced.FirstPower,
                FirstCurrent = induced.FirstCurrent,
                FirstTestTime = induced.FirstTestTime,
                FirstC1V = induced.FirstC1V,
                FirstC1A = induced.FirstC1A,
                FirstC1W = induced.FirstC1W,
                FirstC2V = induced.FirstC2V,
                FirstC2A = induced.FirstC2A,
                FirstC2W = induced.FirstC2W,
                FirstC3V = induced.FirstC3V,
                FirstC3A = induced.FirstC3A,
                FirstC3W = induced.FirstC3W,
                SecondStatus = new TestStatus((TestStatusType)induced.SecondStatus),
                SecondLimitText = induced.SecondLimitText,
                SecondVoltStatus = induced.SecondVoltStatus,
                SecondWattStatus = induced.SecondWattStatus,
                SecondTimeRequired = induced.SecondTimeRequired,
                SecondVoltage = induced.SecondVoltage,
                SecondPower = induced.SecondPower,
                SecondCurrent = induced.SecondCurrent,
                SecondTestTime = induced.SecondTestTime,
                SecondC1V = induced.SecondC1V,
                SecondC1A = induced.SecondC1A,
                SecondC1W = induced.SecondC1W,
                SecondC2V = induced.SecondC2V,
                SecondC2A = induced.SecondC2A,
                SecondC2W = induced.SecondC2W,
                SecondC3V = induced.SecondC3V,
                SecondC3A = induced.SecondC3A,
                SecondC3W = induced.SecondC3W,
                WattLimit = induced.WattLimit,
                SetCondition = induced.SetCondition,
                VoltageRange = induced.VoltageRange,
                CurrentRange = induced.CurrentRange
            });
        }

        // Reconstruct Impulse collection
        foreach (var impulse in unit.ImpulseTests.OrderBy(i => i.TestNumber))
        {
            unitData.Impulse.Add(new ImpulseData {
                H1ShotCounter = impulse.H1ShotCounter,
                H1Status = new TestStatus((TestStatusType)impulse.H1Status),
                H1WaveformCompare = new TestStatus((TestStatusType)impulse.H1WaveformCompareStatus),
                H1Voltage = impulse.H1Voltage,
                H2ShotCounter = impulse.H2ShotCounter,
                H2Status = new TestStatus((TestStatusType)impulse.H2Status),
                H2WaveformCompare = new TestStatus((TestStatusType)impulse.H2WaveformCompareStatus),
                H2Voltage = impulse.H2Voltage,
                H3ShotCounter = impulse.H3ShotCounter,
                H3Status = new TestStatus((TestStatusType)impulse.H3Status),
                H3WaveformCompare = new TestStatus((TestStatusType)impulse.H3WaveformCompareStatus),
                H3Voltage = impulse.H3Voltage,
                X1ShotCounter = impulse.X1ShotCounter,
                X1Status = new TestStatus((TestStatusType)impulse.X1Status),
                X1WaveformCompare = new TestStatus((TestStatusType)impulse.X1WaveformCompareStatus),
                X1Voltage = impulse.X1Voltage,
                X2ShotCounter = impulse.X2ShotCounter,
                X2Status = new TestStatus((TestStatusType)impulse.X2Status),
                X2WaveformCompare = new TestStatus((TestStatusType)impulse.X2WaveformCompareStatus),
                X2Voltage = impulse.X2Voltage,
                X3ShotCounter = impulse.X3ShotCounter,
                X3Status = new TestStatus((TestStatusType)impulse.X3Status),
                X3WaveformCompare = new TestStatus((TestStatusType)impulse.X3WaveformCompareStatus),
                X3Voltage = impulse.X3Voltage,
                SetCondition = impulse.SetCondition,
                SecondarySetCondition = impulse.SecondarySetCondition
            });
        }

        return unitData;
    }

    /// <summary>
    /// Gets recent session history for display/selection.
    /// </summary>
    public async Task<List<SessionSummary>> GetRecentSessionsAsync(int limit = 10)
    {
        await _lock.WaitAsync();
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            var sessions = await dbContext.SessionStates
                .OrderByDescending(s => s.UpdatedAt)
                .Take(limit)
                .Select(s => new SessionSummary {
                    Id = s.Id,
                    OperatorName = s.OperatorName,
                    OperatorId = s.OperatorId,
                    SerialNumber = s.SerialNumber,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsCurrent = s.IsCurrent,
                    SessionEndReason = s.SessionEndReason
                })
                .ToListAsync();

            return sessions;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Clears the current session by marking it as non-current.
    /// Use this when user manually logs out or clears unit data.
    /// </summary>
    public async Task ClearCurrentSessionAsync(string reason = "Manual logout/clear")
    {
        await _lock.WaitAsync();
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            var currentSessions = await dbContext.SessionStates
                .Where(s => s.IsCurrent)
                .ToListAsync();

            foreach (var session in currentSessions)
            {
                session.IsCurrent = false;
                session.SessionEndReason = reason;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            logger.LogDebug("Current session cleared: {Reason}", reason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear current session");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Updates only the unit data portion of the current session.
    /// Links to existing Unit or creates new one in historical database.
    /// NEVER deletes Unit records - they are permanent history.
    /// </summary>
    public async Task UpdateUnitDataAsync(UnitData? unitData)
    {
        await _lock.WaitAsync();
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            // Ensure database is created before attempting operations
            await dbContext.Database.EnsureCreatedAsync();

            var currentSession = await dbContext.SessionStates
                .Where(s => s.IsCurrent)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (currentSession != null)
            {
                // Update serial number and link to Unit in historical database
                currentSession.SerialNumber = unitData?.SerialNumber;
                currentSession.UpdatedAt = DateTime.UtcNow;

                if (unitData != null)
                {
                    // Check if Unit exists with this SerialNumber + WorkOrder
                    var existingUnit = await dbContext.Units
                        .FirstOrDefaultAsync(u =>
                            u.SerialNumber == unitData.SerialNumber &&
                            u.WorkOrder == unitData.WorkOrder);

                    if (existingUnit != null)
                    {
                        // Link to existing unit and update timestamp
                        currentSession.UnitId = existingUnit.Id;
                        existingUnit.UpdatedAt = DateTime.UtcNow;
                        logger.LogDebug("Linked to existing Unit: Serial={SerialNumber}, WO={WorkOrder}",
                            unitData.SerialNumber, unitData.WorkOrder);
                    }
                    else
                    {
                        // Create new unit in historical database
                        var unit = MapUnitDataToUnit(unitData, null);// No operator context in this path
                        dbContext.Units.Add(unit);
                        currentSession.Unit = unit;// EF will set UnitId
                        logger.LogDebug("Created new Unit: Serial={SerialNumber}, WO={WorkOrder}",
                            unitData.SerialNumber, unitData.WorkOrder);
                    }
                }
                else
                {
                    // Clear unit link (no unit loaded)
                    currentSession.UnitId = null;
                }

                await dbContext.SaveChangesAsync();
                logger.LogDebug("Unit data updated in session: Serial={SerialNumber}", unitData?.SerialNumber);
            }
            else
            {
                await CreateNewSessionAsync(dbContext, null, unitData, null, "Auto-created (unit update)");
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Session created with unit data: Serial={SerialNumber}", unitData?.SerialNumber);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update unit data");
            // Don't throw - allow operation to succeed even if persistence fails
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Updates only the operator portion of the current session.
    /// Useful when operator logs in/out but unit data remains.
    /// </summary>
    public async Task UpdateOperatorAsync(Employee? employee)
    {
        await _lock.WaitAsync();
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();

            // Ensure database is created before attempting operations
            await dbContext.Database.EnsureCreatedAsync();

            var currentSession = await dbContext.SessionStates
                .Where(s => s.IsCurrent)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (currentSession != null)
            {
                currentSession.OperatorId = employee?.Id;
                currentSession.OperatorName = employee?.Name;
                currentSession.SupervisorId = employee?.SuperVisorId;
                currentSession.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                logger.LogDebug("Operator updated in session: {OperatorName}", employee?.Name);
            }
            else
            {
                logger.LogDebug("No current session, creating new one for operator: {OperatorName}", employee?.Name);
                await CreateNewSessionAsync(dbContext, employee, null, null, "Auto-created (operator update)");
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Session created with operator: {OperatorName}", employee?.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update operator");
            // Don't throw - allow login to succeed even if persistence fails
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task CreateNewSessionAsync(
        TSM31StateDbContext dbContext,
        Employee? employee,
        UnitData? unitData,
        string? currentTestAction,
        string sessionEndReason)
    {
        var existingSessions = await dbContext.SessionStates
            .Where(s => s.IsCurrent)
            .ToListAsync();

        foreach (var session in existingSessions)
        {
            session.IsCurrent = false;
            session.UpdatedAt = DateTime.UtcNow;
        }

        var newSession = new AppSessionState {
            OperatorId = employee?.Id,
            OperatorName = employee?.Name,
            SupervisorId = employee?.SuperVisorId,
            SerialNumber = unitData?.SerialNumber,
            CurrentTestAction = currentTestAction,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsCurrent = true,
            SessionEndReason = sessionEndReason
        };

        // Link to Unit in historical database if UnitData is present
        if (unitData != null)
        {
            // Check if Unit already exists with this SerialNumber + WorkOrder
            var existingUnit = await dbContext.Units
                .Include(u => u.Ratings)
                .Include(u => u.HipotTests)
                .Include(u => u.InducedTests)
                .Include(u => u.ImpulseTests)
                .FirstOrDefaultAsync(u =>
                    u.SerialNumber == unitData.SerialNumber &&
                    u.WorkOrder == unitData.WorkOrder);

            if (existingUnit != null)
            {
                // Unit already exists - link to it and update timestamp
                logger.LogDebug("Linking to existing Unit: Serial={SerialNumber}, WO={WorkOrder}",
                    unitData.SerialNumber, unitData.WorkOrder);
                newSession.UnitId = existingUnit.Id;
                existingUnit.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // New unit - create historical record
                logger.LogDebug("Creating new Unit: Serial={SerialNumber}, WO={WorkOrder}",
                    unitData.SerialNumber, unitData.WorkOrder);
                var unit = MapUnitDataToUnit(unitData, employee);
                dbContext.Units.Add(unit);
                newSession.Unit = unit;// EF will set UnitId automatically
            }
        }

        dbContext.SessionStates.Add(newSession);
    }

    /// <summary>
    /// Maps a UnitData object to Unit entity and all related test entities.
    /// Sets timestamps and operator information for historical tracking.
    /// </summary>
    private static Unit MapUnitDataToUnit(UnitData unitData, Employee? @operator)
    {
        var now = DateTime.UtcNow;
        var unit = new Unit {
            SerialNumber = unitData.SerialNumber,
            WorkOrder = unitData.WorkOrder,
            CatalogNumber = unitData.CatalogNumber,
            CustomerName = unitData.CustomerName,
            CheckNumber = unitData.CheckNumber,
            Kva = unitData.Kva,
            UnitType = (int)unitData.UnitType,
            IsDownloaded = unitData.IsDownloaded,
            IsManualEntry = unitData.IsManualEntry,
            IsSideBySide = unitData.IsSideBySide,
            PolarityDesign = unitData.PolarityDesign,
            CurrentTest = unitData.CurrentTest,
            TotalTests = unitData.TotalTests,
            PrimaryBushings = unitData.PrimaryBushings,
            PrimaryCoilConfiguration = unitData.PrimaryCoilConfiguration,
            PrimaryMaterial = unitData.PrimaryMaterial,
            PrimaryRatings = unitData.PrimaryRatings,
            SecondaryBushings = unitData.SecondaryBushings,
            SecondaryCoilConfiguration = unitData.SecondaryCoilConfiguration,
            SecondaryMaterial = unitData.SecondaryMaterial,
            SecondaryRatings = unitData.SecondaryRatings,
            HasArrestor = unitData.HasArrestor,
            HasDisconnect = unitData.HasDisconnect,
            RegulatorType = unitData.RegulatorType,
            RegulatorVoltageRating = unitData.RegulatorVoltageRating,
            RegulatorBil = unitData.RegulatorBil,
            RegulatorHipotSetCondition = unitData.RegulatorHipotSetCondition,
            // Operator tracking
            OperatorId = @operator?.Id,
            OperatorName = @operator?.Name,
            SupervisorId = @operator?.SuperVisorId,
            // Timestamps
            DownloadedAt = now,
            UpdatedAt = now
        };

        // Map Ratings collection
        for (int i = 0; i < unitData.Ratings.Count; i++)
        {
            var rating = unitData.Ratings[i];
            unit.Ratings.Add(new Rating {
                TestNumber = i + 1,
                PrimaryVoltage = rating.PrimaryVoltage,
                PrimaryCurrent = rating.PrimaryCurrent,
                PrimaryBIL = rating.PrimaryBIL,
                SecondaryVoltage = rating.SecondaryVoltage,
                SecondaryCurrent = rating.SecondaryCurrent,
                SecondaryBIL = rating.SecondaryBIL,
                DesignRatio = rating.DesignRatio,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Map Hipot collection
        for (int i = 0; i < unitData.Hipot.Count; i++)
        {
            var hipot = unitData.Hipot[i];
            unit.HipotTests.Add(new HipotTest {
                TestNumber = i + 1,
                PrimaryStatus = (int)hipot.PrimaryStatus.Status,
                PrimaryLimitText = hipot.PrimaryLimitText,
                PrimaryVoltStatus = hipot.PrimaryVoltStatus,
                PrimaryAmpStatus = hipot.PrimaryAmpStatus,
                PrimaryTimeStatus = hipot.PrimaryTimeStatus,
                PrimaryLimit = hipot.PrimaryLimit,
                PrimarySetCondition = hipot.PrimarySetCondition,
                PrimaryKv = hipot.PrimaryKv,
                PrimaryCurrent = hipot.PrimaryCurrent,
                PrimaryTimeRequired = hipot.PrimaryTimeRequired,
                PrimaryTestTime = hipot.PrimaryTestTime,
                SecondaryStatus = (int)hipot.SecondaryStatus.Status,
                SecondaryLimitText = hipot.SecondaryLimitText,
                SecondaryVoltStatus = hipot.SecondaryVoltStatus,
                SecondaryAmpStatus = hipot.SecondaryAmpStatus,
                SecondaryTimeStatus = hipot.SecondaryTimeStatus,
                SecondaryLimit = hipot.SecondaryLimit,
                SecondarySetCondition = hipot.SecondarySetCondition,
                SecondaryKv = hipot.SecondaryKv,
                SecondaryCurrent = hipot.SecondaryCurrent,
                SecondaryTimeRequired = hipot.SecondaryTimeRequired,
                SecondaryTestTime = hipot.SecondaryTestTime,
                FourLvbStatus = (int)hipot.FourLvbStatus.Status,
                SetCondition = hipot.SetCondition,
                FourLvbSetCondition = hipot.FourLvbSetCondition,
                FourLvbTestTime = hipot.FourLvbTestTime,
                FourLvbLimit = hipot.FourLvbLimit,
                FourLvbKv = hipot.FourLvbKv,
                FourLvbCurrent = hipot.FourLvbCurrent,
                FourLvbTimeRequired = hipot.FourLvbTimeRequired,
                // Timestamps - tests not started yet when downloaded
                StartedAt = null,
                CompletedAt = null,
                UpdatedAt = now
            });
        }

        // Map Induced collection
        for (int i = 0; i < unitData.Induced.Count; i++)
        {
            var induced = unitData.Induced[i];
            unit.InducedTests.Add(new InducedTest {
                TestNumber = i + 1,
                FirstStatus = (int)induced.FirstStatus.Status,
                FirstLimitText = induced.FirstLimitText,
                FirstVoltStatus = induced.FirstVoltStatus,
                FirstWattStatus = induced.FirstWattStatus,
                FirstTimeRequired = induced.FirstTimeRequired,
                FirstVoltage = induced.FirstVoltage,
                FirstPower = induced.FirstPower,
                FirstCurrent = induced.FirstCurrent,
                FirstTestTime = induced.FirstTestTime,
                FirstC1V = induced.FirstC1V,
                FirstC1A = induced.FirstC1A,
                FirstC1W = induced.FirstC1W,
                FirstC2V = induced.FirstC2V,
                FirstC2A = induced.FirstC2A,
                FirstC2W = induced.FirstC2W,
                FirstC3V = induced.FirstC3V,
                FirstC3A = induced.FirstC3A,
                FirstC3W = induced.FirstC3W,
                SecondStatus = (int)induced.SecondStatus.Status,
                SecondLimitText = induced.SecondLimitText,
                SecondVoltStatus = induced.SecondVoltStatus,
                SecondWattStatus = induced.SecondWattStatus,
                SecondTimeRequired = induced.SecondTimeRequired,
                SecondVoltage = induced.SecondVoltage,
                SecondPower = induced.SecondPower,
                SecondCurrent = induced.SecondCurrent,
                SecondTestTime = induced.SecondTestTime,
                SecondC1V = induced.SecondC1V,
                SecondC1A = induced.SecondC1A,
                SecondC1W = induced.SecondC1W,
                SecondC2V = induced.SecondC2V,
                SecondC2A = induced.SecondC2A,
                SecondC2W = induced.SecondC2W,
                SecondC3V = induced.SecondC3V,
                SecondC3A = induced.SecondC3A,
                SecondC3W = induced.SecondC3W,
                WattLimit = induced.WattLimit,
                SetCondition = induced.SetCondition,
                VoltageRange = induced.VoltageRange,
                CurrentRange = induced.CurrentRange,
                // Timestamps - tests not started yet when downloaded
                FirstStartedAt = null,
                FirstCompletedAt = null,
                SecondStartedAt = null,
                SecondCompletedAt = null,
                UpdatedAt = now
            });
        }

        // Map Impulse collection
        for (int i = 0; i < unitData.Impulse.Count; i++)
        {
            var impulse = unitData.Impulse[i];
            unit.ImpulseTests.Add(new ImpulseTest {
                TestNumber = i + 1,
                H1ShotCounter = impulse.H1ShotCounter,
                H1Status = (int)impulse.H1Status.Status,
                H1WaveformCompareStatus = (int)impulse.H1WaveformCompare.Status,
                H1Voltage = impulse.H1Voltage,
                H2ShotCounter = impulse.H2ShotCounter,
                H2Status = (int)impulse.H2Status.Status,
                H2WaveformCompareStatus = (int)impulse.H2WaveformCompare.Status,
                H2Voltage = impulse.H2Voltage,
                H3ShotCounter = impulse.H3ShotCounter,
                H3Status = (int)impulse.H3Status.Status,
                H3WaveformCompareStatus = (int)impulse.H3WaveformCompare.Status,
                H3Voltage = impulse.H3Voltage,
                X1ShotCounter = impulse.X1ShotCounter,
                X1Status = (int)impulse.X1Status.Status,
                X1WaveformCompareStatus = (int)impulse.X1WaveformCompare.Status,
                X1Voltage = impulse.X1Voltage,
                X2ShotCounter = impulse.X2ShotCounter,
                X2Status = (int)impulse.X2Status.Status,
                X2WaveformCompareStatus = (int)impulse.X2WaveformCompare.Status,
                X2Voltage = impulse.X2Voltage,
                X3ShotCounter = impulse.X3ShotCounter,
                X3Status = (int)impulse.X3Status.Status,
                X3WaveformCompareStatus = (int)impulse.X3WaveformCompare.Status,
                X3Voltage = impulse.X3Voltage,
                SetCondition = impulse.SetCondition,
                SecondarySetCondition = impulse.SecondarySetCondition,
                // Timestamps - tests not started yet when downloaded
                StartedAt = null,
                CompletedAt = null,
                UpdatedAt = now
            });
        }

        return unit;
    }
}

/// <summary>
/// Represents a loaded session state with all components
/// </summary>
public class SessionState
{
    public Employee? Operator { get; set; }
    public UnitData? UnitData { get; set; }
    public string? CurrentTestAction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Lightweight summary of a session for display in lists
/// </summary>
public class SessionSummary
{
    public int Id { get; set; }
    public string? OperatorName { get; set; }
    public string? OperatorId { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsCurrent { get; set; }
    public string? SessionEndReason { get; set; }
}
