# Unit Download, Parsing, and UI Display Flow

This document explains how a unit is downloaded by serial number, transformed into structured runtime objects (`UnitData` and its child collections), and rendered on the WinForms UI. Source code excerpts are copied directly from the application for traceability.

---
## 1. Primary Entry Point: `Download`
File: `HostCommunication.vb`

```vbnet
Public Function Download(ByVal strSerialNumber As String) As Boolean
    Dim strTitle As String = "Host Communications"
    MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, strTitle, True)

    If CurrentUnit.Downloaded Or CurrentUnit.ManualEntry Then
        Dim strMsg As String = "Previous test data has not been uploaded!" & vbCrLf & _
          "Do you want to upload the previous test?"
        Dim intMsgBoxResult As Integer = MsgBox(strMsg, CType(MsgBoxStyle.YesNo + MsgBoxStyle.Exclamation, MsgBoxStyle), strTitle)
        If intMsgBoxResult = vbYes Then
            If Not Upload() Then
                Return False
            End If
        Else
            WritePartialData()
        End If
        strMsg = "Do you want to download new test data?"
        intMsgBoxResult = MsgBox(strMsg, CType(MsgBoxStyle.YesNo + MsgBoxStyle.Exclamation, MsgBoxStyle), strTitle)
        If intMsgBoxResult = vbNo Then
            Return False
        End If
    End If

    CurrentUnit = Nothing
    CurrentUnit = New UnitData(TestStation.MachineID, g_strOperatorID, g_strSupervisorID)
    CurrentUnit.SerialNumber = strSerialNumber

    MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, "Requesting Customer Name from Host.")
    RequestDataFromWebService(strSerialNumber)

    Dim conn As New SqlConnection(g_ConnectionString)
    Dim cmd As New SqlCommand
    cmd.Connection = conn
    cmd.CommandType = CommandType.Text
    cmd.CommandText = "SELECT * FROM Xref WHERE [SERNO]='" & strSerialNumber & "'"
    Dim dr As SqlDataReader
    Dim strCatalogNumber As String = String.Empty
    Dim strWorkOrder As String = String.Empty
    Try
        MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, _
            "Checking Cross Reference table for Serial Number (" & strSerialNumber & ")...")
        conn.Open()
        dr = cmd.ExecuteReader
        If dr.HasRows Then
            dr.Read()
            strWorkOrder = dr(2).ToString
            strCatalogNumber = dr(1).ToString
            dr.Close()
            If strCatalogNumber.Substring(0, 2) = "28" Then
                strWorkOrder = "00000"
                CurrentUnit.UnitType = UnitData.TransformerType.Regulator
            End If
            MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, "Requesting Transformer Test Parameters...")
            cmd.CommandText = "SELECT * FROM Params WHERE [WorkOrder]='" & strWorkOrder & _
                                "' AND [CatalogNumber]='" & strCatalogNumber & _
                                "' ORDER BY [TestNumber] ASC"
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection)
            If dr.HasRows Then
                While dr.Read
                    If IsNumeric(dr(0)) Then
                        MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, _
                            "Downloading Test #" & dr(0).ToString)
                        Call CurrentUnit.ParseTransformerParams(strSerialNumber, dr)
                        MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtInfo, _
                            "Test #" & dr(0).ToString & " successfully received.")
                    End If
                End While
                CurrentUnit.Downloaded = True
                If MainForm.TestTabs.SelectedTab.Name = "DataReview" Then
                    TestTabsLoad.LoadTransformerTestingPage()
                End If
            Else
                MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtWarning, _
                    "Could not find test data for Serial Number (" & strSerialNumber & ") in Param table.")
                Delay(250)
                CurrentUnit.DefaultUnitData()
            End If
        Else
            MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtWarning, _
                "Could not find Serial Number (" & strSerialNumber & ") in Cross Reference table.")
            Delay(250)
            CurrentUnit.DefaultUnitData()
        End If
    Catch ex As Exception
        MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtError, ex.Message)
        Delay(250)
        CurrentUnit.DefaultUnitData()
    End Try
    Return False
End Function
```

