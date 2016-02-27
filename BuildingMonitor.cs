using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class BuildingMonitor : ThreadingExtensionBase
    {
        private BuildingPrefabMapping _mapping;

        private bool _initialized;
        private bool _terminated;
        private bool _paused;
        private int _lastProcessedFrame;
        
        private int _capacity;

        private Building _building;
        private ushort _id;
        private List<HashSet<ushort>> _categories;

        private HashSet<ushort> _added;
        private HashSet<ushort> _removed;

        public override void OnCreated(IThreading threading)
        {
            _initialized = false;
            _terminated = false;

            _added = new HashSet<ushort>();
            _removed = new HashSet<ushort>();

            base.OnCreated(threading);
        }

        /*
         * Handles creation of new buildings and reallocation of existing buildings.
         *
         * Note: This needs to happen before simulation TICK; otherwise, we might miss the
         * building update tracking. The building update record gets cleared whether the
         * simulation is paused or not.
         */
        public override void OnBeforeSimulationTick()
        {
            if (_terminated) return;

            if (!Helper.Instance.BuildingMonitorSpun)
            {
                _initialized = false;
                return;
            }

            if (!Settings.Instance.Enable._BuildingMonitor) return;

            if (!_initialized) return;

            if (!Singleton<BuildingManager>.instance.m_buildingsUpdated) return;

            for (int i = 0; i < Singleton<BuildingManager>.instance.m_updatedBuildings.Length; i++)
            {
                ulong ub = Singleton<BuildingManager>.instance.m_updatedBuildings[i];

                if (ub != 0)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        if ((ub & (ulong)1 << j) != 0)
                        {
                            ushort id = (ushort)(i << 6 | j);

                            if (ProcessBuilding(id))
                                _added.Add(id);
                            else
                                _removed.Add(id);
                        }
                    }
                }
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
         * Handles removal of buildings and status changes
         *
         * Note: Just because a building has been removed visually, it does not mean
         * it is removed as far as the game is concerned. The building is only truly removed
         * when the frame covers the building's id, and that's when we will remove the
         * building from our records.
         */
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            if (!Helper.Instance.BuildingMonitorSpinnable) return;

            if (!Settings.Instance.Enable._BuildingMonitor) return;

            try
            {
                if (!_initialized)
                {
                    _mapping = new BuildingPrefabMapping();

                    _paused = false;
                    
                    _capacity = Singleton<BuildingManager>.instance.m_buildings.m_buffer.Length;

                    _id = (ushort)_capacity;

                    _added.Clear();
                    _removed.Clear();

                    for (ushort i = 0; i < _capacity; i++)
                    {
                        if (ProcessBuilding(i))
                            UpdateBuilding();
                    }

                    _lastProcessedFrame = GetFrame();

                    _initialized = true;
                    Helper.Instance.BuildingMonitorSpun = true;
                    Helper.Instance.BuildingMonitor = this;

                    Helper.Instance.NotifyPlayer("Building monitor initialized");
                }
                else if (!SimulationManager.instance.SimulationPaused)
                {
                    Data.Instance._BuildingsAdded.Clear();
                    Data.Instance._BuildingsUpdated.Clear();
                    Data.Instance._BuildingsRemoved.Clear();

                    foreach (ushort i in _added)
                        Data.Instance._BuildingsAdded.Add(i);

                    _added.Clear();

                    foreach (ushort i in _removed)
                        Data.Instance._BuildingsRemoved.Add(i);

                    _removed.Clear();

                    int end = GetFrame();

                    while (_lastProcessedFrame != end)
                    {
                        _lastProcessedFrame = GetFrame(_lastProcessedFrame + 1);

                        int[] boundaries = GetFrameBoundaries(_lastProcessedFrame);
                        ushort id;

                        for (int i = boundaries[0]; i <= boundaries[1]; i++)
                        {
                            id = (ushort)i;

                            if (UpdateBuilding(id))
                                Data.Instance._BuildingsUpdated.Add(id);
                            else if (Data.Instance._Buildings.Contains(id))
                            {
                                Data.Instance._BuildingsRemoved.Add(id);
                                RemoveBuilding(id);
                            }
                        }
                    }
                }

                OutputDebugLog();
            }
            catch (Exception e)
            {
                string error = "Building monitor failed to initialize\r\n";
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

            Helper.Instance.BuildingMonitorSpun = false;
            Helper.Instance.BuildingMonitor = null;

            if (Data.Instance != null)
            {
                Data.Instance._Buildings.Clear();

                Data.Instance._PlayerBuildings.Clear();
                Data.Instance._Cemeteries.Clear();
                Data.Instance._LandfillSites.Clear();
                Data.Instance._FireStations.Clear();
                Data.Instance._PoliceStations.Clear();
                Data.Instance._Hospitals.Clear();
                Data.Instance._Parks.Clear();
                Data.Instance._PowerPlants.Clear();
                Data.Instance._PlayerOther.Clear();

                Data.Instance._PrivateBuildings.Clear();
                Data.Instance._ResidentialBuildings.Clear();
                Data.Instance._CommercialBuildings.Clear();
                Data.Instance._IndustrialBuildings.Clear();
                Data.Instance._OfficeBuildings.Clear();
                Data.Instance._PrivateOther.Clear();

                Data.Instance._BuildingOther.Clear();

                Data.Instance._BuildingsAbandoned.Clear();
                Data.Instance._BuildingsBurnedDown.Clear();

                Data.Instance._BuildingsWithFire.Clear();
                Data.Instance._BuildingsWithCrime.Clear();
                Data.Instance._BuildingsWithSick.Clear();
                Data.Instance._BuildingsWithDead.Clear();
                Data.Instance._BuildingsWithGarbage.Clear();

                Data.Instance._BuildingsCapacityFull.Clear();
                Data.Instance._BuildingsCapacityStep1.Clear();
                Data.Instance._BuildingsCapacityStep2.Clear();
            }

            base.OnReleased();
        }

        public int GetFrameFromId(ushort id)
        {
            return id >> 7 & 255;
        }

        public int GetFrame()
        {
            return GetFrame((int)Singleton<SimulationManager>.instance.m_currentFrameIndex);
        }

        private int GetFrame(int index)
        {
            return (int)(index & 255);
        }

        private int[] GetFrameBoundaries()
        {
            return GetFrameBoundaries((int)Singleton<SimulationManager>.instance.m_currentFrameIndex);
        }

        private int[] GetFrameBoundaries(int index)
        {
            int frame = (int)(index & 255);
            int frame_first = frame * 192;
            int frame_last = (frame + 1) * 192 - 1;

            return new int[2] { frame_first, frame_last };
        }

        private bool GetBuilding()
        {
            _building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)_id];

            if (_building.Info == null)
                return false;

            if ((_building.m_flags & Building.Flags.Created) == Building.Flags.None)
                return false;

            return true;
        }

        private bool ProcessBuilding(ushort id)
        {
            if (Data.Instance._Buildings.Contains(id))
                RemoveBuilding(id);

            _id = id;

            if (!GetBuilding())
                return false;

            _categories = _mapping.GetMapping(_building.Info);

            if (_categories.Count == 0)
                return false;

            foreach (HashSet<ushort> category in _categories)
                category.Add(_id);

            return true;
        }

        private bool UpdateBuilding(ushort id)
        {
            _id = id;

            if (!GetBuilding())
                return false;

            return UpdateBuilding();
        }

        private bool UpdateBuilding()
        {
            if (CheckAbandoned())
            {
                Data.Instance._BuildingsWithDead.Remove(_id);
                Data.Instance._BuildingsWithGarbage.Remove(_id);
                Data.Instance._BuildingsWithFire.Remove(_id);
                Data.Instance._BuildingsWithCrime.Remove(_id);
                Data.Instance._BuildingsWithSick.Remove(_id);

                Data.Instance._BuildingsCapacityStep1.Remove(_id);
                Data.Instance._BuildingsCapacityStep2.Remove(_id);
                Data.Instance._BuildingsCapacityFull.Remove(_id);
            }
            else if (CheckBurnedDown())
            {
                Data.Instance._BuildingsWithDead.Remove(_id);
                Data.Instance._BuildingsWithGarbage.Remove(_id);
                Data.Instance._BuildingsWithFire.Remove(_id);
                Data.Instance._BuildingsWithCrime.Remove(_id);
                Data.Instance._BuildingsWithSick.Remove(_id);
                Data.Instance._BuildingsAbandoned.Remove(_id);

                Data.Instance._BuildingsCapacityStep1.Remove(_id);
                Data.Instance._BuildingsCapacityStep2.Remove(_id);
                Data.Instance._BuildingsCapacityFull.Remove(_id);
            }
            else
            {
                CheckDead();
                CheckGarbage();
                CheckFire();
                CheckCrime();
                CheckSick();

                CheckCapacityStep1();
                CheckCapacityStep2();
                CheckCapacityFull();
            }

            return true;
        }

        internal void RequestRemoval(ushort id)
        {
            _id = id;

            if (!GetBuilding())
                RemoveBuilding(id);
        }

        private void RemoveBuilding(ushort id)
        {
            Data.Instance._Buildings.Remove(id);

            Data.Instance._PlayerBuildings.Remove(id);
            Data.Instance._Cemeteries.Remove(id);
            Data.Instance._LandfillSites.Remove(id);
            Data.Instance._FireStations.Remove(id);
            Data.Instance._PoliceStations.Remove(id);
            Data.Instance._Hospitals.Remove(id);
            Data.Instance._Parks.Remove(id);
            Data.Instance._PowerPlants.Remove(id);
            Data.Instance._PlayerOther.Remove(id);

            Data.Instance._PrivateBuildings.Remove(id);
            Data.Instance._ResidentialBuildings.Remove(id);
            Data.Instance._CommercialBuildings.Remove(id);
            Data.Instance._IndustrialBuildings.Remove(id);
            Data.Instance._OfficeBuildings.Remove(id);
            Data.Instance._PrivateOther.Remove(id);

            Data.Instance._BuildingOther.Remove(id);

            Data.Instance._BuildingsAbandoned.Remove(id);
            Data.Instance._BuildingsBurnedDown.Remove(id);

            Data.Instance._BuildingsWithDead.Remove(id);
            Data.Instance._BuildingsWithGarbage.Remove(id);
            Data.Instance._BuildingsWithFire.Remove(id);
            Data.Instance._BuildingsWithCrime.Remove(id);
            Data.Instance._BuildingsWithSick.Remove(id);

            Data.Instance._BuildingsCapacityStep1.Remove(id);
            Data.Instance._BuildingsCapacityStep2.Remove(id);
            Data.Instance._BuildingsCapacityFull.Remove(id);
        }

        private bool Check(Building.Flags problems, HashSet<ushort> category)
        {
            if ((_building.m_flags & problems) != Building.Flags.None)
            {
                category.Add(_id);
                return true;
            }
            else
            {
                category.Remove(_id);
                return false;
            }
        }

        private bool Check(Notification.Problem problems, HashSet<ushort> category)
        {
            if ((_building.m_problems & problems) != Notification.Problem.None)
            {
                category.Add(_id);
                return true;
            }
            else
            {
                category.Remove(_id);
                return false;
            }
        }

        private bool CheckDead()
        {
            if (_building.m_deathProblemTimer > 0)
            {
                Data.Instance._BuildingsWithDead.Add(_id);
                return true;
            }
            else
            {
                Data.Instance._BuildingsWithDead.Remove(_id);
                return false;
            }
        }

        private bool CheckAbandoned()
        {
            return Check(Building.Flags.Abandoned, Data.Instance._BuildingsAbandoned);
        }

        private bool CheckBurnedDown()
        {
            return Check(Building.Flags.BurnedDown, Data.Instance._BuildingsBurnedDown);
        }

        private bool CheckGarbage()
        {
            if (_building.Info.m_buildingAI.GetGarbageAmount(_id, ref _building) > 2500 && !(_building.Info.m_buildingAI is LandfillSiteAI))
            {
                Data.Instance._BuildingsWithGarbage.Add(_id);
                return true;
            }
            else
            {
                Data.Instance._BuildingsWithGarbage.Remove(_id);
                return false;
            }
        }

        private bool CheckFire()
        {
            return Check(Notification.Problem.Fire, Data.Instance._BuildingsWithFire);
        }

        private bool CheckCrime()
        {
            return Check(Notification.Problem.Crime, Data.Instance._BuildingsWithCrime);
        }

        private bool CheckSick()
        {
            return Check(Notification.Problem.DirtyWater | Notification.Problem.Pollution | Notification.Problem.Noise, Data.Instance._BuildingsWithSick);
        }

        private bool CheckCapacityStep1()
        {
            return Check(Building.Flags.CapacityStep1, Data.Instance._BuildingsCapacityStep1);
        }

        private bool CheckCapacityStep2()
        {
            return Check(Building.Flags.CapacityStep2, Data.Instance._BuildingsCapacityStep2);
        }

        private bool CheckCapacityFull()
        {
            return Check(Building.Flags.CapacityFull, Data.Instance._BuildingsCapacityFull);
        }

        private void OutputDebugLog()
        {
            if (!Helper.Instance.BuildingMonitorSpun) return;

            if (!Settings.Instance.Debug._BuildingMonitor) return;

            if (!Settings.Instance.Enable._BuildingMonitor) return;

            if (!_initialized) return;

            if (!SimulationManager.instance.SimulationPaused) return;

            if (_paused) return;

            string log = "\r\n";
            log += "==== BUILDINGS ====\r\n";
            log += "\r\n";
            log += String.Format("{0}   Total\r\n", Data.Instance._Buildings.Count);
            log += String.Format("{0}   Added\r\n", Data.Instance._BuildingsAdded.Count);
            log += String.Format("{0}   Updated\r\n", Data.Instance._BuildingsUpdated.Count);
            log += String.Format("{0}   Removed\r\n", Data.Instance._BuildingsRemoved.Count);
            log += "\r\n";
            log += String.Format("{0}   Player Building(s)\r\n", Data.Instance._PlayerBuildings.Count);
            log += String.Format(" =>   {0}   Cemetery(s)\r\n", Data.Instance._Cemeteries.Count);
            log += String.Format(" =>   {0}   LandfillSite(s)\r\n", Data.Instance._LandfillSites.Count);
            log += String.Format(" =>   {0}   FireStation(s)\r\n", Data.Instance._FireStations.Count);
            log += String.Format(" =>   {0}   PoliceStation(s)\r\n", Data.Instance._PoliceStations.Count);
            log += String.Format(" =>   {0}   Hospital(s)\r\n", Data.Instance._Hospitals.Count);
            log += String.Format(" =>   {0}   Park(s)\r\n", Data.Instance._Parks.Count);
            log += String.Format(" =>   {0}   PowerPlant(s)\r\n", Data.Instance._PowerPlants.Count);
            log += String.Format(" =>   {0}   Other\r\n", Data.Instance._PlayerOther.Count);
            log += "\r\n";
            log += String.Format("{0}   Private Building(s)\r\n", Data.Instance._PrivateBuildings.Count);
            log += String.Format(" =>   {0}   Residential\r\n", Data.Instance._ResidentialBuildings.Count);
            log += String.Format(" =>   {0}   Commercial\r\n", Data.Instance._CommercialBuildings.Count);
            log += String.Format(" =>   {0}   Industrial\r\n", Data.Instance._IndustrialBuildings.Count);
            log += String.Format(" =>   {0}   Office(s)\r\n", Data.Instance._OfficeBuildings.Count);
            log += String.Format(" =>   {0}   Other\r\n", Data.Instance._PrivateOther.Count);
            log += "\r\n";
            log += String.Format("{0}   Other Building(s)\r\n", Data.Instance._BuildingOther.Count);
            log += "\r\n";
            log += String.Format("{0}   Abandoned\r\n", Data.Instance._BuildingsAbandoned.Count);
            log += String.Format("{0}   BurnedDown\r\n", Data.Instance._BuildingsBurnedDown.Count);
            log += "\r\n";
            log += String.Format("{0}   w/Death\r\n", Data.Instance._BuildingsWithDead.Count);
            log += String.Format("{0}   w/Garbage\r\n", Data.Instance._BuildingsWithGarbage.Count);
            log += String.Format("{0}   w/Fire\r\n", Data.Instance._BuildingsWithFire.Count);
            log += String.Format("{0}   w/Crime\r\n", Data.Instance._BuildingsWithCrime.Count);
            log += String.Format("{0}   w/Illness\r\n", Data.Instance._BuildingsWithSick.Count);
            log += "\r\n";
            log += String.Format("{0}   CapacityStep1\r\n", Data.Instance._BuildingsCapacityStep1.Count);
            log += String.Format("{0}   CapacityStep2\r\n", Data.Instance._BuildingsCapacityStep2.Count);
            log += String.Format("{0}   CapacityFull\r\n", Data.Instance._BuildingsCapacityFull.Count);
            log += "\r\n";

            Helper.Instance.Log(log);

            _paused = true;
        }
    }
}