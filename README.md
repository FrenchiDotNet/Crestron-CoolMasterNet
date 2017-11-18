# Crestron - CoolMaster Net Control
This Simpl#/Simpl+ library allows for IP control of a CoolMaster Net HVAC controller from a Crestron processor.

### Files

| File/Folder | Description |
| ----------- | ----------- |
| **/bin/** | Contains the compiled .clz library for inclusion in the SIMPL+ modules. |
| **/simpl_plus/** | Contains SIMPL+ modules to link S# library to SIMPL. |
| **/simpl_plus/CoolMaster NET Core.usp** | S+ - Master module, exactly one instance required in project |
| **/simpl_plus/CoolMaster NET Zone.usp** | S+ - Zone module, add one per controlled zone |
| **/test_server/** | Contains a Python module to mimic two zones on a CoolMaster Net controller (not required) |
| **/test_server/CoolMasterTestServ.py** | *See above* |
| **/Core.cs** | S# - Static class that handles communication with hardware. Interfaces with CoolMaster NET Core.usp |
| **/Zone.cs** | S# - Variables and methods for control of a single zone. Interfaces with CoolMaster NET Zone.usp |