### 1.1 Responsibilities
1. Resolves prior un-uploaded data (upload / partial log / abort).
2. Reinitializes `CurrentUnit` with operator & machine context.
3. Enriches customer/work order via web service.
4. Resolves catalog & work order from `Xref`.
5. Pulls ordered test parameter rows from `Params`.
6. Delegates each row to `ParseTransformerParams`.
7. Flags `Downloaded` and refreshes UI.

### 1.2 Notable Issue
The function always ends with `Return False` (never returns `True` on success). A fix would conditionally return `True` when at least one test row parsed.

---
## 2. Web Service Enrichment: `RequestDataFromWebService`
File: `HostCommunication.vb`

```vbnet
Public Sub RequestDataFromWebService(ByVal SerNum As String)
    Dim result As String = String.Empty
    Dim ResultText As String = String.Empty
    Dim r As String = String.Empty
    Dim i As Integer = 0
    Try
        r = "000," & SerNum & ",00000,XXXXXXXXXXXXXXXXXXXXXXX,00000,000000,000000,00000000,00000000,0,XXXXXXXXXX,0000000000000,"
        For i = 1 To 29
            r = r & "000,0000000000,00000,XXXXXXXXXXXXXXXXXXXXXXX,00000,000000,000000,00000000,00000000,0,XXXXXXXXXX,0000000000000,"
        Next
        Using UnitInfo As New WebSrvcCall.UnitInfoWS
            result = UnitInfo.GetSerialData("SOAK", r)
            If (result <> "000") And (result <> String.Empty) Then
                If IsNumeric(result) Then
                    ResultText = UnitInfo.ResponseCodeText(result)
                Else
                    ResultText = result
                End If
            End If
        End Using
    Catch ex As Exception
        If ex.InnerException.Message <> String.Empty Then
            result = ex.InnerException.Message
            If ex.InnerException.Message.Contains("Unable to read data from the transport connection") Then
                Dim _url As String = "http://10.12.1.30:8021/cics/CWBA/IESSOAPS"
                Dim sp As System.Net.ServicePoint = System.Net.ServicePointManager.FindServicePoint(New Uri(_url))
                sp.CloseConnectionGroup("INFO")
            End If
        Else
            result = ex.Message
        End If
        ResultText = result
    Finally
        If result = "000" Then
            Dim info As String() = Split(r, ",")
            With CurrentUnit
                .CustomerName = info(3).Trim
                .WorkOrder = info(2).Trim
            End With
        Else
            With CurrentUnit
                .CustomerName = "Valued Customer"
                .WorkOrder = "00000"
            End With
            MainForm.MsgDisplay.Display(ctlMsgDisplay.MessageType.mtError, _
                "Unable to look up Customer Name/Work Order.  " & ResultText)
            TraceLog.LogToFile(SerNum & ": Unable to lookup customer information.  See error log.")
        End If
    End Try
End Sub
```

### 2.1 Role
Populates `CurrentUnit.CustomerName` and `CurrentUnit.WorkOrder` before SQL parameter acquisition. Falls back to defaults on failure.

---
## 3. Parsing Each Test Row: `ParseTransformerParams`
File: `UnitData.vb`

