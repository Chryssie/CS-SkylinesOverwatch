﻿using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "Skylines Overwatch [Fixed for v1.6]";

            Animals = new HashSet<string>();
            Animals.Add("Seagull");
            Animals.Add("Cow");
            Animals.Add("Pig");
            Animals.Add("Dog");
            Animals.Add("Wolf");
            Animals.Add("Bear");
            Animals.Add("MooseMale");
            Animals.Add("MooseFemale");

            Debug._BuildingMonitor          = false;
            Debug._VehicleMonitor           = false;
            Debug._HumanMonitor             = false;
            Debug._AnimalMonitor            = false;

            Enable._BuildingMonitor         = false;
            Enable._VehicleMonitor          = false;
            Enable._HumanMonitor            = false;
            Enable._AnimalMonitor           = false;
        }

        private static readonly Settings _Instance = new Settings();
        public static Settings Instance { get { return _Instance; } }

        public readonly string Tag;
        public readonly HashSet<string> Animals;

        public DebugSettings Debug;
        public EnableSettings Enable;

        public struct DebugSettings
        {
            internal bool _BuildingMonitor;
            internal bool _VehicleMonitor;
            internal bool _HumanMonitor;
            internal bool _AnimalMonitor;

            public bool BuildingMonitor
            {
                get { return _BuildingMonitor; }
                set { if (value) _BuildingMonitor = true; }
            }

            public bool VehicleMonitor
            {
                get { return _VehicleMonitor; }
                set { if (value) _VehicleMonitor = true; }
            }

            public bool HumanMonitor
            {
                get { return _HumanMonitor; }
                set { if (value) _HumanMonitor = true; }
            }

            public bool AnimalMonitor
            {
                get { return _AnimalMonitor; }
                set
                {
                    if (value)
                    {
                        _BuildingMonitor = true;
                        _AnimalMonitor = true;
                    }
                }
            }
        }

        public struct EnableSettings
        {
            internal bool _BuildingMonitor;
            internal bool _VehicleMonitor;
            internal bool _HumanMonitor;
            internal bool _AnimalMonitor;

            public bool BuildingMonitor
            {
                get { return _BuildingMonitor; }
                set { if (value) _BuildingMonitor = true; }
            }

            public bool VehicleMonitor
            {
                get { return _VehicleMonitor; }
                set { if (value) _VehicleMonitor = true; }
            }

            public bool HumanMonitor
            {
                get { return _HumanMonitor; }
                set { if (value) _HumanMonitor = true; }
            }

            public bool AnimalMonitor
            {
                get { return _AnimalMonitor; }
                set
                {
                    if (value)
                    {
                        _BuildingMonitor = true;
                        _AnimalMonitor = true;
                    }
                }
            }
        }
    }
}
