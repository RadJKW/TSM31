# Template Layout


## Layout

### Template parts
- Web
- WinForm
- Core (Base UI ( Static Content ))
  - Menu Keys
  - Unit Information
  - Test Power
  - Status Bar
  - Operator Login/Logout
  -
- Shared
  - Reusable Services, etc
  - Operator Login
  - Set Condition Forms
  - Interfaces / Base Classes



### Per TestStation
- UI
- IO Objects
- Meter Objects

### Interfaces / base classes
- Meter
- IO
- Data Downlaod
- Logging



```cs
IMeterConnection IMeter
{
    void Connect();
    void Disconnect();
    MeterData ReadData();
}

```