```vbnet
Public Function ParseTransformerParams(ByVal serialNumber As String, ByVal dr As SqlDataReader) As Boolean
    With Me
        .CurrentTest = CInt(dr(0))
        .TotalTests = .CurrentTest
        If .CurrentTest = 1 Then
            If dr(19).ToString = "Y" Then .Arrestor = True
            If dr(49).ToString = "Y" Then .Disconnect = True
            .SerialNumber = serialNumber
            .WorkOrder = dr(1).ToString
            .CatalogNumber = dr(2).ToString
            .CheckNum = CalculateCheckNumber(.SerialNumber, .CatalogNumber)
            .KVA = CSng(dr(3).ToString)
            .PrimaryBushings = CInt(dr(7).ToString)
            .PrimaryMaterial = dr(13).ToString
            .PrimaryRatings = CInt(dr(9).ToString)
            .SecondaryBushings = CInt(dr(8).ToString)
            .SecondaryMaterial = dr(14).ToString
            .SecondaryRatings = CInt(dr(10).ToString)
            .PrimaryCoilCfg = dr(11).ToString
            .SecondaryCoilCfg = dr(12).ToString
            .PolarityDesign = dr(15).ToString
            If (dr(6).ToString = "2") Or (dr(6).ToString = "6") Then .UnitType = Set3phaseUnitType()
            If dr(6).ToString = "1" Then
                If (.CatalogNumber.StartsWith("14")) Or (.CatalogNumber.StartsWith("24")) Then
                    .UnitType = TransformerType.StepDown
                Else
                    .UnitType = TransformerType.SinglePhase
                End If
            End If
            If dr(53).ToString = "Y" Then .SideBySide = True
        End If
        ' Ratings collection
        Dim clsRatingsTemp As New Ratings
        clsRatingsTemp.PrimaryVoltage = CLng(dr(4).ToString)
        clsRatingsTemp.PrimaryCurrent = mKVAValue * 1000 / clsRatingsTemp.PrimaryVoltage
        clsRatingsTemp.SecondaryVoltage = CLng(dr(5).ToString)
        clsRatingsTemp.SecondaryCurrent = mKVAValue * 1000 / clsRatingsTemp.SecondaryVoltage
        clsRatingsTemp.PrimaryBIL = CInt(dr(17).ToString)
        clsRatingsTemp.SecondaryBIL = CInt(dr(18).ToString)
        CurrentUnit.Ratings.Add(clsRatingsTemp)
        ' Hipot
        Dim clsHipotTemp As New HipotData
        clsHipotTemp.PrimaryStatus.Status = CType(IIf(dr(45).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsHipotTemp.PrimaryLimit = CInt(dr(23).ToString)
        ' (BIL to SetCondition mapping omitted here for brevity – full logic in source)
        clsHipotTemp.SecondaryStatus.Status = CType(IIf(dr(46).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsHipotTemp.SecondaryLimit = CInt(dr(22).ToString)
        If (dr(50).ToString = "R") And Not Is3Phase() Then
            clsHipotTemp.FourLVBStatus.Status = TestStatus.StatusType.Required
            clsHipotTemp.FourLVBSetCondition = CInt(CInt(dr(51).ToString) / 1000)
            clsHipotTemp.FourLVBTestTime = CInt(dr(52).ToString)
            clsHipotTemp.FourLVBLimit = clsHipotTemp.SecondaryLimit
        Else
            clsHipotTemp.FourLVBStatus.Status = TestStatus.StatusType.NotRequired
        End If
        CurrentUnit.Hipot.Add(clsHipotTemp)
        ' Induced
        Dim clsInducedTemp As New Induced
        clsInducedTemp.FirstStatus.Status = CType(IIf(dr(38).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsInducedTemp.SecondStatus.Status = CType(IIf(dr(47).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsInducedTemp.FirstTimeRequired = TestStation.FirstInducedTime
        clsInducedTemp.SecondTimeRequired = CInt(dr(48).ToString)
        clsInducedTemp.WattLimit = CInt(dr(21).ToString)
        clsInducedTemp.SetCondition = CInt(dr(20).ToString)
        Induced.Add(clsInducedTemp)
        ' Impulse
        Dim clsImpulseTemp As New Impulse
        clsImpulseTemp.H1Status.Status = CType(IIf(dr(39).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsImpulseTemp.H2Status.Status = CType(IIf(dr(40).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsImpulseTemp.H3Status.Status = CType(IIf(dr(41).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsImpulseTemp.X1Status.Status = CType(IIf(dr(42).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsImpulseTemp.X2Status.Status = CType(IIf(dr(43).ToString = "R", TestStatus.StatusType.Required, TestStatus.StatusType.NotRequired), TestStatus.StatusType)
        clsImpulseTemp.SetCondition = clsRatingsTemp.PrimaryBIL
        If .UnitType = TransformerType.StepDown Then
            clsImpulseTemp.SecondarySetCondition = clsRatingsTemp.SecondaryBIL
        End If
        Impulse.Add(clsImpulseTemp)
    End With
    MainForm.DisplayTransformerInfo()
    Return True
End Function
```

