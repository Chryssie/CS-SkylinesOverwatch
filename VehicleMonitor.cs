using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class VehicleMonitor : ThreadingExtensionBase
    {
        private VehiclePrefabMapping _mapping;

        private bool _initialized;
        private bool _terminated;
        private bool _paused;
        private int _lastProcessedFrame;
        
        private int _capacity;

        private Vehicle _vehicle;
        private ushort _id;
        private List<HashSet<ushort>> _categories;

        public override void OnCreated(IThreading threading)
        {
            _initialized = false;
            _terminated = false;

            base.OnCreated(threading);
        }

        public override void OnBeforeSimulationTick()
        {
            if (_terminated) return;

            if (!Helper.Instance.VehicleMonitorSpun)
            {
                _initialized = false;
                return;
            }

            base.OnBeforeSimulationTick();
        }

        public override void OnBeforeSimulationFrame()
        {
            base.OnBeforeSimulationFrame();
        }

        public override void OnAfterSimulationFrame()
        {
            _paused = false;

            base.OnAfterSimulationFrame();
        }

        public override void OnAfterSimulationTick()
        {
            base.OnAfterSimulationTick();
        }

        /*
         * Handles creation and removal of vehicles
         *
         * Note: Just because a vehicle has been removed visually, it does not mean
         * it is removed as far as the game is concerned. The vehicle is only truly removed
         * when the frame covers the vehicle's id, and that's when we will remove the
         * vehicle from our records.
         */
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            if (!Helper.Instance.VehicleMonitorSpinnable) return;

            if (!Settings.Instance.Enable._VehicleMonitor) return;

            try
            {
                if (!_initialized)
                {
                    _mapping = new VehiclePrefabMapping();

                    _paused = false;
                    
                    _capacity = Singleton<VehicleManager>.instance.m_vehicles.m_buffer.Length;

                    _id = (ushort)_capacity;

                    _initialized = true;
                    Helper.Instance.VehicleMonitorSpun = true;
                    Helper.Instance.VehicleMonitor = this;

                    Helper.Instance.NotifyPlayer("Vehicle monitor initialized");
                }
                else if (!SimulationManager.instance.SimulationPaused)
                {
                    Data.Instance._VehiclesUpdated.Clear();
                    Data.Instance._VehiclesRemoved.Clear();

                    int end = GetFrame();

                    while (_lastProcessedFrame != end)
                    {
                        _lastProcessedFrame = GetFrame(_lastProcessedFrame + 1);

                        int[] boundaries = GetFrameBoundaries();
                        ushort id;

                        for (int i = boundaries[0]; i <= boundaries[1]; i++)
                        {
                            id = (ushort)i;

                            if (UpdateVehicle(id))
                                Data.Instance._VehiclesUpdated.Add(id);
                            else if (Data.Instance._Vehicles.Contains(id))
                            {
                                Data.Instance._VehiclesRemoved.Add(id);
                                RemoveVehicle(id);
                            }
                        }
                    }
                }

                OutputDebugLog();
            }
            catch (Exception e)
            {
                string error = "Vehicle monitor failed to initialize\r\n";
                error += String.Format("Error: {0}\r\n", e.Message);
                error += "\r\n";
                error += "==== STACK TRACE ====\r\n";
                error += e.StackTrace;

                Helper.Instance.Log(error);

                _terminated = true;
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        public override void OnReleased()
        {
            _initialized = false;
            _terminated = false;
            _paused = false;

            Helper.Instance.VehicleMonitorSpun = false;
            Helper.Instance.VehicleMonitor = null;

            if (Data.Instance != null)
            {
                Data.Instance._Vehicles.Clear();

                Data.Instance._Cars.Clear();
                Data.Instance._Trains.Clear();
                Data.Instance._Aircraft.Clear();
                Data.Instance._Ships.Clear();
                Data.Instance._VehicleOther.Clear();

                Data.Instance._Hearses.Clear();
                Data.Instance._GarbageTrucks.Clear();
                Data.Instance._FireTrucks.Clear();
                Data.Instance._PoliceCars.Clear();
                Data.Instance._Ambulances.Clear();
                Data.Instance._Buses.Clear();
                Data.Instance._CarOther.Clear();
            }

            base.OnReleased();
        }

        public int GetFrameFromId(ushort id)
        {
            return id >> 6 & 15;
        }

        private int GetFrame()
        {
            return GetFrame((int)Singleton<SimulationManager>.instance.m_currentFrameIndex);
        }

        private int GetFrame(int index)
        {
            return (int)(index & 15);
        }

        public static int[] GetFrameBoundaries()
        {
            return GetFrameBoundaries((int)Singleton<SimulationManager>.instance.m_currentFrameIndex);
        }

        private static int[] GetFrameBoundaries(int index)
        {
            int frame = (int)(index & 15);
            int frame_first = frame * 1024;
            int frame_last = (frame + 1) * 1024 - 1;

            return new int[2] { frame_first, frame_last };
        }

        private bool GetVehicle()
        {
            _vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int)_id];

            if (_vehicle.m_leadingVehicle != 0)
                return false;

            if (_vehicle.m_cargoParent != 0)
                return false;
            
            if (_vehicle.Info == null)
                return false;

            if ((_vehicle.m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.Spawned)
                return false;

            return true;
        }

        private bool UpdateVehicle(ushort id)
        {
            _id = id;

            if (!GetVehicle())
                return false;

            _categories = _mapping.GetMapping(_vehicle.Info);

            if (_categories.Count == 0)
                return false;

            foreach (HashSet<ushort> category in _categories)
                category.Add(_id);

            return true;
        }

        internal void RequestRemoval(ushort id)
        {
            _id = id;

            if (!GetVehicle())
                RemoveVehicle(id);
        }

        private void RemoveVehicle(ushort id)
        {
            Data.Instance._Vehicles.Remove(id);

            Data.Instance._Cars.Remove(id);
            Data.Instance._Trains.Remove(id);
            Data.Instance._Aircraft.Remove(id);
            Data.Instance._Ships.Remove(id);
            Data.Instance._VehicleOther.Remove(id);

            Data.Instance._Hearses.Remove(id);
            Data.Instance._GarbageTrucks.Remove(id);
            Data.Instance._FireTrucks.Remove(id);
            Data.Instance._PoliceCars.Remove(id);
            Data.Instance._Ambulances.Remove(id);
            Data.Instance._Buses.Remove(id);
            Data.Instance._CarOther.Remove(id);
        }

        private void OutputDebugLog()
        {
            if (!Helper.Instance.VehicleMonitorSpun) return;

            if (!Settings.Instance.Debug._VehicleMonitor) return;

            if (!Settings.Instance.Enable._VehicleMonitor) return;

            if (!_initialized) return;

            if (!SimulationManager.instance.SimulationPaused) return;

            if (_paused) return;

            string log = "\r\n";
            log += "==== VEHICLES ====\r\n";
            log += "\r\n";
            log += String.Format("{0}   Total\r\n", Data.Instance._Vehicles.Count);
            log += String.Format("{0}   Updated\r\n", Data.Instance._VehiclesUpdated.Count);
            log += String.Format("{0}   Removed\r\n", Data.Instance._VehiclesRemoved.Count);
            log += "\r\n";
            log += String.Format("{0}   Car(s)\r\n", Data.Instance._Cars.Count);
            log += String.Format(" =>   {0}   Hearse(s)\r\n", Data.Instance._Hearses.Count);
            log += String.Format(" =>   {0}   Garbage Truck(s)\r\n", Data.Instance._GarbageTrucks.Count);
            log += String.Format(" =>   {0}   Fire Truck(s)\r\n", Data.Instance._FireTrucks.Count);
            log += String.Format(" =>   {0}   Police Car(s)\r\n", Data.Instance._PoliceCars.Count);
            log += String.Format(" =>   {0}   Ambulance(s)\r\n", Data.Instance._Ambulances.Count);
            log += String.Format(" =>   {0}   Bus(s)\r\n", Data.Instance._Buses.Count);
            log += String.Format(" =>   {0}   Other\r\n", Data.Instance._CarOther.Count);
            log += "\r\n";
            log += String.Format("{0}   Train(s)\r\n", Data.Instance._Trains.Count);
            log += String.Format(" =>   {0}   Passenger Train(s)\r\n", Data.Instance._PassengerTrains.Count);
            log += String.Format(" =>   {0}   Metro Train(s)\r\n", Data.Instance._MetroTrains.Count);
            log += String.Format(" =>   {0}   Cargo Train(s)\r\n", Data.Instance._CargoTrains.Count);
            log += String.Format(" =>   {0}   Other\r\n", Data.Instance._TrainOther.Count);
            log += "\r\n";
            log += String.Format("{0}   Aircraft\r\n", Data.Instance._Aircraft.Count);
            log += String.Format("{0}   Ship(s)\r\n", Data.Instance._Ships.Count);
            log += String.Format("{0}   Other\r\n", Data.Instance._VehicleOther.Count);
            log += "\r\n";

            Helper.Instance.Log(log);

            _paused = true;
        }
    }
}