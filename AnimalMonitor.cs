using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class AnimalMonitor : ThreadingExtensionBase
    {
        private AnimalPrefabMapping _mapping;

        private Dictionary<ushort, HashSet<ushort>> _buildingsAnimals;

        private bool _initialized;
        private bool _terminated;
        private bool _paused;
        
        private int _capacity;

        private CitizenInstance _animal;
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

            if (!Helper.Instance.AnimalMonitorSpun)
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
         * Handles creation and removal of animals
         *
         * Note: Just because a animal has been removed visually, it does not mean
         * it is removed as far as the game is concerned. The animal is only truly removed
         * when the frame covers the animal's id, and that's when we will remove the
         * animal from our records.
         */
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            if (!Helper.Instance.AnimalMonitorSpinnable) return;

            if (!Settings.Instance.Enable._AnimalMonitor) return;

            try
            {
                if (!_initialized)
                {
                    _mapping = new AnimalPrefabMapping();

                    _buildingsAnimals = new Dictionary<ushort, HashSet<ushort>>();

                    _paused = false;
                    
                    _capacity = Singleton<CitizenManager>.instance.m_instances.m_buffer.Length;

                    _id = (ushort)_capacity;

                    for (int i = 0; i < _capacity; i++)
                        UpdateAnimal((ushort)i);

                    _initialized = true;
                    Helper.Instance.AnimalMonitorSpun = true;
                    Helper.Instance.AnimalMonitor = this;

                    Helper.Instance.NotifyPlayer("Animal monitor initialized");
                }
                else if (Data.Instance.BuildingsUpdated.Length > 0)
                {
                    Data.Instance._AnimalsUpdated.Clear();
                    Data.Instance._AnimalsRemoved.Clear();

                    foreach (ushort building in Data.Instance._BuildingsUpdated)
                    {
                        ushort id = Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_targetCitizens;

                        while (id != 0)
                        {
                            if (UpdateAnimal(id))
                            {
                                Data.Instance._AnimalsUpdated.Add(id);
                                AddBuildingsAnimal(building, id);
                            }
                            else
                            {
                                Data.Instance._AnimalsRemoved.Add(id);

                                if (Data.Instance._Animals.Contains(id))
                                    RemoveAnimal(id);
                            }

                            id = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)id].m_nextTargetInstance;
                        }

                        CheckBuildingsAnimals(building);
                    }

                    foreach (ushort building in Data.Instance._BuildingsRemoved)
                        RemoveBuildingsAnimals(building);
                }

                OutputDebugLog();
            }
            catch (Exception e)
            {
                string error = "Animal monitor failed to initialize\r\n";
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
            _buildingsAnimals = new Dictionary<ushort, HashSet<ushort>>();

            _initialized = false;
            _terminated = false;
            _paused = false;

            Helper.Instance.AnimalMonitorSpun = false;
            Helper.Instance.AnimalMonitor = null;

            if (Data.Instance != null)
            {
                Data.Instance._Animals.Clear();

                Data.Instance._Birds.Clear();
                Data.Instance._Seagulls.Clear();

                Data.Instance._Livestock.Clear();
                Data.Instance._Cows.Clear();
                Data.Instance._Pigs.Clear();

                Data.Instance._Pets.Clear();
                Data.Instance._Dogs.Clear();

                Data.Instance._Wildlife.Clear();
                Data.Instance._Wolves.Clear();
                Data.Instance._Bears.Clear();
                Data.Instance._Moose.Clear();

                Data.Instance._AnimalOther.Clear();
            }

            base.OnReleased();
        }

        private void AddBuildingsAnimal(ushort building, ushort animal)
        {
            if (!_buildingsAnimals.ContainsKey(building))
                _buildingsAnimals.Add(building, new HashSet<ushort>());

            _buildingsAnimals[building].Add(animal);
        }

        private void CheckBuildingsAnimals(ushort building)
        {
            if (!_buildingsAnimals.ContainsKey(building))
                return;

            HashSet<ushort> animals = _buildingsAnimals[building];

            if (animals.Count == 0)
            {
                _buildingsAnimals.Remove(building);
                return;
            }

            List<ushort> removals = new List<ushort>();

            foreach (ushort animal in animals)
            {
                if (Data.Instance._AnimalsUpdated.Contains(animal) || Data.Instance._AnimalsRemoved.Contains(animal))
                    continue;

                removals.Add(animal);
            }

            foreach (ushort animal in removals)
            {
                animals.Remove(animal);

                Data.Instance._AnimalsRemoved.Add(animal);
                RemoveAnimal(animal);
            }

            if (animals.Count == 0)
                _buildingsAnimals.Remove(building);
        }

        private void RemoveBuildingsAnimals(ushort building)
        {
            if (!_buildingsAnimals.ContainsKey(building))
                return;

            HashSet<ushort> animals = _buildingsAnimals[building];

            foreach (ushort animal in animals)
            {
                Data.Instance._AnimalsRemoved.Add(animal);
                RemoveAnimal(animal);
            }

            _buildingsAnimals.Remove(building);
        }

        private bool GetAnimal()
        {
            _animal = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)_id];

            if (_animal.Info == null)
                return false;

            if (!_animal.Info.m_citizenAI.IsAnimal())
                return false;

            if ((_animal.m_flags & CitizenInstance.Flags.Created) == CitizenInstance.Flags.None)
                return false;

            if (float.IsNegativeInfinity(_animal.Info.m_maxRenderDistance))
                return false;

            return true;
        }

        private bool UpdateAnimal(ushort id)
        {
            _id = id;

            if (!GetAnimal())
                return false;

            _categories = _mapping.GetMapping(_animal.Info);

            if (_categories.Count == 0)
                return false;

            foreach (HashSet<ushort> category in _categories)
                category.Add(_id);

            return true;
        }

        internal void RequestRemoval(ushort id)
        {
            _id = id;

            if (!GetAnimal())
                RemoveAnimal(id);
        }

        private void RemoveAnimal(ushort id)
        {
            Data.Instance._Animals.Remove(id);

            Data.Instance._Birds.Remove(id);
            Data.Instance._Seagulls.Remove(id);

            Data.Instance._Livestock.Remove(id);
            Data.Instance._Cows.Remove(id);
            Data.Instance._Pigs.Remove(id);

            Data.Instance._Pets.Remove(id);
            Data.Instance._Dogs.Remove(id);

            Data.Instance._Wildlife.Remove(id);
            Data.Instance._Wolves.Remove(id);
            Data.Instance._Bears.Remove(id);
            Data.Instance._Moose.Remove(id);

            Data.Instance._AnimalOther.Remove(id);
        }

        private void OutputDebugLog()
        {
            if (!Helper.Instance.AnimalMonitorSpun) return;

            if (!Settings.Instance.Debug._AnimalMonitor) return;

            if (!Settings.Instance.Enable._AnimalMonitor) return;

            if (!_initialized) return;

            if (!SimulationManager.instance.SimulationPaused) return;

            if (_paused) return;

            string log = "\r\n";
            log += "==== ANIMALS ====\r\n";
            log += "\r\n";
            log += String.Format("{0}   Total\r\n", Data.Instance._Animals.Count);
            log += String.Format("{0}   Updated\r\n", Data.Instance._AnimalsUpdated.Count);
            log += String.Format("{0}   Removed\r\n", Data.Instance._AnimalsRemoved.Count);
            log += "\r\n";
            log += String.Format("{0}   Bird(s)\r\n", Data.Instance._Birds.Count);
            log += String.Format(" =>   {0}   Seagull(s)\r\n", Data.Instance._Seagulls.Count);
            log += "\r\n";
            log += String.Format("{0}   Livestock\r\n", Data.Instance._Livestock.Count);
            log += String.Format(" =>   {0}   Cow(s)\r\n", Data.Instance._Cows.Count);
            log += String.Format(" =>   {0}   Pig(s)\r\n", Data.Instance._Pigs.Count);
            log += "\r\n";
            log += String.Format("{0}   Pet(s)\r\n", Data.Instance._Pets.Count);
            log += String.Format(" =>   {0}   Dog(s)\r\n", Data.Instance._Dogs.Count);
            log += "\r\n";
            log += String.Format("{0}   Wildlife\r\n", Data.Instance._Wildlife.Count);
            log += String.Format(" =>   {0}   Wolf(s)\r\n", Data.Instance._Wolves.Count);
            log += String.Format(" =>   {0}   Bear(s)\r\n", Data.Instance._Bears.Count);
            log += String.Format(" =>   {0}   Moose\r\n", Data.Instance._Moose.Count);
            log += "\r\n";
            log += String.Format("{0}   Other\r\n", Data.Instance._AnimalOther.Count);
            log += "\r\n";

            Helper.Instance.Log(log);

            _paused = true;
        }
    }
}