### 3.1 Effects
- First row initializes global transformer metadata.
- Every row appends one element to each collection: `Ratings`, `Hipot`, `Induced`, `Impulse`.
- `TotalTests` becomes the highest encountered test number (implicit monotonic assumption).

---
## 4. Data Model Snapshot (`UnitData`)
File: `UnitData.vb`

Key persisted / session fields:
- Identity: `SerialNumber`, `CatalogNumber`, `WorkOrder`, `CustomerName`, `CheckNum`.
- Configuration: `KVA`, `Primary/SecondaryVoltage`, BILs, bushings, coil configs, `UnitType`, `PolarityDesign`.
- Collections (indexed per test number): `Ratings`, `Hipot`, `Induced`, `Impulse`.
- State: `Downloaded`, `CurrentTest`, `TotalTests`, `ManualEntry`.

Supporting upload serialization: `BuildPacket` (formats a positional string combining statuses, numeric values, and limits for each test) for multi-test export.

---
## 5. UI Binding / Display Pipeline

### 5.1 Initial Refresh
`TestTabsLoad.LoadTransformerTestingPage()` (file: `TestTabsLoad.vb`) picks a current test (via `ChooseCurrentTestNumber`), ensures safe state (power off, meter stopped), and calls `LoadTestingRecordedData()` to push collection contents into three main list views (Induced, Hipot, Impulse) in the Data Review tab.

```vbnet
Public Sub LoadTransformerTestingPage(Optional ByVal ManualTestChange As Boolean = False)
    LoadingTab = True
    MainForm.TestTabs.TabPages("Induced").Text = "Induced"
    ' ... (power + meter reset) ...
    If Not ManualTestChange Then
        Call ChooseCurrentTestNumber()
    End If
    If MainForm.TestTabs.SelectedTab.Name <> "DataReview" Then
        MainForm.TestTabs.SelectTab("DataReview")
        MainForm.MsgDisplay.Clear()
    End If
    LoadTestingRecordedData()
    LoadingTab = False
End Sub
```

### 5.2 Recorded Data Population
`LoadTestingRecordedData()` iterates each collection and builds rows with color-coded status cells (Green = Passed, Red = Failed, Yellow = Aborted, LightBlue row highlight = current test).

```vbnet
For Each oInduced In CurrentUnit.Induced
    itmX = New ListViewItem
    itmX.Text = intTestNumber.ToString
    If intTestNumber = CurrentUnit.CurrentTest Then itmX.BackColor = Color.LightBlue
    itmX.SubItems.Add(oInduced.FirstVoltage.ToString("#0.00"))
    ' ... (more subitems + status coloring) ...
Next
```

Same pattern for Hipot (`lvwHipotTestData`) and Impulse (`lvwImpulseData`).

### 5.3 Data Entry Tab Binding
When navigating to Data Entry (`LoadDataEntryPage` → `LoadDataEntryFields`), transformer-level attributes and the per-test rating at `CurrentTest` index populate text boxes & combo boxes:

```vbnet
.SerialNumberTextBox.Text = CurrentUnit.SerialNumber
.CatalogNumberTextBox.Text = CurrentUnit.CatalogNumber
.PrimaryVoltageTextBox.Text = CurrentUnit.Ratings(index).PrimaryVoltage.ToString("##,##0")
' ... status combo boxes: REQUIRED / NOTREQUIRED mapping ...
```

