/*
**        File | Zone.cs 
**      Author | Ryan French
** Description | Zone class stores variables specific to a single HVAC zone, and facilitates communication
**               between SIMPL and the Core class via delegates.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace CoolMaster_NET_Controller {

    public class Zone {

        //===================// Members //===================//

        public string Name;

        public string UID;
        public string OnOff;
        public string CorF;

        public string SetpointRaw;
        public float  Setpoint;

        public string TempRaw;
        public float  Temp;

        public string FanSpeed;
        public string SystemMode;

        public bool Demand;

        public bool lockSetpoint;
        internal CTimer setpointLockout;

        public delegate void ZoneFeedback(ushort OnOff, 
                                          ushort SetPtAna,
                                          SimplSharpString SetPtStr, 
                                          ushort TempAna,
                                          SimplSharpString TempStr,
                                          SimplSharpString FanSpd,
                                          SimplSharpString SysMode,
                                          ushort demand);
        public ZoneFeedback ZoneFeedbackEvent { get; set; }

        public DelegateUshortUshort SetpointFeedbackEvent { get; set; }
        public DelegateUshortUshort TempFeedbackEvent { get; set; }

        public DelegateUshort OnOffFeedbackEvent { get; set; }
        public DelegateUshort DemandFeedbackEvent { get; set; }

        public DelegateString FanSpdFeedbackEvent { get; set; }
        public DelegateString SysModeFeedbackEvent { get; set; }

        //===================// Constructor //===================//

        public Zone() {

            Name = "";
            UID = "";
            OnOff = "";
            CorF = "";
            SetpointRaw = "";
            TempRaw = "";
            FanSpeed = "";
            SystemMode = "";

            Setpoint = 0f;
            Temp = 0f;

            Demand = false;

            lockSetpoint = false;

        }

        //===================// Methods //===================//

        //-------------------------------------//
        //    Function | Configure
        // Description | Called by Zone S+ symbol to populate Name and UID variables.
        //-------------------------------------//

        public void Configure(string _name, string _uid) {

            Name = _name;
            UID  = _uid;

        }

        //-------------------------------------//
        //    Function | Update (DEPRECATED)
        // Description | Called by Core after data for this zone has been
        //               read from the controller
        //-------------------------------------//

        /*public void Update () {

            // Send values to S+
            ZoneFeedbackEvent(
                (ushort)(OnOff == "ON" ? 1 : 0),
                (ushort)(Setpoint * 10),
                SetpointRaw,
                (ushort)(Temp * 10),
                TempRaw,
                FanSpeed,
                SystemMode,
                (ushort)(Demand ? 1 : 0)
            );

        }*/

        //-------------------------------------//
        //    Function | UpdateOnOff
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateOnOff(string _state) {

            if (_state != OnOff) {
                OnOff = _state;
                OnOffFeedbackEvent((ushort)(OnOff == "ON" ? 1 : 0));
            }

        }

        //-------------------------------------//
        //    Function | UpdateFanSpeed
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateFanSpeed(string _state) {

            if (_state != FanSpeed) {
                FanSpeed = _state;
                FanSpdFeedbackEvent(_state);
            }

        }

        //-------------------------------------//
        //    Function | UpdateSystemMode
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateSystemMode(string _state) {

            if (_state != SystemMode) {
                SystemMode = _state;
                SysModeFeedbackEvent(_state);
            }

        }

        //-------------------------------------//
        //    Function | UpdateDemand
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateDemand(bool _state) {

            if (_state != Demand) {
                Demand = _state;
                DemandFeedbackEvent((ushort)(Demand ? 1 : 0));
            }

        }

        //-------------------------------------//
        //    Function | UpdateSetpoint
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateSetpoint(string _raw) {

            if (_raw != SetpointRaw) {
                SetpointRaw = _raw;
                Setpoint    = float.Parse(_raw);

                string[] sp = SetpointRaw.Split('.');
                SetpointFeedbackEvent(ushort.Parse(sp[0]), ushort.Parse(sp[1]));
            }

        }

        //-------------------------------------//
        //    Function | UpdateTemp
        // Description | Only send update to S+ if state has changed.
        //-------------------------------------//

        public void UpdateTemp(string _raw) {

            if (_raw != TempRaw) {
                TempRaw = _raw;
                Temp = float.Parse(_raw);

                string[] tmp =  TempRaw.Split('.');

                TempFeedbackEvent(ushort.Parse(tmp[0]), ushort.Parse(tmp[1]));
            }

        }

        //-------------------------------------//
        //    Function | SetOnOff
        // Description | Receive user input from S+
        //-------------------------------------//

        public void SetOnOff(ushort _state) {

            Core.QueueCommand(String.Format("{0} {1}", _state == 1 ? "on" : "off", UID));

        }

        //-------------------------------------//
        //    Function | SetSysMode
        // Description | Receive user input from S+
        //-------------------------------------//

        public void SetSysMode(string _state) {

            Core.QueueCommand(String.Format("{0} {1}", _state, UID));

        }

        //-------------------------------------//
        //    Function | SetFanSpeed
        // Description | Receive user input from S+
        //-------------------------------------//

        public void SetFanSpeed(string _state) {

            Core.QueueCommand(String.Format("fspeed {0} {1}", UID, _state));

        }

        //-------------------------------------//
        //    Function | SetpointUpDown
        // Description | Receive user input from S+
        //-------------------------------------//

        public void SetpointUpDown(ushort _dir) {

            // Ignore command if controller hasn't finished poll yet
            if (Setpoint == 0f) 
                return;

            // Set flag to lock out feedback from controller
            if (!lockSetpoint) {
                lockSetpoint = true;
                setpointLockout = new CTimer(SetpointLockoutExpired, 5000);
            } else {
                setpointLockout.Stop();
                setpointLockout.Reset(5000);
            }

            // Update local variable
            Setpoint += _dir == 1 ? 1.0f : -1.0f;
            SetpointRaw = getStringValue(Setpoint);

            string[] sp = SetpointRaw.Split('.');
            SetpointFeedbackEvent(ushort.Parse(sp[0]), ushort.Parse(sp[1]));

            // Send new setpoint value
            Core.QueueCommand(String.Format("temp {0} {1}", UID, Setpoint));

        }

        //-------------------------------------//
        //    Function | SetpointDirect
        // Description | Receive user input from S+
        //-------------------------------------//

        public void SetpointDirect(ushort _val) {

            Setpoint = ((float)_val)/10;
            SetpointRaw = getStringValue(Setpoint);

            string[] sp = SetpointRaw.Split('.');
            SetpointFeedbackEvent(ushort.Parse(sp[0]), ushort.Parse(sp[1]));
            Core.QueueCommand(String.Format("temp {0} {1}", UID, Setpoint));
        }

        //-------------------------------------//
        //    Function | getStringValue
        // Description | Converts float representation of setpoint to string with single decimal point (000.0)
        //-------------------------------------//

        internal static string getStringValue(float _val) {

            float correction = _val < 100 ? _val * 10 : _val;

            return String.Format("{0}.{1}", (int)correction / 10, correction % 10);

        }

        //===================// Event Handlers //===================//

        //-------------------------------------//
        //    Function | SetpointLockoutExpired
        // Description | Releases lock on accepting incoming setpoint value from remove server.
        //-------------------------------------//

        internal void SetpointLockoutExpired(object o) {

            lockSetpoint = false;

        }

    } // End Zone class

}