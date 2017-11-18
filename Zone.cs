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

        public delegate void SetpointFeedback(ushort SetPtAna, SimplSharpString SetPtStr);
        public SetpointFeedback SetpointFeedbackEvent { get; set; }

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
        // FUNCTION: SetUID
        // Description: ...
        //-------------------------------------//

        public void Configure(string _name, string _uid) {

            Name = _name;
            UID  = _uid;

        }

        //-------------------------------------//
        // FUNCTION: Update
        // Description: Called by Core after data for this zone has been
        //              read from the controller
        //-------------------------------------//

        public void Update () {

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

        }

        //-------------------------------------//
        // FUNCTION: SetOnOff
        // Description: ...
        //-------------------------------------//

        public void SetOnOff(ushort _state) {

            Core.QueueCommand(String.Format("{0} {1}", _state == 1 ? "on" : "off", UID));

        }

        public void SetSysMode(string _state) {

            Core.QueueCommand(String.Format("{0} {1}", _state, UID));

        }

        public void SetFanSpeed(string _state) {

            Core.QueueCommand(String.Format("fspeed {0} {1}", UID, _state));

        }

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
            SetpointRaw = Setpoint.ToString();

            SetpointFeedbackEvent((ushort)(Setpoint * 10), SetpointRaw); 

        }

        public void SetpointDirect(ushort _val) {
            Setpoint = ((float)_val)/10;
            SetpointRaw = Setpoint.ToString();
            SetpointFeedbackEvent((ushort)(Setpoint * 10), SetpointRaw);
            Core.QueueCommand(String.Format("temp {0} {1}", UID, Setpoint));
        }

        //===================// Event Handlers //===================//

        internal void SetpointLockoutExpired(object o) {

            // Release setpoint lock
            lockSetpoint = false;

            // Send new setpoint value
            Core.QueueCommand(String.Format("temp {0} {1}", UID, Setpoint));

        }

    } // End Zone class

}