### 5.4 Drill-Down Test Pages
Selecting specific test categories (Induced / Hipot / Impulse) loads specialized pages:
- `LoadInducedPage()` constructs per-test parameter & recorded list views and sets required timer labels.
- `LoadHipotPage()` configures mode (primary/secondary/simultaneous/4LVB) and populates two list views.
- `LoadImpulsePage()` sets tab caption (H1/H2/H3/X1/X2), resets connection state, and populates impulse lists.

Each uses helper functions (`LoadInducedTestParam`, `LoadHipotRecordedData`, etc.) to maintain a consistent highlight and status coloring scheme.

---
## 6. Status Lifecycles
- Parsing: All Required tests seeded with `TestStatus.StatusType.Required` (download-time).
- Execution updates: Recording or aborting tests mutates the corresponding status object in the per-test collection element.
- Display refresh: After mutation, the appropriate `Load*RecordedData` routine re-renders the affected row(s) immediately.

---
## 7. Upload Serialization
`UnitData.BuildPacket` assembles positional ASCII records combining:
- Header: Machine ID, transaction code (phase variant), timestamp, serial, test number, catalog, KVA, ratings.
- Status Flags: Impulse (H1..X2), Hipot (Primary/Secondary/& 4LVB), Induced (First/Second).
- Measurements: Voltages, Currents, Powers, Durations.
- Tag status summary (P/F) derived from any Failed/Aborted flags.
- Operator ID suffix.

These packets are written per test by `Upload()` → `WriteUploadFiles()` (`*.nak` queue files) after pass/fail reconciliation (with optional aborted→failed conversion).

---
## 8. End-to-End Sequence Summary
1. Operator requests download (Data Entry F1 or external trigger).
2. Prior data decision (upload / partial / cancel).
3. `CurrentUnit` re-created; serial set.
4. Web service call enriches customer/work order (optional failure fallback).
5. SQL `Xref` resolves catalog/work order; regulator type check.
6. SQL `Params` returns ordered rows; each row parsed into `UnitData` collections.
7. UI refresh on Data Review tab via `LoadTransformerTestingPage`.
8. Operator navigates test pages; executions update collection status objects.
9. After all required tests, `Upload` emits per-test packet files.

---
## 9. Improvement Opportunities (Not Yet Applied)
- Return `True` on successful download.
- Parameterize SQL to avoid injection risk.
- Extract mapping constants (column indices) into named constants or DTO.
- Add defensive checks for substring operations on catalog numbers.
- Introduce structured DTO from data layer to reduce raw `SqlDataReader` coupling.

---
## 10. Quick Reference of Core Functions
| Purpose | Function | File |
|---------|----------|------|
| Orchestrate download | `Download` | `HostCommunication.vb` |
| Customer/work order lookup | `RequestDataFromWebService` | `HostCommunication.vb` |
| Row → model mapping | `ParseTransformerParams` | `UnitData.vb` |
| UI initial refresh | `LoadTransformerTestingPage` | `TestTabsLoad.vb` |
| Populate lists | `LoadTestingRecordedData` | `TestTabsLoad.vb` |
| Data entry bind | `LoadDataEntryFields` | `TestTabsLoad.vb` |
| Serialize for upload | `BuildPacket` | `UnitData.vb` |

---
## 11. Minimal Data Flow Diagram
```
Serial Number → Download()
  → RequestDataFromWebService() → CurrentUnit.CustomerName/WorkOrder
  → SQL Xref → Catalog + WorkOrder
  → SQL Params (rows) → ParseTransformerParams() → Collections (Ratings/Hipot/Induced/Impulse)
  → LoadTransformerTestingPage() → ListViews (DataReview)
  → (Operator executes tests) → Status Mutations → Load*RecordedData()
  → Upload() → BuildPacket() → *.nak files
```

---
End of Document.
